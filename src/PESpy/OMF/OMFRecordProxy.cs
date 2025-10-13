using System;
using System.Diagnostics;
using System.Text;

namespace PESpy.OMF
{
    class OMFRecordProxy
    {
        private OMFRecord omfRecord;

        public OMFRecordProxy(OMFRecord omfRecord)
        {
            this.omfRecord = omfRecord;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value => ObjectOMFRecordDispatcher.Instance.Dispatch(omfRecord);

        public static string DebuggerDisplay(OMFRecord omfRecord)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(omfRecord.RecordType).Append("]");

            var value = ObjectOMFRecordDispatcher.Instance.Dispatch(omfRecord);

            var defaultStr = omfRecord.RecordType.ToString();

            var str = value.ToString();

            if (defaultStr != str)
                builder.Append(" ").Append(str);

            return builder.ToString();
        }
    }
}
