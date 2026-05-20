using System;
using ClrDebug;
using PESpy.LIB;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy.View
{
    internal class LIBFileAccessor : FileAccessor
    {
        public LIBFile LIBFile { get; }

        public LIBFileAccessor(LIBFile libFile) : base(libFile, GetBitness(libFile))
        {
            LIBFile = libFile;
            FileViewKind = ViewKind.LIBFile;
        }

        private static int GetBitness(LIBFile libFile)
        {
            //The LIB file does not have a bitness, but the OBJ members inside it do. We assume that all
            //OBJ members will have the same bitness
            var importLibrary = libFile.ImportLibrary;

            if (importLibrary.Length == 0)
                return 32; //We're not going to have any code anyway, so it doesn't matter

            //If we didn't have any long members, then everything is referred to in simple imports
            //and this doesn't really matter; we assume 32-bit in this case
            IMAGE_FILE_MACHINE machine = IMAGE_FILE_MACHINE_UNKNOWN;

            foreach (var item in importLibrary)
            {
                if (item.IsLong)
                {
                    var l = (LongImportLibraryMember) item;

                    var myMachine = l.FileHeader.Machine;

                    if (machine == IMAGE_FILE_MACHINE_UNKNOWN)
                        machine = myMachine;
                    else if (myMachine != machine)
                        throw new System.NotImplementedException(); //multiple machine types
                }
            }

            return GetBitness(machine);
        }

        protected override object CreateOverview()
        {
            throw new NotImplementedException();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new LIBFileViewWriterHelper(LIBFile),
                    LIBFile.CreateByteViewProvider(this),
                    fileAccessor: this
                );

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }
    }
}
