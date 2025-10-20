using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PESpy.View
{
    public unsafe abstract class FileAnalyzer
    {
        #region Static

        public static bool TryAnalyze(
            string fileName,
            out FileAccessor fileAccessor,
            IFileDisassembler disassembler = null,
            IFileAnalyzerProgress? progress = null)
        {
            if (TryAnalyzeInternal(fileName, out fileAccessor, disassembler, progress))
            {
                //Cleanup the objects on the LOH that were allocated during analysis
                GC.Collect();
                return true;
            }

            fileAccessor = default;
            return false;
        }

        private static bool TryAnalyzeInternal(
            string fileName,
            out FileAccessor fileAccessor,
            IFileDisassembler disassembler,
            IFileAnalyzerProgress? progress)
        {

            if (Detector.TryOpenFile(fileName, out var file))
            {
                try
                {
                    var fileAnalyzer = file.Kind switch
                    {
                        FileKind.PE => new PEFileAnalyzer(new PEFileAccessor((PEFile) file), disassembler, progress),
                        //FileKind.NE => new NEFileAnalyzer(new NEFileAccessor((NEFile) file), disassembler, progress),
                        //FileKind.LE => new LEFileAnalyzer(new LEFileAccessor((LEFile) file), disassembler, progress),
                        //FileKind.DOS => new DOSFileAnalyzer(new DOSFileAccessor((DOSFile) file), disassembler, progress),
                        //FileKind.DBG => new DBGFileAnalyzer(new DBGFileAccessor((DBGFile) file), disassembler, progress),
                        //FileKind.PDB => new PDBFileAnalyzer(new PDBFileAccessor((PDBFile) file), disassembler, progress),
                        //FileKind.PortablePDB => new PortablePDBFileAnalyzer(new PortablePDBFileAccessor((PortablePDBFile) file), disassembler, progress),
                        //FileKind.OBJ => new OBJFileAnalyzer(new OBJFileAccessor((OBJFile) file), disassembler, progress),
                        //FileKind.LIB => new LIBFileAnalyzer(new LIBFileAccessor((LIBFile) file), disassembler, progress),
                        //FileKind.OMF => new OMFFileAnalyzer(new OMFFileAccessor((OMFFile) file), disassembler, progress),
                        //FileKind.OMFLIB => new OMFLIBFileAnalyzer(new OMFLIBFileAccessor((OMFLIBFile) file), disassembler, progress),
                        _ => throw new NotImplementedException($"Don't know how to open a file of type '{file.Kind}'")
                    };

                    fileAccessor = fileAnalyzer.Execute();
                    return true;
                }
                catch
                {
                    file.Dispose();

                    throw;
                }
            }
            else
            {
                fileAccessor = default!;
                return false;
            }
        }

        #endregion

        protected readonly FileAccessor _fileAccessor;
        protected readonly IFileDisassembler? _fileDisassembler;
        private readonly HashSet<int> _queuedAddresses = new HashSet<int>();

        //The limiting factor on performance now seems to be lock contention. I tried to do away with the lock and use
        //concurrent queue instead (forgetting about checking whether an address has already been processed or not) but that
        //ended up being slower. Checking the Usage before adding to candidate additions also didn't help
        private readonly Queue<WorkItem> _globalWorkQueue = new Queue<WorkItem>();
        private readonly object _globalWorkQueueLock = new object();

        protected FileAnalyzer(FileAccessor fileAccessor, IFileDisassembler? fileDisassembler)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = fileDisassembler;
        }

        public abstract FileAccessor Execute();

        //AddCode can't be on the FileAccessor because it needs to interact with members specific to performing analysis

        //The value we're being passed is definitely an RVA. If the PE File is not loaded, we need to convert the RVA to a physical offset
        protected ViewByte* AddCode(int address, int rva, int sectionIndex)
        {
            var pViewByte = _fileAccessor.GetViewByteForSection(address, sectionIndex);

            if (!_queuedAddresses.Add(address))
                return pViewByte; //Already seen

            //Don't mark it as code yet; we'll mark it as code if we successfully disassemble it. Our code reader
            //also asserts that anything it tries to disassemble into has status Unknown
            EnqueueWork(address, address, rva);

            return pViewByte;
        }

        private void EnqueueWork(int owner, int address, int rva)
        {
#if DEBUG
            Debug.Assert(owner != 0);
            Debug.Assert(address != 0);

            //Object files have not been laid out yet, so the concept of an RVA is meaningless
            //if (this is not OBJFileAnalyzer)
            //    Debug.Assert(rva != 0);
#endif

            _globalWorkQueue.Enqueue(new WorkItem(owner, address, rva));
        }

        #region WorkDisasmQueue

        protected void WorkDisasmQueue()
        {
            var disassembler = _fileDisassembler;

            if (disassembler == null)
                return;

            //var numThreads = Environment.ProcessorCount;
            var numThreads = 1;

            var threads = new Thread[numThreads];
            for (var i = 0; i < threads.Length; i++)
            {
                var thread = new Thread(() => disassembler.WorkThreadProc(_fileAccessor, _globalWorkQueue, _globalWorkQueueLock, numThreads))
                {
                    Name = $"Disasm {i}",
                    IsBackground = true
                };

                thread.Start();

                threads[i] = thread;
            }

            //Now just wait for all threads to finish
            foreach (var thread in threads)
                thread.Join();
        }

        #endregion
        #region MarkPadding

        protected void MarkPadding()
        {
            var sectionAccessors = _fileAccessor.SectionAccessors;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
                    if (pViewByte->Kind == ViewByteKind.Unknown)
                    {
                        switch (*pBytes)
                        {
                            case 0xCC:
                                pViewByte->Kind = ViewByteKind.Data;
                                pViewByte->DataKind = ViewByteDataKind.Padding;
                                pViewByte++;
                                pBytes++;

                                while (pViewByte < pEnd)
                                {
                                    if (pViewByte->Kind == ViewByteKind.Unknown && *pBytes == 0xCC)
                                    {
                                        pViewByte->Kind = ViewByteKind.Body;
                                        pViewByte++;
                                        pBytes++;
                                    }
                                    else
                                        break;
                                }
                                break;

                            case 0x00:
                                //We will treat 0 as padding when there are at least 4 0's in a row. This means that it's not a trailing 0 and a null terminator
                                //from a UTF-16 string
                                if (pViewByte + 3 < pEnd && pBytes[1] == 0 && pBytes[2] == 0 && pBytes[3] == 0)
                                {
                                    pViewByte->Kind = ViewByteKind.Data;
                                    pViewByte->DataKind = ViewByteDataKind.Padding;
                                    pViewByte++;
                                    pBytes++;

                                    while (pViewByte < pEnd)
                                    {
                                        if (pViewByte->Kind == ViewByteKind.Unknown && *pBytes == 0)
                                        {
                                            pViewByte->Kind = ViewByteKind.Body;
                                            pViewByte++;
                                            pBytes++;
                                        }
                                        else
                                            break;
                                    }
                                }
                                else
                                {
                                    pViewByte++;
                                    pBytes++;
                                }
                                break;

                            default:
                                pViewByte++;
                                pBytes++;
                                break;
                        }
                    }
                    else
                    {
                        pViewByte++;
                        pBytes++;
                    }
                }
            }
        }

        #endregion
    }
}
