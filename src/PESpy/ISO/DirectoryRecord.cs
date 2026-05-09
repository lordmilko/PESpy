using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PESpy.ISO
{
    internal unsafe class ISOContext
    {
        public byte* pISO { get; init; }
        public int LogicalBlockSize { get; init; }
        public Encoding Encoding { get; init; }
    }

    public readonly unsafe struct DirectoryRecord
    {
        public byte Length { get; }

        public byte ExtendedAttributeRecordLength { get; }

        public uint LocationOfExtent { get; }

        public uint DataLength { get; }

        public DateTime? RecordingDateTime { get; }

        public FileFlags FileFlags { get; }

        public byte FileUnitSize { get; }

        public byte InterleaveGapSize { get; }

        public ushort VolumeSequenceNumber { get; }

        public byte LengthOfFileIdentifier { get; }

        public string FileIdentifier { get; }

        public byte[] SystemUseData { get; }

        public NativeSpan<byte> Bytes => new NativeSpan<byte>(_context.pISO + (_context.LogicalBlockSize * LocationOfExtent), (int) DataLength);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private DirectoryEntryIterator Entries
        {
            get
            {
                if ((FileFlags & FileFlags.Directory) == 0)
                    return default;

                return new DirectoryEntryIterator(_context, LocationOfExtent, DataLength);
            }
        }

        public string FullPath
        {
            get
            {
                using var list = new ValueList<string>();

                DirectoryRecord? current = this;

                while (current != null)
                {
                    list.Add(current.Value.FileIdentifier);
                    current = current.Value.Parent;
                }

                using var builder = new ValueStringBuilder();

                builder.Append('.');
                builder.Append(Path.DirectorySeparatorChar);

                for (var i = list.Count - 1; i >= 0; i--)
                {
                    builder.Append(list[i]);

                    if (i > 0)
                        builder.Append(Path.DirectorySeparatorChar);
                }

                return builder.ToString();
            }
        }

        public DirectoryRecord? Parent
        {
            get
            {
                //We can only store our parent extent when traversing downwards.
                //When traversing back upwards, we don't know what our parent's
                //parent's extent is. But that's OK, because in that scenario we're
                //a directory anyway and so can simply ask our second child which
                //stores our parent directly

                DirectoryRecord parent;

                if (_parentExtent == 0)
                {
                    Debug.Assert((FileFlags & FileFlags.Directory) != 0);

                    var iterator = Entries.GetEnumerator();

                    if (!iterator.MoveNext() || !iterator.MoveNext())
                        throw new NotImplementedException();

                    parent = iterator.Current;
                }
                else
                {
                    var pParent = _context.pISO + (_context.LogicalBlockSize * _parentExtent);
                    var reader = new ISOByteReader(pParent, _context.LogicalBlockSize);

                    parent = new DirectoryRecord(ref reader, _context, 0);
                }

                //The record that Parent points to describes itself as "self" (\0)
                //as well. This is not very helpful! So what we need to do then is get
                //our _grandparent_, and then find the record under that that contains
                //the same extent as our parent, and that will give us a DirectoryRecord
                //that actually contains a name
                var grandParentIterator = parent.Entries.GetEnumerator();

                if (!grandParentIterator.MoveNext() || !grandParentIterator.MoveNext())
                    throw new NotImplementedException();

                var grandParent = grandParentIterator.Current;

                foreach (var entry in grandParent.Entries)
                {
                    if (entry.LocationOfExtent == parent.LocationOfExtent)
                    {
                        if (entry.FileIdentifier == "\0")
                            return null; //The parent is the root

                        return entry;
                    }
                }

                return null;
            }
        }

        public IEnumerable<DirectoryRecord> EnumerateFiles(bool recurse)
        {
            foreach (var entry in Entries)
            {
                if ((entry.FileFlags & FileFlags.Directory) == 0)
                    yield return entry;
                else
                {
                    if (recurse)
                    {
                        switch (entry.FileIdentifier)
                        {
                            case "\0": //Self
                            case "\u0001": //Parent
                                continue;
                        }

                        Debug.Assert(entry.FileIdentifier != null);

                        foreach (var grandChild in entry.EnumerateFiles(recurse))
                            yield return grandChild;
                    }
                }
            }
        }

        private readonly ISOContext _context;

        //Note: since files don't store any entries in them, you can't
        //locate their parents to get their full path! Thus, we store this
        //to enable constructing the full path
        private readonly uint _parentExtent;

        internal DirectoryRecord(
            ref ISOByteReader reader,
            ISOContext context,
            uint parentExtent)
        {
            _context = context;
            _parentExtent = parentExtent;

            //10.1 (pdf page 47)

            Length = reader.ReadByte();
            ExtendedAttributeRecordLength = reader.ReadByte();
            LocationOfExtent = reader.ReadBothUInt32(start: 3, end: 10);
            DataLength = reader.ReadBothUInt32(start: 11, end: 18);
            RecordingDateTime = reader.ReadDateTime(start: 19, end: 25);
            FileFlags = (FileFlags) reader.ReadByte();
            FileUnitSize = reader.ReadByte();
            InterleaveGapSize = reader.ReadByte();
            VolumeSequenceNumber = reader.ReadBothUInt16(start: 29, end: 32);
            LengthOfFileIdentifier = reader.ReadByte();

            var bytesUsed = 33 + LengthOfFileIdentifier;

            FileIdentifier = reader.ReadChars(34, bytesUsed, context.Encoding); //The root directory is identified by "\0" (8.6.2)

            Debug.Assert(FileIdentifier != null);

            //There is a single byte of padding if the length is _even-
            if ((LengthOfFileIdentifier & 1) == 0)
            {
                reader.SkipByte();
                bytesUsed++;
            }

            var remaining = Length - bytesUsed;

            if (remaining > 0)
            {
                //Following this, there may potentially be "SystemUseData"
                SystemUseData = reader.ReadArray(remaining);
            }
        }

        public override string ToString()
        {
            if (FileIdentifier == "\0")
                return "<Self>";

            if (FileIdentifier == "\u0001")
                return "<Parent>";

            return FileIdentifier;
        }
    }
}
