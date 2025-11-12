using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Tests
{
    internal class PEXRefViewWriter : PEViewWriter
    {
        [DebuggerDisplay("FieldOffset = {FieldOffset}, TargetOffset = {TargetOffset}")]
        public struct XRef
        {
            public int FieldOffset;
            public int TargetOffset;
        }

        public List<XRef> XRefs { get; } = new List<XRef>();

        public PEXRefViewWriter(PEFile peFile) : base(peFile)
        {
        }

        public override void WriteOffsetXRef(int structOffset, int fieldOffset, int targetOffset)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetOffset = targetOffset });
        }

        public override void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            throw new System.NotImplementedException();
        }

        public override void WriteVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            throw new System.NotImplementedException();
        }
    }
}
