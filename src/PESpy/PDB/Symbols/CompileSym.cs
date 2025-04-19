using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COMPILESYM"/> structure.
    /// </summary>
    public readonly unsafe struct CompileSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COMPILESYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        //todo: bitfields

        public short machine => value->machine;

        public short verFEMajor => value->verFEMajor;
        public short verFEMinor => value->verFEMinor;
        public short verFEBuild => value->verFEBuild;

        public short verMajor => value->verMajor;
        public short verMinor => value->verMinor;
        public short verBuild => value->verBuild;

        public FixedUtf8String verSt => SymType.ReadString(value, value->verSt);

        internal CompileSym(COMPILESYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return verSt.ToString();
        }
    }
}

