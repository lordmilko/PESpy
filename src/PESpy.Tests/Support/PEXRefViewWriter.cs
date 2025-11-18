using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Tests
{
    internal class PEXRefViewWriter : PEViewWriter
    {
        [DebuggerDisplay("FieldOffset = {FieldOffset}, TargetValue = {TargetValue}")]
        public struct XRef
        {
            public int FieldOffset;
            public int TargetValue;
        }

        public List<XRef> XRefs { get; } = new List<XRef>();

        public PEXRefViewWriter(PEFile peFile) : base(peFile)
        {
        }

        public override void WriteOffsetXRef(int structOffset, int fieldOffset, int targetOffset)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetOffset });
        }

        public override void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetRVA });
        }

        public override void WriteVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetRVA });
        }
    }
}
