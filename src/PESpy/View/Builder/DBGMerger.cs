using System.Collections.Generic;

namespace PESpy.View.Builder
{
    internal class DBGMerger : Merger
    {
        private DBGFile dbgFile;

        public DBGMerger(
            DBGFile dbgFile,
            List<IView> sortedStructs,
            Extension extension) : base(sortedStructs, null, new List<DirectoryInfo>(), extension)
        {
            this.dbgFile = dbgFile;
        }

        internal override IView[] Merge()
        {
            /* There's not much to DBG files. They're literally just
             * - IMAGE_SEPARATE_DEBUG_HEADER
             * - IMAGE_SECTION_HEADER
             * - Exported Names
             * - IMAGE_DEBUG_DIRECTORY
             * - IMAGE_COFF_SYMBOLS_HEADER
             * - Coff Symbol Table
             * - IMAGE_DEBUG_MISC
             * - NB10I
             *
             * and a bit of padding in-between. */

            var length = (int) extension.GetInputLength();

            //We don't wrap this up in a HeaderView or anything; the file format is too simple, just return as is
            var result = BuildSection(0, length, v => v, v => v);

            return result;
        }
    }
}
