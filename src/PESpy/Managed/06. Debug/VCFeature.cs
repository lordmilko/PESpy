using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the VCFeature debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public class VCFeature : IValue, IViewable //This will always be boxed, so no point being a struct
    {
        //Field names are based on the names listed with dumpbin

        public int PreVC11 { get; }

        public int C_CPP { get; } //C/C++

        public int GS { get; }

        public int SDL { get; }

        public int GuardN { get; }

        public RawOffset Offset { get; }

        internal VCFeature(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            PreVC11 = reader.ReadInt32();
            C_CPP = reader.ReadInt32();
            GS = reader.ReadInt32();
            SDL = reader.ReadInt32();
            GuardN = reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("VCFeature", this, ViewKind.VCFeature);

            s.WriteField(nameof(PreVC11), PreVC11);
            s.WriteField(nameof(C_CPP), C_CPP);
            s.WriteField(nameof(GS), GS);
            s.WriteField(nameof(SDL), SDL);
            s.WriteField(nameof(GuardN), GuardN);
        }
    }
}
