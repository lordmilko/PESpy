using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClrDebug;
using ClrDebug.DIA;
using Iced.Intel;
using PESpy.View;

namespace PESpy
{
    //Represents a visitor capable of constructing a text representation of a given line
    //and pausing/resuming visitation
    public unsafe class ViewByteFormatter : ViewVisitor
    {
        //Stores the path of all "resumable" nodes up to the current point. A node is considered
        //to be "resumable" if has children thus should be interrupted such that it does not spend
        //too much time writing content that may later end up being discarded (e.g. structs, disassembly, etc)
        private Stack<EntityState> _path = new();
        private FileAccessor _fileAccessor;
        private ValueStringBuilder.NonRef _builder;
        private List<PhysicalLine> _physicalLines = new List<PhysicalLine>();
        private List<LogicalLine> _logicalLines = new List<LogicalLine>();
        private readonly IntelAsmWriter _intelAsmWriter;

        private IntelAsmWriter? _dosStubAsmWriter;

        private int _sectionAccessorIndex;
        private string _currentSectionName;
        private ViewByte* _sectionViewByteLimit;
        private ViewByte* _currentGlobalViewByte;
        private int _rva;
        private byte* _pBytes;
        private int _addressWidth;
        private int _numLinesWritten;
        private int _numLinesNeeded;

        private const FieldViewFlags prohibitDecimalFlags = FieldViewFlags.Address | FieldViewFlags.HexString; //Allow size

        internal Direction Direction;

        private string DebuggerDisplay => _builder.ToString();

        internal Span<LogicalLine> PeekLogicalLines()
        {
            FinalizeLogicalLine();

            //We can't clear the list yet; when the elements are reference types, clear will also do Array.Clear
            var span = CollectionsMarshal.AsSpan(_logicalLines);

            return span;
        }

        public void ClearLogicalLines()
        {
            _logicalLines.Clear();
        }

        private void FinalizeLogicalLine()
        {
            //In some circumstances we may need to eagerly build the logical line to retain our depth when walking backwards
            if (_physicalLines.Count == 0)
                return;

            var logicalLine = new LogicalLine(_builder.ToString(), _path.Count, _physicalLines.ToArray()); //todo: find a way to not allocate
            _builder.Clear();
            _physicalLines.Clear();

            _logicalLines.Add(logicalLine);
        }

        public ViewByteFormatter(FileAccessor fileAccessor, ISymbolResolver symbolResolver = null)
        {
            _fileAccessor = fileAccessor;
            _builder = new ValueStringBuilder.NonRef(100);
            _intelAsmWriter = new IntelAsmWriter(fileAccessor.Bitness, symbolResolver);
        }

        private IntelAsmWriter GetDOSStubAsmWriter() =>
            _dosStubAsmWriter ??= new IntelAsmWriter(16, null);
        internal void ClearPath() => _path.Clear();

        #region Control

        //Initializes the visitor to the value at the given address. If the address is partway
        //into a struct, the visitor will automatically initialize the path such that it starts
        //at the value found at the given address.
        public void StartWithOwner(
            int targetAddress, //The address of the first value to print. This may be the start of an entity, the start of a field, or partway into a field
            int ownerAddress, //The top level entity that owns the targetAddress
            int numLinesNeeded)
        {
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
            _sectionViewByteLimit = sectionAccessor.pViewBytes + sectionAccessor.Length;

            if (pViewByte->Kind != ViewByteKind.Body)
            {
                //Easy case: we're already on a point of interest
                StartWithOwner(targetAddress, targetAddress, numLinesNeeded);
                return;
            }

            //We need to rewind to the start of the last global entity, and then fast forward
            //into that to get to the point where the target address starts

            var ownerAddress = targetAddress;

            while (pViewByte->Kind == ViewByteKind.Body)
            {
                ownerAddress--;
                pViewByte--;
            }

            StartWithOwner(targetAddress, ownerAddress, numLinesNeeded);
        }

        private void InitializeSection(in SectionAccessor sectionAccessor, ViewByte* pViewByte)
        {
            _currentSectionName = sectionAccessor.Name;

            _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out var rva, out _);

            _sectionViewByteLimit = sectionAccessor.pViewBytes + sectionAccessor.Length;

            var relativeOffset = (int) (pViewByte - sectionAccessor.pViewBytes);

            _rva = rva + relativeOffset;
            _pBytes = pBytes + relativeOffset;

            _addressWidth = GetAddressWidth((ulong) sectionAccessor.EndAddress);
        }

        internal bool PrepareToFastForward(LogicalLine lastLogicalLine, int numLinesNeeded)
        {
            //We've previously been scrolling up, which means, if the current item in the path is a view,
            //its child index points to the child at the top of the screen. But we're about to start
            //scrolling down, which means we need to adjust the path to show the child that is currently
            //at the bottom of the screen

            Debug.Assert(Direction == Direction.Up);

            //There should always be a depth
            Debug.Assert(lastLogicalLine.Depth > 0);

            Direction = Direction.Down;

            var lastVisibleLine = lastLogicalLine.FirstVisibleLine;

            EntityState current;

            //If we were rewinding previously and popped off the beginning node, our path is now empty
            if (_path.Count == 0 || (current = _path.Peek()).View == null)
            {
                //We're not currently on a view...at the top of the screen. That doesn't tell us what may actually exist at the bottom of the screen!
                //At this point, we've got no-idea what's going on, we need to recompute the path
                _path.Clear();

                StartWithoutOwner(lastVisibleLine.StartAddress, numLinesNeeded);
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
                        //The first visible line corresponds to some field in the current struct. Rewind through the children to find the field that owns us
                        Debug.Assert(current.LastChildIndex != -1);

                        var children = current.Children.Value;

                        for (var i = current.LastChildIndex - 1; i >= 0; i--)
                        {
                            var child = children[i];

                            if (child.Contains(firstVisibleLine.StartAddress))
                            {
                                Debug.Assert(firstLogicalLine.Depth >= _path.Count);

                                if (firstLogicalLine.Depth == _path.Count)
                                {
                                    //We're at the right depth. Update the path to record where the screen is up to
                                    current.LastChildIndex = i;
                                    return false;
                }
                else
                {
                    //Go up one level, and rewind through our ancestor's children to find the one that contains us,
                    //then start digging into them, popping and pushing onto the stack as we go
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
                        current.Children = ((IContainerView) current.View!).Children;

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
                else if (current.Kind == EntityKind.Asm)
                {
                    if (current.IsComplete)
                    {
                        _path.Pop();

                        //If we're messing with the global view byte, we should be top level
                        Debug.Assert(_path.Count == 0);

                        targetAddress = current.StartOffset + current.BytesWritten;
                        lastParentSize = current.BytesWritten;

                        //We don't call IncrementBytes, because we already do that
                        //as we read each instruction, however the increment we do
                        //does not affect the global view byte, so we need to update
                        //that here
                        _currentGlobalViewByte += current.BytesWritten;
                        incrementResult = current.LastIncrementResult;
                    }
                    else
                    {
                        //Continue writing
                        ProcessCode(_currentGlobalViewByte + current.BytesWritten, current.StartOffset + current.BytesWritten, current);

                        return true;
                    }
                }
                else if (current.Kind == EntityKind.Data)
                {
                    //If we're messing with our global positions, we should be top level
                    _path.Pop();
                    Debug.Assert(_path.Count == 0);

                    Debug.Assert(current.IsComplete);
                    targetAddress = current.StartOffset;
                    lastParentSize = current.BytesWritten;
                    incrementResult = IncrementBytes(lastParentSize, ref _currentGlobalViewByte, ref targetAddress);
                }
            switch (incrementResult)
            {
                case IncrementResult.SameSection:
                case IncrementResult.NextSection:
                    //Go next!
                    ProcessViewByte(_currentGlobalViewByte, targetAddress, targetAddress);
                    break;

                default:
                    return false;
            }
            return true;
        }

        internal bool MovePrevious()
        {
            Debug.Assert(Direction == Direction.Up);

            while (_path.Count > 0)
            {
                var current = _path.Peek();

                if (current.Kind == EntityKind.View)
                {
                    if (current.LastChildIndex == -1)
                    {
                        current.Children = ((IContainerView) current.View).Children;
                        current.LastChildIndex = current.Children.Value.Count; //We go +1 past the end because we're about to do -1
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
                        Visit(child);
                        return true;
                    }
        #region ProcessViewByte

        private void ProcessViewByte(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            switch (pViewByte->Kind)
            {
                case ViewByteKind.Code:
                    var state = new EntityState(EntityKind.Asm, ownerAddress);
                    _path.Push(state);

                    //ProcessCode doesn't need to worry about the owner; it just processes global instructions one instruction at a time
                    ProcessCode(pViewByte, targetAddress, state);
                    break;

                case ViewByteKind.Data:
                    ProcessData(pViewByte, targetAddress, ownerAddress);
                    break;

                case ViewByteKind.Unknown:
                    var length = pViewByte->GetUnknownLength(_sectionViewByteLimit);
                    ProcessRawData(pViewByte, targetAddress, ownerAddress, "Unknown", length);
                    break;

                    var children = current.Children.Value;
            }
        }

        //We don't need to pass in owner address; we only support imprecise
        private void ProcessCode(ViewByte* pViewByte, int targetAddress, EntityState state)
        {
            //Write lines until we're told to stop, and then save where we were
            //up to in case the user scrolls further down

            IntelAsmWriter intelAsmWriter = state.IsDOSStub ? GetDOSStubAsmWriter() : _intelAsmWriter;

            if (pViewByte->HasName)
            {
                WriteLinePrefix(targetAddress, true);
                if (_fileAccessor is PEFileAccessor && name == "DOS Stub"u8)
                {
                    //We need a special 16-bit assembly writer!
                    intelAsmWriter = GetDOSStubAsmWriter();
                    state.IsDOSStub = true;

                    _builder.Append(name);
                }
                else
                    WriteName(name);

                AppendLine(targetAddress, 0);
            }

            var bytesWrittenThisRequest = 0;

            while (true)
            {
                if (pViewByte->IsIL)
                    throw new NotImplementedException();

                WriteLinePrefix(targetAddress);
                var bytesWritten = intelAsmWriter.Write(_pBytes, _rva, ref _builder);

                AppendLine(targetAddress + bytesWrittenThisRequest, bytesWritten);
                bytesWrittenThisRequest += bytesWritten;

                //This will handle moving us to the next section and/or checking
                //if we've reached the end
                if ((state.LastIncrementResult = IncrementBytes(bytesWritten, ref pViewByte, ref targetAddress)) == IncrementResult.End)
                {
                    break; //We ran out of bytes!
                }

                //Check this before checking num lines needed, because if we're out of
                //code, there's no point saving our position
                if (pViewByte->Kind != ViewByteKind.Code)
                {
                    state.IsComplete = true;
                    break;
                }

                if (_numLinesWritten >= _numLinesNeeded)
                {
                    break;
                }

                //We're going to write another line, so finalize the current logical line
                FinalizeLogicalLine();
            }

            state.BytesWritten += bytesWrittenThisRequest;
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
                    var length = pViewByte->GetLength(_sectionViewByteLimit);
                    ProcessRawData(pViewByte, targetAddress, ownerAddress, "Padding", length);
                    break;
        private void ProcessStructView(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            Debug.Assert(_rva != -1);

            var kind = _fileAccessor.GetStructKind(ownerAddress);

            IView view = _fileAccessor.GetStructView(targetAddress, kind);

            if (targetAddress != ownerAddress)
            {
                //We're being asked to start partway into a struct. We need to traverse the struct until we get to the field we're after
                //while also updating the path we're up to
                view = BuildPathToAddress(view, targetAddress);
            }

            view.Accept(this);
        }

        //It doesn't matter what the target address is, we only write a single line here
        private void ProcessString(ViewByte* pViewByte, int ownerAddress)
        {
#if DEBUG
            var startLinesWritten = _numLinesWritten;
#endif
            WriteLinePrefix(ownerAddress);

            var length = pViewByte->GetLength(_sectionViewByteLimit);

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

            AppendLine(ownerAddress, length);

#if DEBUG
            var totalLinesWritten = _numLinesWritten - startLinesWritten;
            Debug.Assert(totalLinesWritten == 1, "Writing more than 1 line means we need to take the target address into consideration and start writing from the first line that contains that address");
#endif

            _path.Push(new EntityState(EntityKind.Data, ownerAddress)
            {
                IsComplete = true,
                BytesWritten = length
            });
        }

        private void ProcessNumericData(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            Debug.Assert(targetAddress == ownerAddress, "Starting in the middle is not yet implemented");

            var length = pViewByte->GetLength(_sectionViewByteLimit);

            _path.Push(new EntityState(EntityKind.Data, ownerAddress)
            {
                IsComplete = true,
                BytesWritten = length
            });

            if (pViewByte->HasName)
            {
                WriteLinePrefix(targetAddress, true);

                WriteName(targetAddress);

                AppendLine(targetAddress, 0);
            }

            WriteLinePrefix(targetAddress);

            if (pViewByte->DataKind == ViewByteDataKind.Integer)
            {
                //Based on the length, it's either byte, short, int or long, and then there's also a flag on the ViewByte
                //to tell us whether it's unsigned or not

                _builder.Append("0x");

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

            //The byte is known to be data, it's just the type of the data is unknown

            var length = pViewByte->GetLength(_sectionViewByteLimit);

            _path.Push(new EntityState(EntityKind.Data, ownerAddress)
            {
                IsComplete = true,
                BytesWritten = length
            });

            if (pViewByte->HasName)
            {
                WriteLinePrefix(targetAddress, true);

                WriteName(targetAddress);

                AppendLine(targetAddress, 0);
            }

            WriteBytes(new Span<byte>(_pBytes, length), targetAddress, 4);
        }

        private void ProcessRawData(ViewByte* pViewByte, int targetAddress, int ownerAddress, string name, int length)
        {
            Debug.Assert(targetAddress == ownerAddress, "Starting in the middle is not yet implemented");

            var kind = *_pBytes; //either 0x00 or 0xCC

            WriteLinePrefix(targetAddress);
            _builder.Append(name);
            _builder.Append(" (");
            _builder.Append(length);
            _builder.Append(')');
            AppendLine(targetAddress, length);

            var data = new Span<byte>(_pBytes, length);

            //todo: we can change this to start at the start of the line that the targetaddress belongs to
            _path.Push(new EntityState(EntityKind.Data, ownerAddress)
            {
                IsComplete = true,
                BytesWritten = data.Length,
            });

            const int bytesPerLine = 100;

            WriteBytes(data, targetAddress, 100);
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
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;
                else
                {
                    //It should be the end of the road, and we should contain the offset
                    if (!view.Contains(targetAddress))
                        throw new System.NotImplementedException(); //invalid

                    return view;
                }
            }
        }

        private void WriteBytes(Span<byte> data, int targetAddress, int bytesPerLine)
            for (var i = 0; i < data.Length; i++)
            {
                //Because the 0th element will be on the line, do target - 1
                if ((i % bytesPerLine) == 0)
                {
                    if (i > 0)
                    {
                        //We're at the end of the line, so we need to report what the address at the start of the line was

                        var lineStart = (targetAddress) + (i - bytesPerLine);

                        AppendLine(lineStart, bytesPerLine);
                    }

                    WriteLinePrefix(targetAddress + i);
                }
                else
                {
                    _builder.Append(',');
                }

                _builder.Append("0x");

                var b = data[i];

                //If it's a single character hex digit, add a leading 0
                if (b <= 0xF)
                    _builder.Append('0');

                _builder.AppendHex(b);
            }

            var remainder = data.Length % bytesPerLine;

            if (remainder != 0)
            {
                var numLinesWritten = data.Length / bytesPerLine;

                var lineStart = targetAddress + (numLinesWritten * bytesPerLine);

                //There's leftover bytes on the last line
                AppendLine(lineStart, remainder);
            }
            else
            {
                AppendLine(targetAddress + data.Length - bytesPerLine, bytesPerLine);
            }
        }

        private IncrementResult IncrementBytes(int increment, ref ViewByte* pViewByte, ref int targetAddress)
        {
            pViewByte += increment;
            targetAddress += increment;
            _rva += increment;
            _pBytes += increment;

            if (pViewByte >= _sectionViewByteLimit)
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
        internal static Type FormatField(IFieldView view, FileAccessor fileAccessor, ref ValueStringBuilder.NonRef builder)
        {
        protected internal override void VisitField(IFieldView view)
        {
            WriteLinePrefix(view.Offset);
            _builder.AppendRightPadded(view.Name, 30);
            _builder.Append(" = ");

            FormatField(view, _fileAccessor, ref _builder);

            AppendLine(view.Offset, view.Size);
        }

        internal static Type FormatField(
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

                #endregion
                #region Strings

                case nameof(FixedUtf8String):
                    builder.Append(((FieldView<FixedUtf8String>) view).Value);
                    return typeof(FixedUtf8String);

                case nameof(AnsiString):
                    builder.Append((FixedUtf8String) ((FieldView<AnsiString>) view).Value);
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

                #endregion

                case nameof(Timestamp):
                    var timestamp = ((FieldView<Timestamp>) view).Value;

                    AppendEnumNumeric((uint) timestamp, ref builder);

                    return typeof(Timestamp);

                case nameof(Guid):
                    builder.Append(((FieldView<Guid>) view).Value);
                    return typeof(Guid);

                case "NativeSpan`1":
                    //Not ideal, but just check the various types
                    if (view is FieldView<NativeSpan<short>> @as)
                    {
                        var value = @as.Value;

                        for (var i = 0; i < value.Length; i++)
                        {
                            builder.Append("0x");
                            builder.AppendHex((ushort) value[i]);

                            if (i < value.Length - 1)
                                builder.Append(",");
                        }

                        return typeof(NativeSpan<short>);
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

        private static string[] units =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        //Singletons want to expand enums into multiple rows, but lists want to include the enum description inline
        internal static bool TryGetMeaningIncludeEnum(
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
            out string value,
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

                if (size < 1024)
                {
                    builder.Append((long) size);
                    builder.Append(" B");

                    value = builder.ToString();
                    kind = MeaningValueKind.Size;
                    return true;
                }

                var i = 0;

                while (size >= 1024 && i < units.Length - 1)
                {
                    size /= 1024;
                    i++;
                }
                builder.Append(size.ToString("0.##"));
                builder.Append(' ');
                builder.Append(units[i]);

                value = builder.ToString();
                kind = MeaningValueKind.Size;
                return true;
            }

            value = default;
            kind = default;
            return false;
        }

        private static unsafe bool TryGetFunctionHead(
            FileAccessor fileAccessor,
            ViewByte* pStart,
            ViewByte* pViewByte,
            int originalOffset,
            out string value,
            out MeaningValueKind kind)
        {
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

                if ((resultValue & currentValue) == currentValue)
                {
                    resultValue -= currentValue;
                    foundItems[foundItemsCount++] = index;
                    resultLength = checked(resultLength + names[index].Length);
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
            };
        }
        protected internal override void VisitStruct(IStructView view)
        {
            WriteLinePrefix(view.Offset);
            _builder.Append(view.Name);
            AppendLine(view.Offset, 0);

            _path.Push(new EntityState(view));
        }

        protected internal override void VisitStructField(IStructFieldView view)
        {
            WriteLinePrefix(view.Offset);
            _builder.Append(view.Name);
            _builder.Append(" (");
            _builder.Append(view.StructName);
            _builder.Append(")");
            AppendLine(view.Offset, 0);

            //We're not going to do VisitStruct, so we need to do this in lieu of that
            _path.Push(new EntityState(view.Value));
        }
        private void WriteLinePrefix(int targetAddress, bool isGlobal = false)
        {
            _builder.Append(_currentSectionName);
            _builder.Append(':');
            _builder.AppendHex((ulong) targetAddress, _addressWidth);
            _builder.Append("         ");

            var count = _path.Count;

            //Code always has an entry in the path, which messes up our indentation
            //when writing the header
            if (isGlobal)
                count--;

            for (var i = 0; i < count; i++)
                _builder.Append("    ");
        }

        private void WriteName(int targetAddress) =>
            WriteName(_fileAccessor.GetName(targetAddress));

        private void WriteName(FixedUtf8String name)
        {
            if (name.StartsWith("?"))
            {
                var utf8Builder = new Utf8StringBuilder();

                try
                {
                    //If this fails, it just adds the original
                    Demangler.ParseString(name, ref utf8Builder, UNDNAME.UNDNAME_NO_ECSU | UNDNAME.UNDNAME_NO_PTR64);

                    _builder.Append(utf8Builder.ToPointer());
                }
                finally
                {
                    utf8Builder.Dispose();
                }
            }
            else
                _builder.Append(name);
        }

        private void AppendLine(int startOffset, int length)
        {
            _builder.Append('\n');
            _numLinesWritten++;

            var startIndex = 0;

            if (_physicalLines.Count > 0)
            {
                var lastLine = _physicalLines[_physicalLines.Count - 1];
                startIndex = lastLine.EndIndex + 1;
            }

            var physicalLine = new PhysicalLine(startIndex, (_builder.Length - 1))
            {
                StartAddress = startOffset,
                Length = length,

                RelativeIndex = _physicalLines.Count
            };
            _physicalLines.Add(physicalLine);
        }
        public void Dispose()
        {
            _builder.Dispose();
        }
    }
}
