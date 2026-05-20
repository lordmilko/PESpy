using System;
using PESpy.Ecma335;

namespace PESpy.View
{
    interface IMetadataViewWriterHelper
    {
        MetadataSizes MetadataReader { get; }
    }

    interface IViewWriterHelper
    {
        FileKind FileKind { get; }

        bool Is32Bit { get; }

        ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate { get; }

        Func<int, int>? GetRealOffsetDelegate { get; }

        void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories);
    }
}
