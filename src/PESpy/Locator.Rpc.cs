using System;
using System.IO;
using System.Threading.Tasks;

namespace PESpy
{
    public static partial class Locator
    {
        public static unsafe string? LocateRpcServer(Guid interfaceId, string searchPath = "C:\\Windows\\system32")
        {
            var bytes = interfaceId.ToByteArray();

            var files = Directory.EnumerateFiles(searchPath, "*.dll");

            string result = null;

            Parallel.ForEach(files, (file, state) =>
            {
                using var fs = File.OpenRead(file);
                using var mmf = new MemoryMappedFileHolder(fs);

                var index = AppHostSignature.KMPSearch(bytes, mmf.Address, mmf.Length);

                if (index != -1)
                {
                    if (Detector.TryDetectFile(file, mmf, mmf.Length, out var fileKind, out _, out _) && fileKind == FileKind.PE)
                    {
                        using var peFile = new PEFile(file, mmf, 0);

                        var rpcInfo = peFile.RpcInfo;

                        if (rpcInfo != null)
                        {
                            var serverInterfaces = rpcInfo.ServerInterfaces;

                            for (var i = 0; i < serverInterfaces.Length; i++)
                            {
                                ref var serverInterface = ref serverInterfaces[i];

                                if (serverInterface.InterfaceId.SyntaxGUID == interfaceId)
                                {
                                    result = file;
                                    state.Break();
                                }
                            }
                        }
                    }
                }
            });

            return result;
        }
    }
}
