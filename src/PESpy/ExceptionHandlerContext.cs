using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy
{
    internal class ExceptionHandlerContext
    {
        private PEFile peFile;
        private bool hasRequestedImports;
        private ImageImportDescriptor[]? imports;
        private Dictionary<int, WellKnownExceptionHandlerKind?> addressCache = new();

        public ExceptionHandlerContext(PEFile peFile)
        {
            this.peFile = peFile;
        }

        public bool TryGetKind(int rva, out WellKnownExceptionHandlerKind? kind)
        {
            if (addressCache.TryGetValue(rva, out kind))
                return true;

            //While ntdll may list __C_specific_handler in its exports, this basically
            //such a niche scenario that it doesn't make sense to have everyone else pay
            //the price of looking up their exports just to not find any exception handlers.
            //As such, we do not attempt to do this
            return false;
        }

        public bool TryGetImport(int rva, out WellKnownExceptionHandlerKind kind)
        {
            if (!hasRequestedImports)
            {
                imports = peFile.ImportTable;
                hasRequestedImports = true;
            }

            if (imports == null)
            {
                kind = default;
                return false;
            }

            if (peFile.TryGetOffset(rva, out var offset))
            {
                for (var i = 0; i < imports.Length; i++)
                {
                    var descriptor = imports[i];

                    var iat = descriptor.ImportAddressTable.ValueOrDefault;

                    if (iat == null)
                        continue;

                    for (var j = 0; j < iat.Length; j++)
                    {
                        if (iat[j].Offset == offset)
                        {
                            var name = descriptor.ImportLookupTable.Value[j].Name;

                            if (TryGetKindForName(name.Value.Name.ToString(), out kind))
                                addressCache[(int) rva] = kind;
                            else
                                addressCache[(int) rva] = kind;

                            return true;
                        }
                    }
                }
            }

            kind = default;
            return false;
        }

        public void AddMatch(int rva, WellKnownExceptionHandlerKind? kind)
        {
            addressCache[rva] = kind;
        }

        private bool TryGetKindForName(string name, out WellKnownExceptionHandlerKind kind)
        {
            switch (name)
            {
                case "__C_specific_handler":
                    kind = WellKnownExceptionHandlerKind.__C_specific_handler;
                    break;

                case "__C_specific_handler_noexcept":
                    kind = WellKnownExceptionHandlerKind.__C_specific_handler_noexcept;
                    break;

                case "__CxxFrameHandler":
                    kind = WellKnownExceptionHandlerKind.__CxxFrameHandler;
                    break;

                case "__CxxFrameHandler3":
                    kind = WellKnownExceptionHandlerKind.__CxxFrameHandler3;
                    break;

                case "__CxxFrameHandler4":
                    kind = WellKnownExceptionHandlerKind.__CxxFrameHandler4;
                    break;

                case "__GSHandlerCheck":
                    kind = WellKnownExceptionHandlerKind.__GSHandlerCheck;
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
