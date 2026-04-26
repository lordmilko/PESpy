using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /* richprint has a "dumb" list of product items to product descriptions. The major issue with this list is a lot of information is duplicated.
     * In most cases, you only need to rely on the build id to determine the product, but they maintain a mapping of every single tool kind to every single
     * toolchain kind.
     * 
     * https://github.com/dishather/richprint/blob/master/comp_id.txt
     * 
     * Instead, we base our product info on PEAnatomist, which contains the most overwhelming comprehensive list in existence (as far as I can find).
     * Names present in richprint that are missing from PEAnatomist that I have not vetted yet include the following:
     * 
     * - VS98 (6.0) SP6 cvtres build 1736
     * - VS98 (6.0) cvtres build 1720
     * - VS2005 [8.0] build 50320
     * - VS2019 v16.11.14 build 30144
     * - VS2022 v17.3.0 pre 4.0 build 31628
     */

    //Per Windows 2000

    /// <summary>
    /// Represents the <see cref="PRODITEM"/> structure which describes an entry in the Rich Header.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly partial struct ProdItem : IValue, IViewable //Stored in an array, so can be a struct
    {
        private string DebuggerDisplay()
        {
            var info = ProductInfo;

            if (info != null)
            {
                var builder = new StringBuilder();

                var mainKind = (ProductKind) ((int) info.Value.Kind & ~ProductKindFlags.LanguageMask);

                builder.Append("[").Append(mainKind);

                var language = info.Value.LanguageKind;

                if (language != ProductKind.None)
                    builder.Append(":").Append(language);

                builder.Append("] ");

                var toolsetFullName = info.Value.ToolsetFullName;

                if (toolsetFullName != null)
                    builder.Append(toolsetFullName).Append(" / ");

                builder.Append(info.Value.ToolFullName).Append(" / ");
                builder.Append(ProdId);

                return builder.ToString();
            }
            else
                return ProdId.ToString();
        }

        internal bool TryGetProductInfo(PRODID prodId, int buildId, out ProductInfo productInfo)
        {
            /* There's two aspects to the PRODITEM
             * - Which tool was used (as identified by the PRODID)
             * - Which toolset was that tool's build ID found in
             */

            if ((int) prodId >= tools.Length)
            {
                //Unknown tool; can't get product info. Arguably, we _could_ just search all known buildId's we have recorded
                //to see if we can identify the product that way, but that's not how PEAnatomist's object model works. If we find
                //that this ever becomes an issue, we can tweak our logic
                Debug.Assert(false);
                productInfo = default;
                return false;
            }

            var tool = tools[(int) prodId];

            //Get the span that corresponds to toolset group that the tool is associated with
            var toolsets = GetToolGroup(tool.group);

            var index = BinarySearchExact(buildId, toolsets);

            //I'm not 100% sure that this logic is right, but it'll do for now
            if (index == -1)
            {
                /* We diverge from PEAnatomist 0.2 here. PEAnatomist would have you believe that C2 19.42.34321 belongs to VS2015 14.0.
                 * This is clearly false. When PEAnatomist doesn't know the build ID, it defaults to using the name of the build group,
                 * which in the case of VS2015 can cause us to be way off, because VS2015+ all share the same group. While it is true
                 * that within the build range covered by VS2015+ there are several stray VS2017 items here and there, overall I think
                 * we would be better off just searching for the closest match and reporting that the version is at least higher than this
                 * 
                 * PEAnatomist 0.4 seems to yield the same results as us, but that's probably just because of all of the new versions
                 * that have been added
                 */

                if (tool.group == VS_2015_14_0)
                    index = BinarySearchClosest(buildId, toolsets);

                if (index == -1)
                {
                    string fallbackToolsetName = null;

                    if (tool.majorVersion != 0 && tool.group != 0)
                    {
                        Debug.Assert(false, "Add this build to our build map and list as not being part of PEAnatomist");
                        //todo: should we also list the name as being approximate in that case? its not an approximate build so much as an approximate name,
                        //which means we need to change how our approx property works to no longer just be a getter but allow the caller to set it either to true manually
                        //or do the check of toolset.toolbuildid vs buildid manually
                        fallbackToolsetName = toolsetNames[tool.group - 1];
                    }

                    productInfo = new ProductInfo(tool.majorVersion, tool.minorVersion, buildId, tool.kind, fallbackToolsetName, null, null, buildId);
                    return true;
                }
            }

            var toolset = toolsets[index];

            var toolsetName = toolsetNames[toolset.nameIndex - 1];

            int minorVersion = tool.minorVersion;

            if (tool.group == VS_2015_14_0)
            {
                //The version of the compiler in VS2015 is simply 14.0. But since VS2017, the compiler version increases
                //at a steady rate, and multiple releases may share the same tooling version. Note that there is a distinction
                //between the MSVC version (which is in the 14 range) and the actual version of cl.exe (which, if you run it,
                //you'll see it's actually in the 19 range). As such, PEAnatomist maintains a "range" lookup table for VS2015+
                //https://learn.microsoft.com/en-us/cpp/overview/compiler-versions?view=msvc-170

                //Walk the table backwards trying to find the best minor version to use, based on our build ID
                for (var i = utc1900MinorVersionMap.Length - 1; i >= 0; i--)
                {
                    var item = utc1900MinorVersionMap[i];

                    if (buildId >= item.buildId)
                    {
                        minorVersion = item.minorVersion;
                        break;
                    }
                }
            }

            //This toolset could either have a named release type (RTM, Preview 2, etc) or its own build ID (e.g. the 2 in 16.6.2)

            if ((toolset.releaseType & HAS_BUILD) == HAS_BUILD)
            {
                //This release type is smuggling the build ID of the toolset itself
                var toolsetBuildId = toolset.releaseType & ~+HAS_BUILD;

                productInfo = new ProductInfo(tool.majorVersion, minorVersion, buildId, tool.kind, toolsetName, toolsetBuildId, null, toolset.toolBuildId);
                return true;
            }
            else
            {
                //The release type is a 1-based index into the release types map. 0 means there's no release type

                var releaseType = toolset.releaseType == 0 ? null : toolsetReleaseTypes[toolset.releaseType - 1];

                productInfo = new ProductInfo(tool.majorVersion, minorVersion, buildId, tool.kind, toolsetName, null, releaseType, toolset.toolBuildId);
                return true;
            }
        }

        //Binary search to find the item that matches this build ID. Theoretically you could have any combination of product and build ID,
        //but I suppose that's unlikely; PEAnatomist seems to have done the hard work of determining what can be expected
        static int BinarySearchExact(int buildId, in ReadOnlySpan<(int buildId, int nameIndex, int releaseType)> span)
        {
            var lo = 0;
            var hi = span.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                var itemBuildId = span[mid].buildId;

                if (itemBuildId > buildId)
                    hi = mid - 1;
                else if (itemBuildId < buildId)
                    lo = mid + 1;
                else
                    return mid;
            }

            //No match
            return -1;
        }

        static int BinarySearchClosest(int buildId, in ReadOnlySpan<(int buildId, int nameIndex, int releaseType)> span)
        {
            var lo = 0;
            var hi = span.Length - 1;

            var best = -1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                var itemBuildId = span[mid].buildId;

                if (itemBuildId > buildId)
                    hi = mid - 1;
                else if (itemBuildId < buildId)
                {
                    best = mid;
                    lo = mid + 1;
                }
            }

            //No match
            return best;
        }

        private const int ProdIdOffset = 0;
        private const int BuildIdOffset = 2;
        private const int CountOffset = 4;

        public PRODID ProdId { get; init; }

        /* The format of a 4 digit version is major.minor.build.revision.
         * This property gets the build ID of the tool, e.g. cl.exe, link.exe.
         * You may use this build ID to also try and identify the toolchain that that
         * tool was a part of. e.g. VS2022 17.4.1. Observe that the toolchain here
         * also has a build ID, but it's not related to the build ID we see here */
        public ushort BuildId { get; init; }

        /// <summary>
        /// Gets the original @comp.id value that was placed in an *.obj file (or was synthesized by the linker) that this product item was constructed from.
        /// </summary>
        public int CompID => ((ushort) ProdId << 16) | BuildId;

        public int Count { get; init; }

        public ProductInfo? ProductInfo
        {
            get
            {
                if (TryGetProductInfo(ProdId, BuildId, out var productInfo))
                    return productInfo;

                return default;
            }
        }

        public int Offset { get; }

        internal const int StructSize =
            sizeof(short) + //ProdId
            sizeof(short) + //BuildId
            sizeof(int); //Count

        internal ProdItem(int start, int bufferPos, Span<byte> bytes)
        {
            Offset = start + bufferPos;

            var dwProdid = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            var dwCount = MemoryMarshal.Read<int>(bytes.Slice(bufferPos + 4));

            ProdId = (PRODID) ((dwProdid & 0xFFFF0000) >> 16);
            BuildId = (ushort)((dwProdid & 0x0000FFFF));
            Count = dwCount;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ProdItem, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ProdId), ProdIdOffset, ProdId, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(BuildId), BuildIdOffset, BuildId);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Count), CountOffset, Count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
