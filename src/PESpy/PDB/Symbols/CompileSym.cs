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

        /// <inheritdoc cref="COMPILESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COMPILESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        //todo: bitfields

        /// <inheritdoc cref="COMPILESYM.machine"/>
        public short machine => value->machine;

        /// <inheritdoc cref="COMPILESYM.verFEMajor"/>
        public short verFEMajor => value->verFEMajor;

        /// <inheritdoc cref="COMPILESYM.verFEMinor"/>
        public short verFEMinor => value->verFEMinor;

        /// <inheritdoc cref="COMPILESYM.verFEBuild"/>
        public short verFEBuild => value->verFEBuild;

        /// <inheritdoc cref="COMPILESYM.verMajor"/>
        public short verMajor => value->verMajor;

        /// <inheritdoc cref="COMPILESYM.verMinor"/>
        public short verMinor => value->verMinor;

        /// <inheritdoc cref="COMPILESYM.verBuild"/>
        public short verBuild => value->verBuild;

        /// <inheritdoc cref="COMPILESYM.verSt"/>
        public FixedUtf8String verSt => SymType.ReadString(value, value->verSt);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //flags
            sizeof(short)  + //machine
            sizeof(short)  + //verFEMajor
            sizeof(short)  + //verFEMinor
            sizeof(short)  + //verFEBuild
            sizeof(short)  + //verMajor
            sizeof(short)  + //verMinor
            sizeof(short);   //verBuild

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

