using System;
using PESpy.Ecma335;
using PESpy.View.Builder;

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

        IView Finalize(ViewWriter viewWriter);

        void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories);
    }
}
