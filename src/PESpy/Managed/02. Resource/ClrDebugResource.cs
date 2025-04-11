using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CLR_DEBUG_RESOURCE"/> structure that describes the DBI/DAC module that is associated
    /// with a CLR build. This structure is found in coreclr.dll <see cref="ResourceType.RCData"/> entries whose names begin with "CLRDEBUGINFO".
    /// </summary>
    public class ClrDebugResource : IValue, IViewable //This gets boxed in an IValue anyway, so is a class
    {
        public static readonly Guid CLR_ID_ONECORE_CLR = new Guid("B1EE760D-6C4A-4533-BA41-6F4F661FABAF");

        public int Version { get; }
        public Guid Signature { get; }
        public int DacTimeStamp { get; }
        public int DacSizeOfImage { get; }
        public int DbiTimeStamp { get; }
        public int DbiSizeOfImage { get; }

        public int Offset { get; }

        public const int StructSize =
            sizeof(int) + //Version
            16 + //Signature
            sizeof(int) + //DacTimeStamp
            sizeof(int) + //DacSizeOfImage
            sizeof(int) + //DbiTimeStamp
            sizeof(int); //DbiSizeOfImage

        internal ClrDebugResource(int version, Guid signature, IFileReader reader)
        {
            Offset = (int) reader.Position - 20;

            Version = version;
            Signature = signature;
            DacTimeStamp = reader.ReadInt32();
            DacSizeOfImage = reader.ReadInt32();
            DbiTimeStamp = reader.ReadInt32();
            DbiSizeOfImage = reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(CLR_DEBUG_RESOURCE), this, ViewKind.ClrDebugResource);

            s.WriteField("dwVersion", Version);
            s.WriteField("signature", Signature);
            s.WriteField("dwDacTimeStamp", DacTimeStamp);
            s.WriteField("dwDacSizeOfImage", DacSizeOfImage);
            s.WriteField("dwDbiTimeStamp", DbiTimeStamp);
            s.WriteField("dwDbiSizeOfImage", DbiSizeOfImage);
        }
    }
}
