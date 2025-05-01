using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //Name is made up
    public readonly unsafe struct CxxILHeaderSymbol
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        public SSR SymbolType => *(SSR*) value;

        public short ProductId => *(short*) (value + 1);

        public int ILId => *(int*) (value + 3);

        public int MaxKey => *(int*) (value + 7);

        public byte Translator => *(byte*) (value + 11); //Language? 1 == C++?

        //Flags based on flags & value, where value is:
        //1: hasC9IL = false
        //2: hasLongTypes = true
        //512: hasTypedIL = true
        //hasSecAttributes = (byte)((uint)num >> 4 & 1U) != 0
        public int Attributes => *(byte*) (value + 12);

        public sbyte SmallMajor => *(sbyte*) (value + 16);

        public short LargeMajor
        {
            get
            {
                if (SmallMajor != sbyte.MinValue)
                    return -1;

                return *(short*) (value + 17);
            }
        }

        public sbyte SmallMinor => *(sbyte*) (value + 16 + MajorSpace(this));

        public short LargeMinor
        {
            get
            {
                if (SmallMinor != sbyte.MinValue)
                    return -1;

                return *(short*) (value + 17 + MajorSpace(this));
            }
        }

        public sbyte SmallBuild => *(sbyte*) (value + 16 + MajorSpace(this) + MinorSpace(this));

        public short LargeBuild
        {
            get
            {
                if (SmallBuild != sbyte.MinValue)
                    return -1;

                return *(short*) (value + 17 + MajorSpace(this) + MinorSpace(this));
            }
        }

        public sbyte SmallQFE
        {
            get
            {
                if (ILId < 20070207)
                    return 0;

                return *(sbyte*) (value + 16 + MajorSpace(this) + MinorSpace(this) + BuildSpace(this));
            }
        }

        public short LargeQFE
        {
            get
            {
                if (ILId < 20070207)
                    return 0;

                if (SmallQFE != sbyte.MinValue)
                    return -1;

                return *(byte*) (value + 17 + MajorSpace(this) + MinorSpace(this) + BuildSpace(this));
            }
        }

        public int TrailerOffset
        {
            get
            {
                if (ILId < 20040904)
                    return 0;

                return *(int*) (value + 16 + MajorSpace(this) + MinorSpace(this) + BuildSpace(this) + QFESpace(this));
            }
        }

        public CxxILHeaderSymbol(byte* value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MajorSpace(in CxxILHeaderSymbol symbol)
        {
            if (symbol.SmallMajor == sbyte.MinValue)
                return 3;

            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MinorSpace(in CxxILHeaderSymbol symbol)
        {
            if (symbol.SmallMinor == sbyte.MinValue)
                return 3;

            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BuildSpace(in CxxILHeaderSymbol symbol)
        {
            if (symbol.SmallBuild == sbyte.MinValue)
                return 3;

            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int QFESpace(in CxxILHeaderSymbol symbol)
        {
            if (symbol.SmallQFE == sbyte.MinValue)
                return 3;

            return 1;
        }
    }
}
