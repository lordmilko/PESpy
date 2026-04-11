using System;
using ClrDebug;

namespace PESpy
{
    /* I can't for the life of me figure out how segments in DOS NB05 symbols are supposed to work.
     * I'm not sure if you're supposed to index into your sstSegDef as per https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
     * or what; either way, I just can't figure out how to map segment numbers to physical addresses.
     * CV.EXE and IDA Pro seem to be in sync about the data that is pointed to by certain addresses,
     * but I have no idea how they're doing it. So for now, we will provide a limited set of symbols,
     * wherein we only support displaying symbols that we can be reasonably confident have "simple"
     * addressing schemes, wherein they're in the first segment and there's no frame or offset hijinx
     * that might mess up our calculations
     */

    internal class DOSNB05SymbolAccessor : NB05SymbolAccessor
    {
        public DOSNB05SymbolAccessor(IFile file, IMAGE_FILE_MACHINE machineType) : base(file, machineType)
        {
        }

        public override int? GetRelativeVirtualAddress(ushort seg, int off)
        {
            throw new NotImplementedException("Reading RVAs from DOS segments + offsets is not implemented");
        }
    }
}
