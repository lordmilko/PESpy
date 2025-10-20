using System;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    /* Contrary to https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
     * my observation is that in NB05, symbols are ordered
     * 1. Everything in module 1 (including the sstModule)
     * 2. Everything in module 2 (including the sstModule)
     *
     * etc
     *
     * By contrast, in later codeView versions the ordering is
     *
     * 1. All of the sstModule entries
     * 2. Everything else for module 1
     * 3. Everything else for module 2
     *
     * etc
     */

    //In NB05, sections are ordered
    //https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
    internal class NB05SymbolAccessor : ISymbolAccessor
    {
        protected IFile file;
        internal NB05Data data; //Set after construction
        internal CV_SIGNATURE CvSignature;

        internal CodeViewSig CodeViewSig => data.Signature;

        public bool HasLengthPrefixedStrings { get; set; }

        public NB05SymbolAccessor(IFile file)
        {
            this.file = file;
        }

        public ImageSectionHeader[]? GetSectionHeaders() => file.GetSectionHeaders();

        public virtual SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            /* The ordering of module sections should be
             * 1. sstTypes
             * 2. sstPublics
             * 3. sstSymbols
             * 4. sstSrcModule
             */

            throw new NotImplementedException();
        }

        public virtual TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_ItemId typeIndex)
        {
            throw new NotImplementedException();
        }

        public virtual int? GetRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(GetSectionHeaders(), seg, off);
    }
}
