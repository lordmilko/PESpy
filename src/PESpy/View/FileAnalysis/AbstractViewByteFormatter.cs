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
            if (_path.Count > 0)
            {
                //We were in the middle of processing something previously; descend up our tree to
                //see if we already have any of the nodes that pertain to the path that will be associated
                //with the given address
                throw new NotImplementedException();
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

                    ownerAddress -= (int) (pViewByte - head);
                    pViewByte = head;

                    //throw new NotImplementedException();
                }

                //if (pViewByte->Kind == ViewByteKind.Code)
                //{
                //    ownerAddress = targetAddress;

                //    //We need to rewind to find the first code instruction that does not have flow from the previous instruction
                //    //to it
                //    while (pViewByte->HasFlow)
                //    {
                //        do
                //        {
                //            ownerAddress--;
                //            pViewByte--;
                //        } while (pViewByte->Kind == ViewByteKind.Body);

                //        Debug.Assert(pViewByte->Kind == ViewByteKind.Code);
                //    }
                //}

                //todo: this is no good, because for code we dont care that we're at the start of an instruction, we need to know if we're at the start of a block!

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

            var lastVisibleLine = lastLogicalLine.LastVisibleLine;

            while (_path.Count > 0)
            {
                var current = _path.Peek();

                if (current.View == null)
                {
                    //We're not currently on a view...at the top of the screen. That doesn't tell us what may actually exist at the bottom of the screen!
                    //At this point, we've got no-idea what's going on, we need to recompute the path

                    _path.Clear();

                    StartWithoutOwner(lastVisibleLine.EndAddress, numLinesNeeded); //EndAddress is +1 past the end
                    return true;
                }

                if (current.View.Contains(lastVisibleLine.StartAddress))
                {
                    //Construct the path up to the last logical line

                    while (_path.Count < lastLogicalLine.Depth)
                    {
                        IView view = current.View;

                        if (view is IStructFieldView s)
                            view = s.Value;
                        else if (view is IStructArrayFieldView)
                            throw new NotImplementedException();

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
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    return false;

                    if (current.LastChildIndex != -1)
                    {
                        //Go to the previous child and dig into the end of it

                        current.LastChildIndex--;

                        //var child = current.Children.Value[current.LastChildIndex];

                        //while (true)
                        //{
                        //    if (child is IContainerView c)
                        //    {

                        //    }
                        //    else
                        //    {
                        //        _path.Push()
                        //    }
                        //}
                        return false;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    //Go up one level and see if they contain us instead
                    _path.Pop();
                }
            }

            //todo: in both fast forward and rewind, what if the first or last line is something with length 0.
            //its the last visible line, but we dont want the last visible line, we want the last line of the logical line
            //cos thats where we need to resume from?

            //Ran out of path. Just goto instead

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
                        {
                            //This is the thing that's at the top of the screen, and we want to start _before_ this

                            if (_path.Count == 1)
                            {
                                //The thing that's at the top of the screen is a top level struct; we don't know what comes before it,
                                //so we need to start without owner
                                _path.Clear();

                                StartWithoutOwner(firstVisibleLine.StartAddress - 1, numLinesNeeded);
                                return true;
                            }

                            throw new NotImplementedException(); //todo: not sure what to do

                            //Whatever current is, is the point we should start from
                            if (current.LastChildIndex != -1)
                                current.LastChildIndex = 0; //We'll rewind past this when we MovePrevious

                            return false;
                        }
                        else if (firstLogicalLine.Depth > _path.Count)
                        {
                            do
                            {
                                //Dig into the fields to get to the required depth
                                Debug.Assert(current.LastChildIndex != -1);

                                current.LastChildIndex = 0;

                                var nextChild = current.Children.Value[0];

                                //todo: do we care if the child is an istructfieldview or istructfieldarrayview?

                                if (nextChild is IContainerView c)
                                {
                                    var children = c.Children;

                                    current = new EntityState(nextChild, true)
                                    {
                                        LastChildIndex = 0,
                                        Children = c.Children,
                                    };
                                }
                                else
                                {
                                    current = new EntityState(nextChild, false);
                                }

                                _path.Push(current);
                            } while (firstLogicalLine.Depth > _path.Count);

                            return false;
                        }
                        else //firstLogicalLine.Depth < _path.Count
                        {
                            //We're too deep; we need to pop items until we get to the required depth

                            throw new NotImplementedException();
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

                                    //child = ((IContainerView) child).Children[0];

                                    current.LastChildIndex = i;

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
                                        else
                                        {
                                            throw new NotImplementedException();
                                        }
                                    }

                                    //Continue digging ino the first node of thils child until we're at the target depth
                                    //do
                                    //{
                                    //    if (child is IContainerView c)
                                    //    {
                                    //        var nextChildren = c.Children;

                                    //        _path.Push(new EntityState(child, true)
                                    //        {
                                    //            Children = nextChildren,
                                    //            LastChildIndex = nextChildren.Count - 1
                                    //        });
                                    //        child = nextChildren[nextChildren.Count - 1];
                                    //    }
                                    //    else
                                    //    {
                                    //        _path.Push(new EntityState(child, false));
                                    //    }
                                    //} while (_path.Count < firstLogicalLine.Depth);

                                    return false;
                                }

                                throw new NotImplementedException();
                            }
                        }

                        throw new NotImplementedException();

                        //Using the depth of the top visible line, dig into the field until we arrive at the target line
                    }
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
                {
                    throw new NotImplementedException();

                    //If we're messing with our global positions, we should be top level
                    _path.Pop();
                    Debug.Assert(_path.Count == 0);

                    Debug.Assert(current.IsAtEnd);
                    targetAddress = current.StartOffset;
                    lastParentSize = current.BytesWritten;
                    incrementResult = IncrementBytes(lastParentSize, ref _currentGlobalViewByte, ref targetAddress);
                }
                else
                {
                    throw new NotImplementedException();
                }
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



            //todo: this is not efficient. at the end of a given value we should save the viewbyte,
            //then we can just do ++

            //Try and tee up the next child after the last top level parent using StartExact
            //StartExact(lastParent.View.Offset + lastParent.View.Size);

            return true;
        }

        internal bool MovePrevious()
        {
            Debug.Assert(Direction == Direction.Up);

            if (_path.Count == 0)
            {
                //If we've been scrolling up, and just wrote the head of a top level struct, we've got nothing in our path
                //anymore, so we need to ask what the previous top level entity is

                ref var sectionAccessor = ref _fileAccessor.SectionAccessors[_sectionAccessorIndex];

                //currentGlobalViewByte represents the top level entity whose path we're currently drawing. Thus,
                //if we're moving to the previous entity, the currentGlobalViewByte should be set to their head, and
                //the path should be set to the deepest, furthest node within that entity

                if (_currentGlobalViewByte == sectionAccessor.pViewBytes)
                {
                    //We've hit the start of the current section; move to the previous one

                    if (_sectionAccessorIndex == 0)
                        return false; //Can't go any further back

                    //todo: go back and update all global state
                    throw new NotImplementedException();
                }

                var targetAddress = (int) (_currentGlobalViewByte - sectionAccessor.pViewBytes) - 1;

                StartWithoutOwner(targetAddress, 1);

                return true;
            }

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
                    }
                }
                else if (current.Kind == EntityKind.Asm || current.Kind == EntityKind.Data)
                {
                    if (current.IsAtStart)
                    {
                        //We've run out of data or code. Update _currentGlobalViewByte like we do in MoveNext
                        //_currentGlobalViewByte = current.ReverseViewByte;

                        //todo: what if we hit the start of a section and cant go back any further
                        //todo: update rva?
                        //todo: are we updating sectionaccessorindex ourselves?
                        //todo: update pbytes? or we're already handling this?

                        Debug.Assert(_path.Count == 1);
                        _path.Pop();

                        //We've already rewound to the start of the previous item in ReverseResumeAddress. The thing is however, that thing
                        //could potentially be a multi-line value

                        //todo: what if we hit the beginning of a section
                        //StartWithoutOwner(current.ReverseResumeAddress, 1);
                        StartWithoutOwner(current.StartOffset - 1, 1);
                        return true;
                    }
                    else
                    {
                        //Continue writing

                        //todo: i managed to break this scrolling up and down bit by bit around the rich header.
                        //the dos string got down this pathway

                        Debug.Assert(current.IsStreamer);

                        if (current.Kind == EntityKind.Asm)
                            ProcessCode(current.ReverseViewByte, current.ReverseResumeAddress, current);
                        else
                            ProcessRawData(current.ReverseViewByte, current.ReverseResumeAddress, current.StartOffset, current.DataName, current.DataLength, current);

                        return true;
                    }
                }
                else
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
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

                default:
                    throw new NotImplementedException();
            }
        }

        private void ProcessStructView(ViewByte* pViewByte, int targetAddress, int ownerAddress)
        {
            Debug.Assert(_rva != -1);

            var kind = _fileAccessor.GetStructKind(ownerAddress);

            IView view = _fileAccessor.GetStructView(targetAddress, kind);

            //todo: if we're rewinding, then when we build the path what we actually need to do is build the _deepest_ path to the given address.
            //if we're going forwards, build the shallowest path

            if (targetAddress != ownerAddress)
            {
                //We're being asked to start partway into a struct. We need to traverse the struct until we get to the field we're after
                //while also updating the path we're up to
                view = BuildPathToAddress(view, targetAddress);

                if (Direction == Direction.Up)
                {
                    //If we're rewinding, and are being asked to start from the end of a struct, we need to
                    //modify our path construction and ensure that we dig into the deepest field at the end of the struct, and
                    //start going backwards from there
                    Debug.Assert(!(view is IStructView));
                }
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

            WriteLinePrefix(targetAddress);

            if (pViewByte->DataKind == ViewByteDataKind.Integer)
            {
                //Based on the length, it's either byte, short, int or long, and then there's also a flag on the ViewByte
                //to tell us whether it's unsigned or not

                //_builder.Append("0x");

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

            //The byte is known to be data, it's just the type of the data is unknown

            var length = pViewByte->GetLength(_sectionViewByteEnd);

            //todo: we can change this to start at the start of the line that the targetaddress belongs to
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

            WriteBytes_old(new Span<byte>(_pBytes, length), targetAddress, 4);
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
                    else
                    {
                        Debug.Assert(false); //We should either be starting from unknown or padding
                    }
                }
                else
                {
                    //Move to the value prior to the instruction we just wrote
                    if (!DecrementBytes(ref pViewByte, ref targetAddress))
                    {
                        state.IsAtStart = true;
                        break;
                    }

                    //if (bytesWrittenThisRequest == remaining)
                    //    break; //We hit the end

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

            return;

            var kind = *_pBytes; //either 0x00 or 0xCC

            int effectiveLength;
            int startOffset = 0;
            bool isComplete;

            if (targetAddress == ownerAddress)
            {
                var startPos = WriteDividerPrefix(targetAddress);
                _builder.Append(name);
                _builder.Append(" (");
                _builder.Append(length);
                _builder.Append(')');
                WriteDividerSuffix(targetAddress, startPos);

                effectiveLength = length;
                startOffset = 0;
                isComplete = true;
            }
            else
            {
                var off = targetAddress - ownerAddress;
                var lineNum = off / bytesPerLine;
                startOffset = lineNum * bytesPerLine;
                effectiveLength = Math.Min(bytesPerLine, length - startOffset);

                isComplete = effectiveLength == length;
            }

            var data = new Span<byte>(_pBytes + startOffset, length);

            //todo: we can change this to start at the start of the line that the targetaddress belongs to
            _path.Push(new EntityState(EntityKind.Data, pViewByte, ownerAddress + startOffset)
            {
                IsAtEnd = isComplete,
                BytesWritten = effectiveLength,
            });

            WriteBytes_old(data, targetAddress, bytesPerLine);

            if (NeedTrailingLine(pViewByte + effectiveLength))
                WriteDivider(ownerAddress + startOffset + effectiveLength);

            //We don't write a line before the start of structs, so 

            //WriteLine(targetAddress + data.Length);

            //todo: need to support suspend and resume
        }

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

        //todo: our lines are no good, we're doubling up and also we need our xfg bytes to
        //be included in the function area

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
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;

                    throw new NotImplementedException(); //this should be impossible
                }
                else if (view is IStructFieldView v)
                {
                    //Unlike when visiting, here we _will_ inspect the IStructView when the loop repeats, so we shouldn't add it to the path

                    view = v.Value;
                    continue;
                }
                else
                {
                    //It should be the end of the road, and we should contain the offset
                    if (!view.Contains(targetAddress))
                        throw new System.NotImplementedException(); //invalid

                    return view;
                }
            }
        }

        private void BuildForwardPathToDepth(EntityState current, int targetAddress, int targetDepth)
        {
            throw new NotImplementedException();
        }

        private void BuildReversePathToDepth()
        {
            throw new NotImplementedException();
        }

        private void WriteBytes_old(Span<byte> data, int targetAddress, int bytesPerLine)
        {
            //todo: need to support suspend and resume, we could potentially have a lot
            //of padding!

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

                var startPos = _builder.Length;

                _builder.Append("0x");

                var b = data[i];

                //If it's a single character hex digit, add a leading 0
                if (b <= 0xF)
                    _builder.Append('0');

                _builder.AppendHex(b);

                //todo: the format from a previous line is bleeding into this and affecting
                //the formatting
                _formatRanges.Add(ViewByteFormatKind.Byte, startPos, _builder.Length);
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

        #region Visitor

        protected internal override void VisitAsm(IAsmView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitBitField(IBitFieldView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitByteBlob(ByteBlobView view)
        {
            throw new NotImplementedException();
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

                case nameof(IMAGE_GUARD):
                    //var imageGuard = ((FieldView<IMAGE_GUARD>) view).Value;
                    //var flags = imageGuard & ~IMAGE_GUARD.IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK;
                    //var metadataSize = (byte) ((int) (imageGuard & IMAGE_GUARD.IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
                    //builder.Append(flags.ToString());
                    //AppendEnumNumeric((ushort) flags, ref builder);
                    //builder.Append("Metadata Size: ");
                    //builder.Append(metadataSize);
                    WriteEnumUInt32<IMAGE_GUARD>(view, ref builder);
                    return typeof(IMAGE_GUARD);

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

                    //If we're reproducible, this isn't a timestamp but rather a hash (?) or something to do with reproducibility

                    //builder.Append(timestamp.ToString());

                    //if (timestamp != 0)
                        

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
                    }
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
                    else
                        throw new NotImplementedException();

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

        private static unsafe bool TryGetFunctionHead(
            FileAccessor fileAccessor,
            ViewByte* pStart,
            ViewByte* pViewByte,
            int originalOffset,
            out string? value,
            out MeaningValueKind kind)
        {
            //todo: this is not safe, we really need to be told the beginning of the section.
            //we couldve discovered some random snippet of code not marked as a function which is the first
            //thing in the section

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

        #endregion

        private static void WriteEnumByte<TEnum>(IFieldView view, ref ValueStringBuilder.NonRef builder) where TEnum : System.Enum
        {
            var enumValue = ((FieldView<TEnum>) view).Value;
            //builder.Append(enumValue.ToString());

            var underlying = Unsafe.As<TEnum, byte>(ref enumValue);
            AppendEnumNumeric(underlying, ref builder);
        }

        private static void WriteEnumUInt16<TEnum>(IFieldView view, ref ValueStringBuilder.NonRef builder) where TEnum : System.Enum
        {
            var enumValue = ((FieldView<TEnum>) view).Value;
            //builder.Append(enumValue.ToString());

            var underlying = Unsafe.As<TEnum, ushort>(ref enumValue);
            AppendEnumNumeric(underlying, ref builder);
        }

        private static void WriteEnumUInt32<TEnum>(IFieldView view, ref ValueStringBuilder.NonRef builder) where TEnum : System.Enum
        {
            var enumValue = ((FieldView<TEnum>) view).Value;
            //builder.Append(enumValue.ToString());

            var underlying = Unsafe.As<TEnum, uint>(ref enumValue);
            AppendEnumNumeric(underlying, ref builder);
        }

        private static void AppendEnumNumeric(ulong value, ref ValueStringBuilder.NonRef builder)
        {
            //builder.Append(" (");
            builder.Append("0x");
            builder.AppendHex(value);
            //builder.Append(")");
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

        protected internal override void VisitHeader(HeaderView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitLogicalRegion(LogicalRegionView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitOverlay(OverlayView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitFile(FileView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitSection(SectionView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitStruct(IStructView view)
        {
            //Derived methods must call the base method to update the _path
            _path.Push(new EntityState(view));
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

        protected internal override void VisitStructArrayField(IStructArrayFieldView view)
        {
            throw new NotImplementedException();
        }

        protected internal override void VisitValue(IValueView view)
        {
            throw new NotImplementedException();
        }

        #endregion

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
                    Demangler.WriteString(name, ref utf8Builder, UNDNAME.UNDNAME_NO_ECSU | UNDNAME.UNDNAME_NO_PTR64);

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

        private static void WriteXRef(int offset, ulong target, FileAccessor fileAccessor, ref ValueStringBuilder.NonRef builder)
        {
            return;

            //If something says it's an address, there had better be an XRef coming out of it!

            if (target == 0)
                throw new NotImplementedException(); //OK, it's not valid

            //todo: what about if its a totally invalid address, or points to an area of a segment that only exists in memory?
            var pViewByte = fileAccessor.GetViewByte(offset, out _);

            if (!pViewByte->HasXRefs)
                return;

            var xrefs = fileAccessor.GetXRefs(offset);

            //A given entity should have a single from xref and potentially multiple two xrefs
            foreach (var xref in xrefs)
            {
                if (xref.Kind == XRefKind.From)
                {
                    //What is the "thing" that this xref points to?
                    var entity = fileAccessor.GetEntity(xref.Other);

                    builder.Append(" -> ");

                    entity.ToString(ref builder);

                    return;
                }
            }

            //Didn't find a From xref
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _builder.Dispose();
        }
    }
}
