using System.Collections.Generic;
using PESpy.View;

namespace PESpy.PDB
{
    public interface IStreamTable : IValue, IViewable
    {
        PN[][] StreamPages { get; }

        SI[] StreamInfos { get; }
    }
}
