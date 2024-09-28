using System.Collections.Generic;
using System.Diagnostics;
#if !DEBUG_POSITION
using RVA = System.Int32;
#endif

namespace PESpy
{
    class ExceptionHandlerContext
    {
        private PEFile peFile;
        private bool hasRequestedImports;
        private ImageImportDescriptor[]? descriptors;
        private Dictionary<int, ByteMatchKind?> addressCache = new();

        public ExceptionHandlerContext(PEFile peFile)
        {
            this.peFile = peFile;
        }

        public bool TryGetKind(int virtualAddress, out ByteMatchKind? kind)
        {
            if (addressCache.TryGetValue((int) virtualAddress, out kind))
                return true;

            return false;
        }

        public bool TryGetImport(RVA virtualAddress, out ByteMatchKind kind)
        {
            if (!hasRequestedImports)
            {
                descriptors = peFile.ImportTable;
                hasRequestedImports = true;
            }

            if (descriptors == null)
            {
                kind = default;
                return false;
            }

            if (peFile.TryGetOffset(virtualAddress, out var offset))
            {
                for (var i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];

                    var iat = descriptor.ImportAddressTable.ValueOrDefault;

                    if (iat == null)
                        continue;

                    for (var j = 0; j < iat.Length; j++)
                    {
                        if (iat[j].Offset == offset)
                        {
                            var name = descriptor.ImportLookupTable.Value[j].Name;

                            if (TryGetKindForName(name.Value.Name, out kind))
                                addressCache[(int) virtualAddress] = kind;
                            else
                                addressCache[(int) virtualAddress] = kind;

                            return true;
                        }
                    }
                }
            }

            kind = default;
            return false;
        }

        public void AddMatch(int virtualAddress, ByteMatchKind? kind)
        {
            addressCache[virtualAddress] = kind;
        }

        public bool TryGetSymbol(int virtualAddress, out ByteMatchKind kind)
        {
            if (peFile.Services != null)
            {
                var name = peFile.Services.GetSymbolForAddress(virtualAddress);

                if (name != null && TryGetKindForName(name, out kind))
                {
                    addressCache[(int) virtualAddress] = kind;
                    return true;
                }
            }

            kind = default;
            return false;
        }

        private bool TryGetKindForName(string name, out ByteMatchKind kind)
        {
            switch (name)
            {
                case "__C_specific_handler":
                    kind = ByteMatchKind.__C_specific_handler;
                    break;

                case "__C_specific_handler_noexcept":
                    kind = ByteMatchKind.__C_specific_handler_noexcept;
                    break;

                case "__CxxFrameHandler":
                    kind = ByteMatchKind.__CxxFrameHandler;
                    break;

                case "__CxxFrameHandler3":
                    kind = ByteMatchKind.__CxxFrameHandler3;
                    break;

                case "__CxxFrameHandler4":
                    kind = ByteMatchKind.__CxxFrameHandler4;
                    break;

                case "__GSHandlerCheck":
                    kind = ByteMatchKind.__GSHandlerCheck;
                    break;

                default:
#if DEBUG
                if (name.Contains("C_specific_handler") || name.Contains("CxxFrameHandler") || name.Contains("GSHandlerCheck"))
                    Debug.Assert(false, $"Need to add support for '{name}'");
#endif
                kind = default;
                return false;

            }

            return true;
        }
    }
}
