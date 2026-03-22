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
                default:
                    throw new NotImplementedException();
            }
        }
                    }
        }
                            }
                        }
