using System.Collections.Generic;
using PESpy.View;

namespace PESpy.PDB
{
    public interface IStreamTable : IValue, IViewable
    {
        List<List<PN>> StreamPages { get; }

        List<SI> StreamInfos { get; }

        bool HasStream(SN sn);

        /// <summary>
        /// Gets or sets the stream info of the stream with the specified page number.
        /// </summary>
        /// <param name="sn">The stream number of the stream to set the stream info for.</param>
        /// <returns>The stream info of the stream with the specified stream number.</returns>
        SI this[SN sn] { get;set; }
    }
}
