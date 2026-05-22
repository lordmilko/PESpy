using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    public struct MstatManifestResourceRow
    {
        public int Size { get; }

        public UserString Name { get; }

        public MstatAssemblyRow Assembly => _mstatHeap.AssemblyTable[_token.Rid - 1];

        internal int NextManifestResource;

        private readonly mdToken _token;
        private readonly MstatHeap _mstatHeap;

        internal MstatManifestResourceRow(MstatInfo.ManifestResource manifestResource, MstatHeap mstatHeap)
        {
            Size = manifestResource.Size;
            Name = manifestResource.Name;
            _token = manifestResource.Token;
            _mstatHeap = mstatHeap;
        }
    }
}
