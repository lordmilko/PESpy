using System;
using PESpy.PDB;

namespace PESpy
{
    public struct VftableInfo
    {
        private string _className;

        /// <summary>
        /// Gets the name of the class that this vftable is used by.
        /// </summary>
        public string ClassName
        {
            get
            {
                if (_className == null)
                    Demangler.CrackVftable(_name, out _className, out _targetName);

                return _className;
            }
        }

        private string _targetName;

        /// <summary>
        /// Gets the name of the interface or base class that this vftable provides methods for.
        /// </summary>
        public string? TargetName
        {
            get
            {
                //ClassName _should_ be guaranteed to exist; so better to check if already
                //have than than _targetName

                if (_className == null)
                    Demangler.CrackVftable(_name, out _className, out _targetName);

                return _targetName;
            }
        }

        public NativeSpan<int> Slots32 { get; }

        public NativeSpan<long> Slots64 { get; }

        private SymType[] _slots;

        public SymType[] Slots
        {
            get
            {
                if (_slots == null)
                {
                    if (Slots32.Length > 0)
                    {
                        var slots32 = Slots32.AsSpan();

                        var results = new SymType[Slots32.Length];

                        for (var i = 0; i < Slots32.Length; i++)
                        {
                            var rva = (int) (slots32[i] - _imageBase);

                            if (!_pdbFile.TryGetSymbolByRVA(rva, out var symType, out var disp))
                                throw new NotImplementedException("Don't know how to handle failing to resolve the RVA of a vftable slot to a symbol");

                            results[i] = symType;
                        }

                        _slots = results;
                    }
                    else
                    {
                        var slots64 = Slots64.AsSpan();

                        var results = new SymType[slots64.Length];

                        for (var i = 0; i < slots64.Length; i++)
                        {
                            var rva = (int) (slots64[i] - _imageBase);

                            if (!_pdbFile.TryGetSymbolByRVA(rva, out var symType, out var disp))
                                throw new NotImplementedException("Don't know how to handle failing to resolve the RVA of a vftable slot to a symbol");

                            results[i] = symType;
                        }

                        _slots = results;
                    }
                }

                return _slots;
            }
        }

        private readonly FixedUtf8String _name;
        private readonly long _imageBase;
        private PDBFile _pdbFile;

        internal VftableInfo(
            FixedUtf8String name,
            NativeSpan<int> slots32,
            NativeSpan<long> slots64,
            long imageBase,
            PDBFile pdbFile)
        {
            _name = name;
            Slots32 = slots32;
            Slots64 = slots64;
            _imageBase = imageBase;
            _pdbFile = pdbFile;
        }

        public override string ToString()
        {
            return Demangler.ParseString(_name);
        }
    }
}
