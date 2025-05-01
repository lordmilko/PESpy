using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public class ImageFunctionOverrideHeader : IValue, IViewable //Will be boxed in ImageDynamicRelocation record
    {
#if PEFAST
        public int FuncOverrideSize => chunk.PeekInt32(0);

        //IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION  FuncOverrideInfo[ANYSIZE_ARRAY]; // FuncOverrideSize bytes in size
        //IMAGE_BDD_INFO BDDInfo; // BDD region, size in bytes: DVRTEntrySize - sizeof(IMAGE_FUNCTION_OVERRIDE_HEADER) - FuncOverrideSize

        private ImageFunctionOverrideDynamicRelocation[] funcOverrides;

        public ImageFunctionOverrideDynamicRelocation[] FuncOverrides
        {
            get
            {
                if (funcOverrides == null)
                {
                    var results = new List<ImageFunctionOverrideDynamicRelocation>();

                    var read = 4;
                    var end = FuncOverrideSize + 4;

                    //ImageFunctionOverrideDynamicRelocation is dynamic in size
                    while (read < end)
                    {
                        var item = new ImageFunctionOverrideDynamicRelocation(chunk.Slice(read));
                        read += item.StructSize;
                        results.Add(item);
                    }

                    Debug.Assert(read == end);

                    funcOverrides = results.ToArray();
                }

                return funcOverrides;
            }
        }

        private ImageBDDInfo bddInfo;

        public ref readonly ImageBDDInfo BDDInfo
        {
            get
            {
                if (bddInfo.Offset == 0)
                {
                    //Don't know if it's guaranteed we'll have BDDInfo
                    var read = 4 + FuncOverrideSize;
                    var remaining = length - read;
                    Debug.Assert(remaining > 0);

                    bddInfo = new ImageBDDInfo(chunk.Slice(read));
                }

                return ref bddInfo;
            }
        }

        public int Offset => chunk.AbsoluteOffset;
#else
        public int FuncOverrideSize { get; }

        public ImageFunctionOverrideDynamicRelocation[] FuncOverrides { get; }

        public ImageBDDInfo BDDInfo { get; }

        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;
        private int length;

        internal ImageFunctionOverrideHeader(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }
#else
        internal ImageFunctionOverrideHeader(IFileReader reader, int end)
        {
            Offset = (int) reader.Position;

            FuncOverrideSize = reader.ReadInt32();

            //IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION  FuncOverrideInfo[ANYSIZE_ARRAY]; // FuncOverrideSize bytes in size
            //IMAGE_BDD_INFO BDDInfo; // BDD region, size in bytes: DVRTEntrySize - sizeof(IMAGE_FUNCTION_OVERRIDE_HEADER) - FuncOverrideSize

            var funcOverrideEnd = reader.Position + FuncOverrideSize;

            var funcOverrides = new List<ImageFunctionOverrideDynamicRelocation>();

            //ImageFunctionOverrideDynamicRelocation is dynamic in size
            while (reader.Position < funcOverrideEnd)
                funcOverrides.Add(new ImageFunctionOverrideDynamicRelocation(reader));

            FuncOverrides = funcOverrides.ToArray();

            //I don't know if it's guaranteed that we'll then have a BDD
            var bddSize = end - reader.Position;
            Debug.Assert(bddSize != 0);

            BDDInfo = new ImageBDDInfo(reader);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_FUNCTION_OVERRIDE_HEADER), this, ViewKind.ImageFunctionOverrideHeader);

            s.WriteField(nameof(FuncOverrideSize), FuncOverrideSize);

            s.WriteInline(FuncOverrides);
            s.WriteInline(BDDInfo);
        }
    }
}
