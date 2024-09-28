using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public class ImageFunctionOverrideHeader : IValue, IViewable //Will be boxed in ImageDynamicRelocation record
    {
        public int FuncOverrideSize { get; }

        public ImageFunctionOverrideDynamicRelocation[] FuncOverrides { get; }

        public ImageBDDInfo BDDInfo { get; }

        public int Offset { get; }

        internal ImageFunctionOverrideHeader(ref FileReader reader, int end)
        {
            Offset = (int) reader.Position;

            FuncOverrideSize = reader.ReadInt32();

            //IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION  FuncOverrideInfo[ANYSIZE_ARRAY]; // FuncOverrideSize bytes in size
            //IMAGE_BDD_INFO BDDInfo; // BDD region, size in bytes: DVRTEntrySize - sizeof(IMAGE_FUNCTION_OVERRIDE_HEADER) - FuncOverrideSize

            var funcOverrideEnd = reader.Position + FuncOverrideSize;

            var funcOverrides = new List<ImageFunctionOverrideDynamicRelocation>();

            //ImageFunctionOverrideDynamicRelocation is dynamic in size
            while (reader.Position < funcOverrideEnd)
                funcOverrides.Add(new ImageFunctionOverrideDynamicRelocation(ref reader));

            FuncOverrides = funcOverrides.ToArray();

            //I don't know if it's guaranteed that we'll then have a BDD
            var bddSize = end - reader.Position;
            Debug.Assert(bddSize != 0);

            BDDInfo = new ImageBDDInfo(ref reader);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_FUNCTION_OVERRIDE_HEADER), this, ViewKind.ImageFunctionOverrideHeader);

            s.WriteField(nameof(FuncOverrideSize), FuncOverrideSize);

            s.WriteInline(FuncOverrides);
            s.WriteInline(BDDInfo);
        }
    }
}
