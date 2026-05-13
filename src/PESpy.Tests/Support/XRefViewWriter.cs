using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Tests
{
    internal class XRefViewWriter : ViewWriter
    {
        [DebuggerDisplay("FieldOffset = {FieldOffset}, TargetValue = {TargetValue}")]
        public struct XRef
        {
            public int FieldOffset;
            public long TargetValue;
        }

        public List<XRef> XRefs { get; } = new List<XRef>();

        public XRefViewWriter(PEFile peFile) : base(peFile)
        {
        }

        public override void WriteOffsetXRef(long structOffset, int fieldOffset, long targetOffset)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetOffset });
        }

        public override void WriteRVAXRef(long structOffset, int fieldOffset, int targetRVA)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetRVA });
        }

        public override void WriteVAXRef(long structOffset, int fieldOffset, long targetRVA)
        {
            XRefs.Add(new XRef { FieldOffset = fieldOffset, TargetValue = targetRVA });
        }
    }
}
