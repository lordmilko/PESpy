using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public class ImageFunctionOverrideHeader : IValue, IViewable //Will be boxed in ImageDynamicRelocation record
    {
        private const int FuncOverrideSizeOffset = 0;

        public int FuncOverrideSize => chunk.PeekInt32(FuncOverrideSizeOffset);

        //IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION  FuncOverrideInfo[ANYSIZE_ARRAY]; // FuncOverrideSize bytes in size
        //IMAGE_BDD_INFO BDDInfo; // BDD region, size in bytes: DVRTEntrySize - sizeof(IMAGE_FUNCTION_OVERRIDE_HEADER) - FuncOverrideSize

        private ImageFunctionOverrideDynamicRelocation[]? funcOverrides;

        public ImageFunctionOverrideDynamicRelocation[] FuncOverrides
        {
            get
            {
                if (funcOverrides == null)
                {
                    using var results = new PooledList<ImageFunctionOverrideDynamicRelocation>();

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

        private readonly MemoryChunk chunk;
        private int length;

        internal ImageFunctionOverrideHeader(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_FUNCTION_OVERRIDE_HEADER, this, ViewKind.ImageFunctionOverrideHeader, length);

        int IViewable.NumChildren() => 1 + FuncOverrides.Length + 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index == 0)
                structWriter.WriteField(nameof(FuncOverrideSize), FuncOverrideSizeOffset, FuncOverrideSize);
            else
            {
                var i = index - 1;

                if (i < FuncOverrides.Length)
                    structWriter.WriteInline(FuncOverrides[i]);
                else
                {
                    if (i == FuncOverrides.Length)
                        structWriter.WriteInline(BDDInfo);
                    else
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
