using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET9_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using ClrDebug;
using ClrDebug.DIA;
using PESpy.PDB;

namespace PESpy.View
{
    public enum ViewByteFormatKind
    {
        Address,
        Code,
        Symbol,
        Number,
        String,
        Byte,
        Line
    }

    public enum MeaningValueKind
    {
        HexString = 1,
        StructXRef,
        ValueXRef,
        FunctionXRef,
        Enum,
        Size
    }

    //Represents a visitor capable of constructing a text representation of a given line
    //and pausing/resuming visitation
    public abstract unsafe class AbstractViewByteFormatter : ViewVisitor
    {
        //Stores the path of all "resumable" nodes up to the current point. A node is considered
        //to be "resumable" if has children thus should be interrupted such that it does not spend
        //too much time writing content that may later end up being discarded (e.g. structs, disassembly, etc)
        protected Stack<EntityState> _path = new();
        protected FileAccessor _fileAccessor;
        protected ValueStringBuilder.NonRef _builder;
        private List<PhysicalLine> _physicalLines = new List<PhysicalLine>();
        protected List<LogicalLine> _logicalLines = new List<LogicalLine>();
        protected readonly ViewByteFormatRangeList _formatRanges;

        private int _sectionAccessorIndex;
        private string? _currentSectionName;
        private ViewByte* _sectionViewByteStart;
        private ViewByte* _sectionViewByteEnd;
        private ViewByte* _currentGlobalViewByte;
        protected int _rva;
        protected byte* _pBytes;
        private int _addressWidth;
        protected int _numLinesWritten;
        protected int _numLinesNeeded;

        private int _dosStubStart;
        private int _dosStubEnd;

        protected const FieldViewFlags prohibitDecimalFlags = FieldViewFlags.Address | FieldViewFlags.HexString; //Allow size

        internal Direction Direction;

        private string DebuggerDisplay => _builder.ToString();

        public Span<LogicalLine> PeekLogicalLines()
        {
            FinalizeLogicalLine();

            //We can't clear the list yet; when the elements are reference types, clear will also do Array.Clear
#if NET9_0_OR_GREATER
            var span = CollectionsMarshal.AsSpan(_logicalLines);
#else
            var span = _logicalLines.ToArray();
#endif

            return span;
        }

        public int LastAddress
        {
            get
            {
                if (_path.Count > 0)
                {
                    var offset = 0;

                    foreach (var item in _path)
                    {
                        switch (item.Kind)
                        {
                            case EntityKind.View:
                                offset += item.View.Offset;
                                break;

                            case EntityKind.Data:
                                offset += item.StartOffset;
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }

                    return offset;
                }

                throw new NotImplementedException();
            }
        }

        public void ClearLogicalLines()
        {
            _logicalLines.Clear();
        }

        protected void FinalizeLogicalLine()
        {
            //In some circumstances we may need to eagerly build the logical line to retain our depth when walking backwards
            if (_physicalLines.Count == 0)
                return;

            var logicalLine = new LogicalLine(
                _builder.ToString(),
                _path.Count,
                _physicalLines.ToArray(),
                _formatRanges.ToArrayAndClear()
            ); //todo: find a way to not allocate

            _builder.Clear();
            _physicalLines.Clear();

            _logicalLines.Add(logicalLine);
        }

        public AbstractViewByteFormatter(FileAccessor fileAccessor)
        {
            _fileAccessor = fileAccessor;
            _builder = new ValueStringBuilder.NonRef(100);
            _formatRanges = new ViewByteFormatRangeList();

            if (fileAccessor is PEFileAccessor p)
            {
                var dosStub = p.PEFile.DosStub;

                _dosStubStart = dosStub.Offset;
                _dosStubEnd = _dosStubStart + dosStub.Bytes.Length;
            }
        }

        public void RequestLines(int numLinesNeeded)
        {
            _numLinesNeeded = numLinesNeeded;
            _numLinesWritten = 0;
        }
            //We're starting from scratch. Find the top level entity that is associated
            //with the given address
            var pViewByte = _fileAccessor.GetViewByte(ownerAddress, out var sectionAccessorIndex);
            _currentGlobalViewByte = pViewByte;

            _numLinesWritten = 0;
            _numLinesNeeded = numLinesNeeded;

            //This method assumes that we already know that owner address does not point to a body.
            //If you don't know what the address points to, you should be calling StartWithoutOwner
            Debug.Assert(pViewByte->Kind != ViewByteKind.Body);

            ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
            _sectionAccessorIndex = sectionAccessorIndex;

            InitializeSection(sectionAccessor, pViewByte);

            //Based on the type of ViewByte we have here, we'll construct an appropriate IView around it
            //to then be processed by an appropriate visitor
            ProcessViewByte(pViewByte, targetAddress, ownerAddress);
        }

        //All we know is that we want to start from a given address, but we don't know who owns it
        public void StartWithoutOwner(int targetAddress, int numLinesNeeded)
        {
            var pViewByte = _fileAccessor.GetViewByte(targetAddress, out var sectionAccessorIndex);

            var sectionAccessors = _fileAccessor.SectionAccessors;

            ref var sectionAccessor = ref sectionAccessors[sectionAccessorIndex];
            _currentSectionName = sectionAccessor.Name;
            _sectionViewByteStart = sectionAccessor.pViewBytes;
            _sectionViewByteEnd = sectionAccessor.pViewBytes + sectionAccessor.Length;

            var ownerAddress = targetAddress;

            if (pViewByte->Kind != ViewByteKind.Body)
            {
                if (pViewByte->Kind == ViewByteKind.Unknown)
                {
                    //We need to rewind to get the first unknown byte, while also making sure
                    //we don't go beyond the start of the section

                    var sectionStart = sectionAccessor.pViewBytes;

                    var head = pViewByte;

                    while (true)
                    {
                        if (head == sectionStart)
                            break;

                        if (head->Kind != ViewByteKind.Unknown)
                        {
                            head++;
                            break;
                        }

                        head--;
                    }
                //Easy case: we're already on a point of interest
                StartWithOwner(targetAddress, ownerAddress, numLinesNeeded);
                return;
            }

            //We need to rewind to the start of the last global entity, and then fast forward
            //into that to get to the point where the target address starts

            while (pViewByte->Kind == ViewByteKind.Body)
            {
                if (pViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                {
                    throw new NotImplementedException();
                }

                ownerAddress--;
                pViewByte--;
            }

            StartWithOwner(targetAddress, ownerAddress, numLinesNeeded);
        }

        private void InitializeSection(in SectionAccessor sectionAccessor, ViewByte* pViewByte)
        {
            _currentSectionName = sectionAccessor.Name;

            _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out var rva, out _);

            _sectionViewByteStart = sectionAccessor.pViewBytes;
            _sectionViewByteEnd = sectionAccessor.pViewBytes + sectionAccessor.Length;

            var relativeOffset = (int) (pViewByte - sectionAccessor.pViewBytes);

            _rva = rva + relativeOffset;
            _pBytes = pBytes + relativeOffset;

            _addressWidth = GetAddressWidth((ulong) sectionAccessor.EndAddress);
        }
                        if (view is IContainerView c)
                        {
                            ViewChildList children;

                            if (current.Children == null)
                            {
                                children = c.Children;
                                current.LastChildIndex = 0;
                                current.Children = children;
                            }
                            else
                                children = current.Children.Value;

                            for (var i = current.LastChildIndex; i < children.Count; i++)
                            {
                                var child = children[i];

                                if (child.Contains(lastVisibleLine.StartAddress))
                                {
                                    current.LastChildIndex = i;
                                    current = new EntityState(child, child is IContainerView);
                                    _path.Push(current);
                                    break;
                                }
            //The last line of the logical line should be visible
            Debug.Assert(lastLogicalLine.Lines[lastLogicalLine.Lines.Length - 1].IsVisible);

            if (lastVisibleLine.Length == 0)
            {
                //The last line is a line break or something for the content that comes next
                StartWithoutOwner(lastVisibleLine.StartAddress, numLinesNeeded);
            }
            else
            {
                //The last line is content itself, so we need to start after it. The EndAddress is +1 past the end
                //of the actual content already
                StartWithoutOwner(lastVisibleLine.EndAddress, numLinesNeeded); //EndAddress is +1 past the end
            }
            
            return true;
        }

        //If we return true, we had to StartWithOwner to figure out what's going on, which means we also wrote a line that the caller
        //will need to process
        internal bool PrepareToRewind(LogicalLine firstLogicalLine, int numLinesNeeded)
        {
            //Opposite of PrepareToFastForward: we need to adjust the path so that the child index of any view
            //is the index of the child that is currently at the top of the screen

            Debug.Assert(Direction == Direction.Down);

            //There should always be a depth
            Debug.Assert(firstLogicalLine.Depth > 0);

            Direction = Direction.Up;

            if (_path.Count == 0)
                return false;

            var firstVisibleLine = firstLogicalLine.FirstVisibleLine;

            while (_path.Count > 0)
            {
                var current = _path.Peek();

                if (current.View == null)
                {
                    //We're not currently on a view...at the bottom of the screen. That doesn't tell us what may actually exist at the top of the screen!
                    //At this point, we've got no-idea what's going on, we need to recompute the path
                    _path.Clear();

                    //todo: a. we're not incrementing the depth when processing fields
                    //b. this code path doesnt take depth into consideration

                    //We're being forced to write something. We don't want to write firstVisibleLine.StartAddress, because that's already
                    //visible. So we write the thing _before_ it, by subtracting 1 from the address
                    StartWithoutOwner(firstVisibleLine.StartAddress - 1, numLinesNeeded);
                    return true;
                }

                if (current.View.Contains(firstVisibleLine.StartAddress))
                {
                    if (current.View.Offset == firstVisibleLine.StartAddress)
                    {
                        //Is this the value we're after, or not? Use the depth stored on the logical line
                        //to tell us whether the value stored in the logical line corresponds to a struct header or a field
                        
                        if (firstLogicalLine.Depth == _path.Count)
                        }
                    }
                    else
                    {
                        //The first visible line corresponds to some field in the current struct. Rewind through the children to find the field that owns us
                        Debug.Assert(current.LastChildIndex != -1);

                        var children = current.Children.Value;

                        for (var i = current.LastChildIndex - 1; i >= 0; i--)
                        {
                            var child = children[i];

                            if (child.Contains(firstVisibleLine.StartAddress))
                            {
                                Debug.Assert(firstLogicalLine.Depth >= _path.Count);

                                if (firstLogicalLine.Depth == _path.Count + 1)
                                {
                                    //We're at the right depth. Update the path to record where the screen is up to
                                    current.LastChildIndex = i;
                                    return false;
                                }
                                else
                                {
                                    //We need to dig into this child. Our depth should not be less than the current depth
                                    Debug.Assert(firstLogicalLine.Depth > _path.Count);
                                    while (_path.Count < firstLogicalLine.Depth)
                                    {
                                        IView view = current.View;

                                        if (view is IStructFieldView s)
                                            view = s.Value;
                                        else if (view is IStructArrayFieldView)
                                            throw new NotImplementedException();

                                        if (view is IContainerView c)
                                        {
                                            if (current.Children == null)
                                            {
                                                children = c.Children;
                                                current.LastChildIndex = children.Count - 1;
                                                current.Children = children;
                                            }
                                            else
                                                children = current.Children.Value;

                                            for (var j = current.LastChildIndex; j >= 0; j--)
                                            {
                                                child = children[j];

                                                if (child.Contains(firstVisibleLine.StartAddress))
                                                {
                                                    current.LastChildIndex = j;
                                                    current = new EntityState(child, child is IContainerView);
                                                    _path.Push(current);
                                                    break;
                                                }
                                            }
                                        }
                    _path.Pop();
                }
            }

            //Ran out of path. Just goto instead

            Debug.Assert(_path.Count == 0);

            StartWithoutOwner(firstVisibleLine.StartAddress - 1, numLinesNeeded);
            return true;
        }

        public bool MoveNext()
        {
            Debug.Assert(this.Direction == Direction.Down);

            int targetAddress = -1;
            var lastParentSize = -1;

            IncrementResult incrementResult = IncrementResult.End;

            while (_path.Count > 0)
            {
                var current = _path.Peek();

                if (current.Kind == EntityKind.View)
                {
                    if (current.LastChildIndex == -1)
                    {
                        if (current.HasChildren)
                            current.Children = ((IContainerView) current.View!).Children;
                        else
                        {
                            //It was a field (which we pushed on just so it could record its depth properly)
                            _path.Pop();
                            continue;
                        }
                    }

                    var children = current.Children!.Value;

                    current.LastChildIndex++;

                    if (current.LastChildIndex < children.Count)
                    {
                        var child = children[current.LastChildIndex];
                        Visit(child);
                        return true;
                    }
                    else
                    {
                        //We've reached the last child
                        var lastParent = _path.Pop();
                        targetAddress = lastParent.StartOffset;
                        lastParentSize = lastParent.View!.Size;

                        if (_path.Count == 0)
                        {
                            incrementResult = IncrementBytes(lastParentSize, ref _currentGlobalViewByte, ref targetAddress);

                            Debug.Assert(incrementResult == IncrementResult.End || _currentGlobalViewByte->Kind != ViewByteKind.Body);
                        }
                    }
                }
                else if (current.Kind == EntityKind.Asm || current.Kind == EntityKind.Data)
                {
                    if (current.IsAtEnd)
                    {
                        _path.Pop();

                        lastParentSize = current.BytesWritten;

                        if (current.IsStreamer)
                        {
                            //If we're messing with the global view byte, we should be top level
                            Debug.Assert(_path.Count == 0);

                            targetAddress = current.StartOffset + current.BytesWritten;

                            //We don't call IncrementBytes, because we already do that
                            //as we read each instruction, however the increment we do
                            //does not affect the global view byte, so we need to update
                            //that here
                            _currentGlobalViewByte += current.BytesWritten;
                            incrementResult = current.LastIncrementResult;
                        }
                        else
                        {
                            //Just some regular old single line data
                            Debug.Assert(current.Kind == EntityKind.Data);

                            targetAddress = current.StartOffset;

                            incrementResult = IncrementBytes(lastParentSize, ref _currentGlobalViewByte, ref targetAddress);
                        }
                    }
                    else
                    {
                        Debug.Assert(current.IsStreamer);

                        //Continue writing
                        var pResumeByte = _currentGlobalViewByte + current.BytesWritten;
                        var resumeAddress = current.StartOffset + current.BytesWritten;

                        if (current.Kind == EntityKind.Asm)
                            ProcessCode(pResumeByte, resumeAddress, current);
                        else
                            ProcessRawData(pResumeByte, resumeAddress, current.StartOffset, current.DataName, current.DataLength, current);

                        return true;
                    }
                }
                else if (current.Kind == EntityKind.Data)
            while (_path.Count > 0)
            {
                var current = _path.Peek();

                if (current.Kind == EntityKind.View)
                {
                    if (current.LastChildIndex == -1)
                    {
                        if (current.HasChildren)
                        {
                            current.Children = ((IContainerView) current.View!).Children;
                            current.LastChildIndex = current.Children.Value.Count; //We go +1 past the end because we're about to do -1
                        }
                        else
                        {
                            //It was a field (which we pushed on just so it could record its depth properly)
                            _path.Pop();
                            continue;
                        }
                    }

                    if (current.LastChildIndex == 0)
                    {
                        //We processed the first child on the last iteration, so now we're at the head.
                        //The head is going to want to push itself onto the stack, so after visiting it we'll need to pop it again
                        _path.Pop();
                        Visit(current.View);

                        //Make sure the line is finalized before we return, because we're going to lose our true depth
                        FinalizeLogicalLine();

                        _path.Pop();
                        return true;
                    }
                    else
                    {
                        //We want to move to the previous child

                        current.LastChildIndex--;
                        var child = current.Children.Value[current.LastChildIndex];

                        if (child is IStructFieldView s)
                            child = s.Value;
                        else if (child is IStructArrayFieldView)
                            throw new NotImplementedException();

                        //todo: do we care if the child is an istructfieldview or istructfieldarrayview?
                        while (child is IContainerView c)
                        {
                            //We need to dig into the children at the end of it
                            var children = c.Children;

                            var entity = new EntityState(child)
                            {
                                Children = children,
                                LastChildIndex = children.Count - 1
                            };

                            _path.Push(entity);

                            child = children[children.Count - 1];
                        }

                        Visit(child);
                        return true;
                        Debug.Assert(current.IsStreamer);

                        if (current.Kind == EntityKind.Asm)
                            ProcessCode(current.ReverseViewByte, current.ReverseResumeAddress, current);
                        else
                            ProcessRawData(current.ReverseViewByte, current.ReverseResumeAddress, current.StartOffset, current.DataName, current.DataLength, current);

                        return true;
                    }
                }
        #endregion

        //Width is in hex chars
        private int GetAddressWidth(ulong value)
        {
            int numChars = 1;

            while ((value >>= 4) != 0)
                numChars++;

            return numChars;
        }

        #region ProcessViewByte

        private void ProcessViewByte(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            EntityState state;

            switch (pViewByte->Kind)
            {
                case ViewByteKind.Code:
                    state = new EntityState(EntityKind.Asm, pViewByte, ownerAddress)
                    {
                        IsStreamer = true
                    };
                    _path.Push(state);

                    //targetAddress might be a body instruction inside the instruction. pViewByte already points to the head
                    ProcessCode(pViewByte, ownerAddress, state);
                    break;

                case ViewByteKind.Data:
                    ProcessData(pViewByte, targetAddress, ownerAddress);
                    break;

                case ViewByteKind.Unknown:
                    state = new EntityState(EntityKind.Data, pViewByte, ownerAddress)
                    {
                        DataName = "Unknown",
                        DataLength = pViewByte->GetUnknownLength(_sectionViewByteEnd), //todo: counting the length of a large number of unknown bytes is very slow, and thats bad if we're on the ui thread
                        IsStreamer = true
                    };
                    _path.Push(state);

                    ProcessRawData(pViewByte, targetAddress, ownerAddress, state.DataName, state.DataLength, state);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        //We don't need to pass in owner address; we only support imprecise
        protected abstract void ProcessCode(ViewByte* pViewByte, int targetAddress, EntityState state);

        protected void ReverseLogicalLines(int startNumLogicalLines)
        {
            if (Direction == Direction.Up)
            {
                //We've written logical lines in reverse order, but when we draw lines in reverse we already reverse the order
                //so we need to un-reverse the lines here
                var endNumLogicalLines = _logicalLines.Count;

                var numLinesToReverse = endNumLogicalLines - startNumLogicalLines;

                if (numLinesToReverse > 0)
                {
                    _logicalLines.Reverse(startNumLogicalLines, numLinesToReverse);
                }
            }
        }

        protected bool IsInDosStub(int targetAddress)
        {
            //Gets whether the given address is within the range of the DOS Stub

            return targetAddress >= _dosStubStart && targetAddress < _dosStubEnd;
        }

        private void ProcessData(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            switch (pViewByte->DataKind)
            {
                case ViewByteDataKind.Struct:
                    ProcessStructView(pViewByte, targetAddress, ownerAddress);
                    break;

                case ViewByteDataKind.String:
                    ProcessString(pViewByte, ownerAddress);
                    break;

                case ViewByteDataKind.Integer:
                case ViewByteDataKind.Decimal:
                    ProcessNumericData(pViewByte, targetAddress, ownerAddress);
                    break;

                case ViewByteDataKind.Unknown:
                    ProcessUnknownData(pViewByte, targetAddress, ownerAddress);
                    break;

                case ViewByteDataKind.Padding:
                    var state = new EntityState(EntityKind.Data, pViewByte, ownerAddress)
                    {
                        DataName = "Padding",
                        DataLength = pViewByte->GetLength(_sectionViewByteEnd),
                        IsStreamer = true
                    };
                    _path.Push(state);

                    ProcessRawData(pViewByte, targetAddress, ownerAddress, state.DataName, state.DataLength, state);
                    break;

        //It doesn't matter what the target address is, we only write a single line here
        private void ProcessString(ViewByte* pViewByte, int ownerAddress)
        {
#if DEBUG
            var startLinesWritten = _numLinesWritten;
#endif
            WriteLinePrefix(ownerAddress);

            var length = pViewByte->GetLength(_sectionViewByteEnd);

            var startPos = _builder.Length;

            if (pViewByte->IsWide)
            {
                _builder.Append("L\"");

                var str = new FixedUtf16String((char*) _pBytes, length / 2);

                _builder.AppendEscaped(str);
            }
            else
            {
                _builder.Append("\"");

                var str = new FixedUtf8String((byte*) _pBytes, length);

                _builder.AppendEscaped(str);
            }

            _builder.Append("\"");

            _formatRanges.Add(ViewByteFormatKind.String, startPos, _builder.Length);

            AppendLine(ownerAddress, length);

#if DEBUG
            var totalLinesWritten = _numLinesWritten - startLinesWritten;
            Debug.Assert(totalLinesWritten == 1, "Writing more than 1 line means we need to take the target address into consideration and start writing from the first line that contains that address");
#endif

            _path.Push(new EntityState(EntityKind.Data, pViewByte, ownerAddress)
            {
                IsAtStart = true,
                IsAtEnd = true,
                BytesWritten = length
            });
        }

        private void ProcessNumericData(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            Debug.Assert(targetAddress == ownerAddress, "Starting in the middle is not yet implemented");

            var length = pViewByte->GetLength(_sectionViewByteEnd);

            _path.Push(new EntityState(EntityKind.Data, pViewByte, ownerAddress)
            {
                IsAtStart = true,
                IsAtEnd = true,
                BytesWritten = length
            });

            if (_fileAccessor.TryGetStructKind(targetAddress, out var kind))
            {
                switch (kind)
                {
                    case ViewKind.XFG:
                        WriteLinePrefix(targetAddress, true);
                        Debug.Assert(length == 8);
                        _builder.Append("XFG: 0x");
                        _builder.AppendHex(*(ulong*) _pBytes, 16);
                        AppendLine(targetAddress, length);
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }

            if (pViewByte->HasName)
            {
                WriteLinePrefix(targetAddress, true);

                WriteName(targetAddress);

                AppendLine(targetAddress, 0);
            }
                if (pViewByte->IsUnsigned)
                {
                    switch (length)
                    {
                        case 1:
                            var b = *_pBytes;
                            _builder.Append(b);
                            break;

                        case 2:
                            var s = *(ushort*) _pBytes;
                            _builder.Append(s);
                            break;

                        case 4:
                            var i = *(uint*) _pBytes;
                            _builder.Append(i);
                            break;

                        case 8:
                            var l = *(ulong*) _pBytes;
                            _builder.Append(l);
                            break;
                    }
                }
                else
                {
                    switch (length)
                    {
                        case 1:
                            var b = *_pBytes;
                            _builder.Append(b);
                            break;

                        case 2:
                            var s = *(short*) _pBytes;
                            _builder.Append(s);
                            break;

                        case 4:
                            var i = *(int*) _pBytes;
                            _builder.Append(i);
                            break;

                        case 8:
                            var l = *(long*) _pBytes;
                            _builder.Append(l);
                            break;
                    }
                }
            }
            else
            {
                Debug.Assert(pViewByte->DataKind == ViewByteDataKind.Decimal);

                if (length == 4)
                {
                    var f32 = *(float*) _pBytes;

                    _builder.Append(f32);
                }
                else
                {
                    Debug.Assert(length == 8);

                    var f64 = *(double*) _pBytes;

                    _builder.Append(f64);
                }
            }

            AppendLine(targetAddress, length);
        }

        private void ProcessUnknownData(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            Debug.Assert(targetAddress == ownerAddress, "Starting in the middle is not yet implemented");
            _path.Push(new EntityState(EntityKind.Data, pViewByte, ownerAddress)
            {
                IsAtEnd = true,
                BytesWritten = length
            });

            if (pViewByte->HasName)
            {
                WriteLinePrefix(targetAddress, true);

                WriteName(targetAddress);

                AppendLine(targetAddress, 0);
            }
        private void ProcessRawData(ViewByte* pViewByte, int targetAddress, int ownerAddress, string name, int length, EntityState state)
        {
            const int bytesPerLine = 100;

            var offset = targetAddress - ownerAddress;

            var bytesWrittenThisRequest = 0;

            var startNumLogicalLines = _logicalLines.Count;

            while (true)
            {
                var lineNumber = (offset + bytesWrittenThisRequest) / bytesPerLine;
                var lineStart = lineNumber * bytesPerLine;
                var remaining = length - lineStart;
                var lineLength = Math.Min(bytesPerLine, remaining);

                var lineBytes = new Span<byte>(_pBytes + lineStart, lineLength);

                targetAddress = ownerAddress + lineStart;

                if (targetAddress == ownerAddress)
                {
                    var startPos = WriteDividerPrefix(targetAddress);
                    _builder.Append(name);
                    _builder.Append(" (");
                    _builder.Append(length);
                    _builder.Append(')');
                    WriteDividerSuffix(targetAddress, startPos);
                }

                WriteBytes(targetAddress, lineBytes);
                bytesWrittenThisRequest += lineBytes.Length;

                if (NeedTrailingLine(pViewByte + lineBytes.Length))
                {
                    WriteDivider(targetAddress + lineBytes.Length);
                    _numLinesWritten++;
                }

                if (Direction == Direction.Down)
                {
                    //This will handle moving us to the next section and/or checking
                    //if we've reached the end
                    if ((state.LastIncrementResult = IncrementBytes(lineBytes.Length, ref pViewByte, ref targetAddress)) == IncrementResult.End)
                    {
                        break; //We ran out of bytes!
                    }

                    if (bytesWrittenThisRequest == remaining)
                    {
                        state.IsAtEnd = true;
                        break; //We hit the end
                    }

                    //Kind must either be Unknown, or Data + Padding
                    if (state.pStartViewByte->Kind == ViewByteKind.Unknown)
                    {
                        if (pViewByte->Kind != ViewByteKind.Unknown)
                        {
                            state.IsAtEnd = true;
                            break;
                        }
                    }
                    else if (state.pStartViewByte->Kind == ViewByteKind.Data)
                    {
                        if (pViewByte->Kind != ViewByteKind.Data || pViewByte->DataKind != ViewByteDataKind.Padding)
                        {
                            state.IsAtEnd = true;
                            break;
                        }
                    }
                    state.ReverseResumeAddress = targetAddress; //Resume from here
                    state.ReverseViewByte = pViewByte;

                    if (state.pStartViewByte->Kind == ViewByteKind.Data)
                    {
                        if (pViewByte->Kind != ViewByteKind.Data || pViewByte->DataKind != ViewByteDataKind.Padding)
                        {
                            state.IsAtStart = true;
                            break;
                        }
                    }
                }

                if (_numLinesWritten >= _numLinesNeeded)
                {
                    break;
                }
            }

            FinalizeLogicalLine();

            ReverseLogicalLines(startNumLogicalLines);

            state.BytesWritten += bytesWrittenThisRequest;
        private void WriteBytes(int lineStart, Span<byte> lineBytes)
        {
            WriteLinePrefix(lineStart);

            for (var i = 0; i < lineBytes.Length; i++)
            {
                var startPos = _builder.Length;

                _builder.Append("0x");

                var b = lineBytes[i];

                //If it's a single character hex digit, add a leading 0
                if (b <= 0xF)
                    _builder.Append('0');

                _builder.AppendHex(b);

                _formatRanges.Add(ViewByteFormatKind.Byte, startPos, _builder.Length);

                if (i < lineBytes.Length - 1)
                    _builder.Append(',');
            }

            AppendLine(lineStart, lineBytes.Length);
        }

        private bool NeedTrailingLine(ViewByte* pViewByte)
        {
            //We write a line before the start of code, but not before the start of structs.
            //So if the next address is not going to be code, we should add a line
            var temp = 0;
            var result = GetIncrementResult(pViewByte, ref temp);

            switch (result)
            {
                case IncrementResult.SameSection:
                    break;

                case IncrementResult.NextSection:
                    //We need to get the first byte of the next section and check their type.
                    //The sectionAccessorIndex has already been incremented for us
                    pViewByte = _fileAccessor.SectionAccessors[_sectionAccessorIndex].pViewBytes;
                    break;

                case IncrementResult.End:
                    return false;

                default:
                    throw new NotImplementedException();
            }

            switch (pViewByte->Kind)
            {
                case ViewByteKind.Code:
                case ViewByteKind.Unknown:
                    return false;

                case ViewByteKind.Data:
                    return pViewByte->DataKind != ViewByteDataKind.Padding;

                default:
                    return true;
            }
        }
        private void WriteDivider(int targetAddress)
        {
            WriteLinePrefixNoSpace(targetAddress);
            AppendFormat(" ---------------------------------------------------------------------------", ViewByteFormatKind.Line);
            AppendLine(targetAddress, 0);
        }

        private int WriteDividerPrefix(int targetAddress)
        {
            WriteLinePrefixNoSpace(targetAddress);

            //Normal line is 76

            var startPos = _builder.Length;

            _builder.Append(" ------- ");

            return startPos;
        }

        private void WriteDividerSuffix(int targetAddress, int startPos)
        {
            _builder.Append(' ');

            for (var i = _builder.Length; i < startPos + 76; i++)
                _builder.Append('-');

            _formatRanges.Add(ViewByteFormatKind.Line, startPos, _builder.Length);
            AppendLine(targetAddress, 0);
        }

        #endregion

        private IView BuildPathToAddress(IView view, int targetAddress)
        {
            //For all nodes up to the result, add them to the path. We don't add the result to the path
            //because they'll be added when the caller calls Accept() on the result

            while (true)
            {
                //If the offset overlaps with the start of a container view, we return the container view
                if (targetAddress == view.Offset)
                    return view;

                if (view is IContainerView c)
                {
                    var children = c.Children;

                    var entity = new EntityState(view)
                    {
                        Children = children
                    };

                    _path.Push(entity);

                    var found = false;

                    foreach (var child in c.Children)
                    {
                        entity.LastChildIndex++;

                        if (child.Contains(targetAddress))
                        {
                            view = child;
        protected IncrementResult IncrementBytes(int increment, ref ViewByte* pViewByte, ref int targetAddress)
        {
            pViewByte += increment;
            targetAddress += increment;
            _rva += increment;
            _pBytes += increment;

            return GetIncrementResult(pViewByte, ref targetAddress);
        }

        protected bool DecrementBytes(ref ViewByte* pViewByte, ref int targetAddress)
        {
            if (pViewByte == _sectionViewByteStart)
            {
                //Move to the previous section

                if (_sectionAccessorIndex == 0)
                    return false;

                _sectionAccessorIndex--;

                ref var sectionAccessor = ref _fileAccessor.SectionAccessors[_sectionAccessorIndex];

                pViewByte = sectionAccessor.pViewBytes + sectionAccessor.Length - 1;
                targetAddress = sectionAccessor.EndAddress - 1;

                //We've reached the start of the value in any case
                return false;
            }

            var pPrevious = pViewByte - 1;

            var @continue = true;

            while (@continue)
            {
                switch (pPrevious->Kind)
                {
                    case ViewByteKind.Body:
                        if (pPrevious->BodyKind == ViewByteBodyKind.SplitHead)
                        {
                            @continue = false;
                            break;
                        }

                        pPrevious--;
                        break;

                    default:
                        @continue = false;
                        break;
                }
            }

            var distance = (int) (pViewByte - pPrevious);

            targetAddress -= distance;
            _pBytes -= distance;
            pViewByte = pPrevious;
            return true;
        }

        private IncrementResult GetIncrementResult(ViewByte* pViewByte, ref int targetAddress)
        {
            if (pViewByte >= _sectionViewByteEnd)
            {
                //Move to the next section, or return false if we're out of sections

                _sectionAccessorIndex++;

                var sectionAccessors = _fileAccessor.SectionAccessors;

                if (_sectionAccessorIndex >= sectionAccessors.Length)
                    return IncrementResult.End;

                ref var sectionAccessor = ref sectionAccessors[_sectionAccessorIndex];

                InitializeSection(sectionAccessor, sectionAccessor.pViewBytes);
                _currentGlobalViewByte = sectionAccessor.pViewBytes;
                targetAddress = sectionAccessor.StartAddress;
                return IncrementResult.NextSection;
            }

            return IncrementResult.SameSection;
        }
        protected internal override void VisitField(IFieldView view)
        {
            //Derived methods must call the base method to update the _path

            //We need to do this so that the logical line records its depth properly
            _path.Push(new EntityState(view, hasChildren: false));
        }

        protected internal static Type FormatField(
            IFieldView view,
            FileAccessor fileAccessor,
            ref ValueStringBuilder.NonRef builder,
            bool listFormat = false)
        {
            bool wantDecimal;
            bool wantPadding;

            switch (view.ValueType)
            {
                #region Numbers

                //We don't just show "0" when it's 0 because we want to show how big each value is
                case nameof(Byte):
                    var @byte = ((FieldView<byte>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex((byte) @byte, wantPadding ? 2 : 0);

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(@byte);
                        builder.Append(')');
                    }

                    return typeof(byte);

                case nameof(Int16):
                    var @short = ((FieldView<short>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex((ushort) @short, wantPadding ? 4 : 0);

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(@short);
                        builder.Append(')');
                    }

                    return typeof(short);

                case nameof(UInt16):

                    var @ushort = ((FieldView<ushort>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex(@ushort, wantPadding ? 4 : 0);

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(@ushort);
                        builder.Append(')');
                    }

                    return typeof(ushort);

                case nameof(Int32):
                    var int32 = ((FieldView<int>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex((uint) int32, wantPadding ? 8 : 0);

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(int32);
                        builder.Append(')');
                    }

                    return typeof(int);

                case nameof(UInt32):
                    var uint32 = ((FieldView<uint>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex((uint) uint32, wantPadding ? 8 : 0);

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(uint32);
                        builder.Append(')');
                    }

                    return typeof(uint);

                case nameof(Int64):
                    var int64 = ((FieldView<long>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex((ulong) int64); //Don't pad, these numbers become too big

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append(int64);
                        builder.Append(')');
                    }

                    return typeof(long);

                case nameof(UInt64):
                    var uint64 = ((FieldView<ulong>) view).Value;

                    GetNumberFormat(listFormat, view, out wantDecimal, out wantPadding);

                    builder.Append("0x");
                    builder.AppendHex(uint64); //Don't pad, these numbers become too big

                    if (wantDecimal)
                    {
                        builder.Append(" (");
                        builder.Append((long) uint64);
                        builder.Append(')');
                    }

                    return typeof(ulong);

                case nameof(PN):
                    var pn = ((FieldView<PN>) view).Value;

                    builder.Append((int) pn);
                    return typeof(PN);

                #endregion
                #region Strings

                case nameof(FixedUtf8String):
                    builder.Append(((FieldView<FixedUtf8String>) view).Value);
                    return typeof(FixedUtf8String);

                case nameof(AnsiString):
                    builder.Append((FixedUtf8String) ((FieldView<AnsiString>) view).Value);
                    return typeof(AnsiString);

                case nameof(String):
                    builder.Append((string) ((FieldView<string>) view).Value);
                    return typeof(AnsiString);

                #endregion
                #region Enums

                case nameof(IMAGE_FILE_MACHINE):
                    WriteEnumUInt32<IMAGE_FILE_MACHINE>(view, ref builder);
                    return typeof(IMAGE_FILE_MACHINE);

                case nameof(IMAGE_FILE):
                    WriteEnumUInt16<IMAGE_FILE>(view, ref builder);
                    return typeof(IMAGE_FILE);

                case nameof(IMAGE_SUBSYSTEM):
                    WriteEnumUInt16<IMAGE_SUBSYSTEM>(view, ref builder);
                    return typeof(IMAGE_SUBSYSTEM);

                case nameof(IMAGE_DEBUG_TYPE):
                    WriteEnumUInt32<IMAGE_DEBUG_TYPE>(view, ref builder);
                    return typeof(IMAGE_DEBUG_TYPE);

                case nameof(PRODID):
                    WriteEnumUInt16<PRODID>(view, ref builder);
                    return typeof(PRODID);
                case nameof(IMAGE_DLLCHARACTERISTICS):
                    WriteEnumUInt16<IMAGE_DLLCHARACTERISTICS>(view, ref builder);
                    return typeof(IMAGE_DLLCHARACTERISTICS);

                case nameof(IMAGE_GUARD_FLAG):
                    WriteEnumByte<IMAGE_GUARD_FLAG>(view, ref builder);
                    return typeof(IMAGE_GUARD_FLAG);

                case nameof(IMAGE_LOADER_FLAGS):
                    WriteEnumByte<IMAGE_LOADER_FLAGS>(view, ref builder);
                    return typeof(IMAGE_LOADER_FLAGS);

                case nameof(IMAGE_SCN):
                    WriteEnumUInt32<IMAGE_SCN>(view, ref builder);
                    return typeof(IMAGE_SCN);

                case nameof(PEMagic):
                    WriteEnumUInt16<PEMagic>(view, ref builder);
                    return typeof(PEMagic);

                case nameof(WIN_CERT_REVISION):
                    WriteEnumUInt16<WIN_CERT_REVISION>(view, ref builder);
                    return typeof(WIN_CERT_REVISION);

                case nameof(WIN_CERT_TYPE):
                    WriteEnumUInt16<WIN_CERT_TYPE>(view, ref builder);
                    return typeof(WIN_CERT_TYPE);
                    else if (view is FieldView<NativeSpan<PN>> @ap)
                    {
                        var value = @ap.Value;

                        for (var i = 0; i < value.Length; i++)
                        {
                            builder.Append((int) value[i]);

                            if (i < value.Length - 1)
                                builder.Append(",");
                        }

                        return typeof(NativeSpan<PN>);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static void GetNumberFormat(bool listFormat, IFieldView view, out bool wantDecimal, out bool wantPadding)
        {
            if (!listFormat)
            {
                wantDecimal = (view.Flags & prohibitDecimalFlags) == 0;
                wantPadding = true;
            }
            else
            {
                //When we're formatting for lists, we want a simplified format for Size as well
                wantDecimal = false;
                wantPadding = false;
            }
        }

        #region Meaning

        //Singletons want to expand enums into multiple rows, but lists want to include the enum description inline
        protected internal static bool TryGetMeaningIncludeEnum(
            FileAccessor fileAccessor,
            IFieldView fieldView,
            Type type,
            out string value,
            out MeaningValueKind kind)
        {
            if (type.IsEnum)
            {
                if (TryGetMultiFlags(type, fieldView, out var singleValue, out var flags))
                {
                    using var builder = new ValueStringBuilder();

                    for (var i = 0; i < flags.Length; i++)
                    {
                        builder.Append(flags[i].Text);

                        if (i < flags.Length - 1)
                            builder.Append(" | ");
                    }

                    value = builder.ToString();
                }
                else
                    value = singleValue;

                kind = MeaningValueKind.Enum;
                return true;
            }
            else
            {
                return TryGetMeaning(fileAccessor, fieldView, out value, out kind);
            }
        }

        internal static unsafe bool TryGetMeaning(
            FileAccessor fileAccessor,
            IFieldView fieldView,
            out string? value,
            out MeaningValueKind kind)
        {
            /* For fields whose value means something, including
             * - hex strings (MZ, PE00, etc)
             * - Enums
             * - RVAs
             * - Timestamps
             * 
             * we want to show some context about what it actually is that this value represents
             */

            if ((fieldView.Flags & FieldViewFlags.HexString) != 0)
            {
                value = GetHexString(fieldView);
                kind = MeaningValueKind.HexString;
                return true;
            }
            else if ((fieldView.Flags & FieldViewFlags.Address) != 0)
            {
                var pViewByte = fileAccessor.GetViewByte(fieldView.Offset, out _);

                if (pViewByte->HasXRefs)
                {
                    var xrefs = fileAccessor.GetXRefs(fieldView.Offset);

                    foreach (var xref in xrefs)
                    {
                        if (xref.Kind == XRefKind.From)
                        {
                            var target = fileAccessor.GetEntity(xref.Other);

                            switch (target.ViewByte->Kind)
                            {
                                case ViewByteKind.Code:
                                    if (target.HasChildren)
                                    {
                                        kind = MeaningValueKind.FunctionXRef;
                                        value = target.ToString();
                                        break;
                                    }

                                    //If we didn't have children, that means we're not pointing to the start of the function.
                                    //We should rewind to find the head
                                    if (TryGetFunctionHead(fileAccessor, target.ViewByte, target.ViewByte, xref.Other, out value, out kind))
                                        return true;

                                    kind = default;
                                    value = default;
                                    return false;

                                case ViewByteKind.Data:
                                    if (target.ViewByte->DataKind == ViewByteDataKind.Struct)
                                        kind = MeaningValueKind.StructXRef;
                                    else
                                        kind = MeaningValueKind.ValueXRef;

                                    value = target.ToString();
                                    break;

                                case ViewByteKind.Body:
                                    //We're partway into a value. Rewind to find the head
                                    var p = target.ViewByte;

                                    do
                                    {
                                        if (p->BodyKind == ViewByteBodyKind.SplitHead)
                                        {
                                            throw new NotImplementedException();
                                        }

                                        p--;
                                    } while (p->Kind == ViewByteKind.Body);

                                    //The EndAddress of a RUNTIME_FUNCTION seems to regularly point to padding after the function. Not only that,
                                    //but it might even be pointing to the _second_ 0xCC after the end of the function
                                    var headTarget = fileAccessor.GetEntity(xref.Other - (int) (target.ViewByte - p));

                                    switch (p->Kind)
                                    {
                                        case ViewByteKind.Data:
                                            if (p->DataKind == ViewByteDataKind.Padding)
                                            {
                                                //Try and rewind past the padding to see if we get back into code; if so, see if we can reach the start of a
                                                //function; if so, we'll say that this location is part of the function

                                                do
                                                {
                                                    p--;
                                                } while (p->Kind == ViewByteKind.Data && p->DataKind == ViewByteDataKind.Padding);

                                                //See if we can get a function out of this
                                                if (TryGetFunctionHead(fileAccessor, target.ViewByte, p, xref.Other, out value, out kind))
                                                    return true;
                                            }

                                            value = default;
                                            kind = default;
                                            return false;

                                        default:
                                            throw new NotImplementedException();
                                    }

                                default:
                                    value = default;
                                    kind = default;
                                    return false; //Random bytes after the end of a function?
                            }

                            return true;
                        }
                    }
                }
            }
            else if ((fieldView.Flags & FieldViewFlags.Size) != 0)
            {
                //Get the best value for the size

                double size = fieldView.ValueType switch
                {
                    nameof(Byte) => ((FieldView<byte>) fieldView).Value,
                    nameof(SByte) => ((FieldView<sbyte>) fieldView).Value,
                    nameof(Int16) => ((FieldView<short>) fieldView).Value,
                    nameof(UInt16) => ((FieldView<ushort>) fieldView).Value,
                    nameof(Int32) => ((FieldView<int>) fieldView).Value,
                    nameof(UInt32) => ((FieldView<uint>) fieldView).Value,
                    nameof(Int64) => ((FieldView<long>) fieldView).Value,
                    nameof(UInt64) => ((FieldView<ulong>) fieldView).Value
                };

                if (size == 0)
                {
                    value = default;
                    kind = default;
                    return false;
                }

                using var builder = new ValueStringBuilder();

                builder.AppendSize(size);

                value = builder.ToString();
                kind = MeaningValueKind.Size;
                return true;
            }

            value = default;
            kind = default;
            return false;
        }
            while (true)
            {
                pViewByte--;

                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Code:
                        if (pViewByte->IsFunction)
                        {
                            var length = (int) (pStart - pViewByte);

                            var functionTarget = fileAccessor.GetEntity(originalOffset - length);

                            if (functionTarget.Name.Length > 0)
                            {
                                using var builder = new ValueStringBuilder();
                                builder.Append(functionTarget.Name);
                                builder.Append("+0x");
                                builder.AppendHex((uint) length);
                                value = builder.ToString();
                                kind = MeaningValueKind.FunctionXRef;
                                return true;
                            }

                            value = default;
                            kind = default;
                            return false;
                        }

                        break;

                    case ViewByteKind.Body:
                        break;

                    default:
                        //Maybe it was a function chunk and there's a random 0xCC in the way. We don't currently have a mechanism to lookup what function this chunk belongs to
                        value = default;
                        kind = default;
                        return false;
                }
            }
        }

        //If we are flags, we avoid an allocation by returning the value stored in names
        internal static bool TryGetMultiFlags(Type type, IFieldView fieldView, out string? singleValue, out EnumFlagInfo[]? flags)
        {
            flags = default;

            if (type.GetCustomAttribute<FlagsAttribute>() == null)
            {
                singleValue = fieldView.Value.ToString()!;
                return false;
            }

            var resultValue = Convert.ToUInt64(fieldView.Value);

            //todo: this is bad, this uses reflection. this added 300kb to a simple console app
            var values = Enum.GetValues(type);
            var names = Enum.GetNames(type);

            //We want to align each item such that 0x2022 breaks up into 0x2000, 0x0020 and 0x0002

            var numChars = 1;

            var temp = resultValue;

            while ((temp >>= 4) != 0)
                numChars++;

            //Based on Enum.ToString()

            int index = values.Length - 1;
            while (index >= 0)
            {
                var val = Convert.ToUInt64(values.GetValue(index));

                if (val == resultValue)
                {
                    //We found an exact match, which means we don'th have flags
                    singleValue = names[index];
                    return false;
                }

                if (val < resultValue)
                {
                    break;
                }

                if ((resultValue & currentValue) == currentValue)
                {
                    resultValue -= currentValue;
                    foundItems[foundItemsCount++] = index;
                    resultLength = checked(resultLength + names[index].Length);
                }

                index--;
            }

            //Collect names

            Span<int> foundItems = stackalloc int[64];

            int resultLength = 0, foundItemsCount = 0;
            while (index >= 0)
            {
                ulong currentValue = Convert.ToUInt64(values.GetValue(index));
                if (index == 0 && currentValue == 0)
                {
                    break;
                }

                index--;
            }

            //Return the names to the caller

            if (resultValue != 0)
            {
                //There is a value not defined in the enum

                flags = new EnumFlagInfo[foundItemsCount + 1];
                flags[foundItemsCount] = new EnumFlagInfo(resultValue, null, numChars);
            }
            else
                flags = new EnumFlagInfo[foundItemsCount];

            for (var i = 0; i < foundItemsCount; i++)
            {
                var enumIndex = foundItems[i];

                flags[i] = new EnumFlagInfo(Convert.ToUInt64(values.GetValue(enumIndex)), names[enumIndex], numChars);
            }

            if (resultLength != 0)
            {
                //By default, our values will implicitly be sorted largest to smallest. But when there's a random unaccounted for value,
                //that changes things, so we need to manually sort things to put that value into position
                Array.Sort(flags, (a, b) => -a.Value.CompareTo(b.Value));
            }

            singleValue = default;
            return true;
        }
        internal static string GetHexString(IFieldView fieldView)
        {
            return fieldView.ValueType switch
            {
                nameof(UInt16) => ((FieldView<ushort>) fieldView).Value switch
                {
                    ImageDosHeader.IMAGE_DOS_SIGNATURE => "MZ"
                },
                nameof(Int32) => ((FieldView<int>) fieldView).Value switch
                {
                    (int) ImageNtHeaders.IMAGE_NT_SIGNATURE => "PE00",
                    (int) CodeViewSig.DNRB => "DNRB",
                    (int) CodeViewSig.NB00 => "NB00",
                    (int) CodeViewSig.NB01 => "NB01",
                    (int) CodeViewSig.NB02 => "NB02",
                    (int) CodeViewSig.NB03 => "NB03",
                    (int) CodeViewSig.NB04 => "NB04",
                    (int) CodeViewSig.NB05 => "NB05",
                    (int) CodeViewSig.NB06 => "NB06",
                    (int) CodeViewSig.NB07 => "NB07",
                    (int) CodeViewSig.NB08 => "NB08",
                    (int) CodeViewSig.NB09 => "NB09",
                    (int) CodeViewSig.NB10 => "NB10",
                    (int) CodeViewSig.NB11 => "NB11",
                    (int) CodeViewSig.RSDS => "RSDS"
                }
        protected internal override void VisitStructField(IStructFieldView view)
        {
            WriteLinePrefix(view.Offset);
            _builder.Append(view.Name);
            _builder.Append(" (");
            AppendFormat(view.StructName, ViewByteFormatKind.Symbol);
            _builder.Append(")");
            AppendLine(view.Offset, 0);

            //We're not going to do VisitStruct, so we need to do this in lieu of that
            _path.Push(new EntityState(view.Value));
        }
        protected void WriteLinePrefix(int targetAddress, bool isGlobal = false)
        {
            WriteLinePrefixNoSpace(targetAddress);

            _builder.Append("         ");

            var count = _path.Count;

            //Code always has an entry in the path, which messes up our indentation
            //when writing the header
            if (isGlobal)
                count--;

            for (var i = 0; i < count; i++)
                _builder.Append("    ");
        }

        protected void WriteLinePrefixNoSpace(int targetAddress)
        {
            _builder.Append(_currentSectionName);
            _builder.Append(':');
            _builder.AppendHex((ulong) targetAddress, _addressWidth);
        }

        protected void WriteName(int targetAddress) =>
            WriteName(_fileAccessor.GetName(targetAddress));

        protected void WriteName(FixedUtf8String name)
        {
            if (name.StartsWith("?"))
            {
                var utf8Builder = new Utf8StringBuilder();

                try
                {
                    //If this fails, it just adds the original
                    Demangler.ParseString(name, ref utf8Builder, UNDNAME.UNDNAME_NO_ECSU | UNDNAME.UNDNAME_NO_PTR64);

                    AppendFormat(utf8Builder.ToPointer(), ViewByteFormatKind.Symbol);
                }
                finally
                {
                    utf8Builder.Dispose();
                }
            }
            else
                AppendFormat(name, ViewByteFormatKind.Symbol);
        }

        protected void AppendLine(int startOffset, int length)
        {
            _builder.Append('\n');
            _numLinesWritten++;

            var startTextIndex = 0;
            var startFormatIndex = 0;

            if (_physicalLines.Count > 0)
            {
                var lastLine = _physicalLines[_physicalLines.Count - 1];
                startTextIndex = lastLine.EndTextIndex + 1;

                //If the item has no formats, the start and end will be the same
                startFormatIndex = lastLine.EndFormatIndex;
            }

            var physicalLine = new PhysicalLine(startTextIndex, (_builder.Length - 1), startFormatIndex, _formatRanges.Count)
            {
                StartAddress = startOffset,
                Length = length,

                RelativeIndex = _physicalLines.Count
            };

            _physicalLines.Add(physicalLine);
        }

        protected void AppendFormat(string value, ViewByteFormatKind format)
        {
            var startPos = _builder.Length;
            _builder.Append(value);

            _formatRanges.Add(format, startPos, _builder.Length);
        }

        protected void AppendFormat(FixedUtf8String value, ViewByteFormatKind format)
        {
            var startPos = _builder.Length;
            _builder.Append(value);

            _formatRanges.Add(format, startPos, _builder.Length);
        }
        }

        public void Dispose()
        {
            _builder.Dispose();
        }
    }
}
