using ClrDebug.PDB;

namespace PESpy.PDB
{
    public static class OmapDataExtensions
    {
        /// <summary>
        /// Given an RVA in the "OMAP" (<see cref="PEFile"/>) address space, convert it to the "Src" (<see cref="PDBFile"/>)
        /// address space to enable looking up symbols within a symbol file.
        /// </summary>
        /// <param name="omapToSrc">The OmapToSrc entries that should be searched to find the original Src address</param>
        /// <param name="rva">An RVA that points to a physical location in a <see cref="PEFile"/>. The entity at the given
        /// RVA was relocated during compilation, and must be resolved to the original "Src" address that was encoded
        /// in the PDB in order to find the symbol that is associated with that address.</param>
        /// <param name="srcRva">The original "Src" address contained in the PDB.</param>
        /// <returns>True if a Src address could be resolved, otherwise false.</returns>
        public static unsafe bool TryConvertOmapToSrc(this NativeSpan<OMAP_DATA> omapToSrc, int rva, out int srcRva)
        {
            //Note: PDBFileGetOmapSectionAndOffset() relies on us preserving the original rva in srcRva on failure
            //so we don't need to worry about copying srcRva to rva on success

            var numEntries = omapToSrc.Length;
            var lo = (OMAP_DATA*) omapToSrc;
            var hi = lo + numEntries;

            while (lo < hi)
            {
                var mid = numEntries / 2;

                var pMid = lo + ((numEntries & 1) != 0 ? mid : (mid - 1));

                if (rva == pMid->rva)
                {
                    if (pMid->rvaTo == 0)
                    {
                        //In DbgHelp, SYMOPT_OMAP_FIND_NEAREST will cause the nearest symbol to be used,
                        //by doing rva-- and setting lo = pMid. This helps handle us being at the start
                        //of an inserted branch instruction. But in DIA, it seems like we simply break out
                        //when we hit our target RVA; if rvaTo has a value, great! Otherwise, it's rewind time

                        lo = pMid + 1;
                        break;
                    }

                    srcRva = pMid->rvaTo;
                    return true;
                }

                if (rva < pMid->rva)
                {
                    hi = pMid;
                    numEntries = (numEntries & 1) != 0 ? mid : (mid - 1);
                }
                else
                {
                    lo = pMid + 1;
                    numEntries = mid;
                }
            }

            //If we failed to find an exact match, lo will point to the next entry after our address
            if (lo == omapToSrc)
            {
                //We point to the very start; it was a total fail
                srcRva = rva;
                return false;
            }

            //Find the previous valid item

            do
            {
                lo--;

                if (lo->rvaTo != 0)
                    break;

            } while (lo > omapToSrc);

            //Uh-oh, we failed to find any remaining valid items!
            if (lo->rvaTo == 0)
            {
                srcRva = default;
                return false;
            }

            var displacement = rva - lo->rva;

            srcRva = lo->rvaTo + displacement;
            return true;
        }

        public static unsafe bool TryConvertOmapFromSrc(this NativeSpan<OMAP_DATA> omapFromSrc, int rva, out int omapRva)
        {
            //Note: PDBFileGetOmapSectionAndOffset() relies on us preserving the original rva in srcRva on failure
            //so we don't need to worry about copying srcRva to rva on success; for consistency, we maintain the same
            //behavior here as well

            var numEntries = omapFromSrc.Length;

            var lo = (OMAP_DATA*) omapFromSrc;
            var hi = lo + numEntries;

            while (lo < hi)
            {
                var mid = numEntries / 2;

                var pMid = lo + ((numEntries & 1) != 0 ? mid : (mid - 1));

                if (rva == pMid->rva)
                {
                    if (pMid->rvaTo != 0)
                    {
                        omapRva = pMid->rvaTo;
                        return true;
                    }
                    else
                    {
                        //A match was found, but the address was in an area that was discarded
                        omapRva = rva;
                        return false;
                    }
                }

                if (rva < pMid->rva)
                {
                    hi = pMid;
                    numEntries = (numEntries & 1) != 0 ? mid : (mid - 1);
                }
                else
                {
                    lo = pMid + 1;
                    numEntries = mid;
                }
            }

            //If we failed to find an exact match, lo will point to the next entry after our address
            if (lo == omapFromSrc)
            {
                //We point to the very start; it was a total fail
                omapRva = rva;
                return false;
            }

            if (lo[-1].rvaTo == 0)
            {
                //A match was found, but the address was in an area that was discarded
                omapRva = rva;
                return false;
            }

            var displacement = rva - lo[-1].rva;

            omapRva = lo[-1].rvaTo + displacement;
            return true;
        }

    }
}
