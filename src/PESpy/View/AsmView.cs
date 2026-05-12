using System;
using System.Diagnostics;

namespace PESpy.View
{
    public interface IAsmView : IView
    {
        string Name { get; }

        string FullName { get; }

        void GetFullName(ref PooledStringBuilder builder);

        byte Bitness { get; }
    }

    public class AsmView<T> : IAsmView, IViewInternal
    {
        public long Offset { get; }

        private string? name;

        public string Name
        {
            get
            {
                if (name == null)
                {
                    var diff = range.StartRVA - range.FunctionRVA;

                    if (diff == 0)
                        name = range.Name.ToString();
                    else if (diff > 0)
                        name = $"{range.Name}+0x{diff:X}";
                    else
                        name = $"{range.Name}-0x{Math.Abs(diff):X}";
                }

                return name;
            }
        }

        public string FullName
        {
            get
            {
                var builder = new PooledStringBuilder();

                try
                {
                    GetFullName(ref builder);

                    return builder.ToString();
                }
                finally
                {
                    builder.Dispose();
                }
            }
        }

        public unsafe void GetFullName(ref PooledStringBuilder builder)
        {
            var pViewByte = _fileAccessor.GetViewByte(Offset, out var sectionAccessorIndex);
            _fileAccessor.GetFullCodeName(Offset, sectionAccessorIndex, pViewByte, ref builder);
        }

        public long Size => range.Length;

        public ViewKind Kind { get; }

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public ViewXRefList XRefs => new ViewXRefList(this, _fileAccessor);

        public ViewImplKind ImplKind => ViewImplKind.Asm;

        //This type is not capable of having children
        public ViewChildList Children => default;

        public byte Bitness { get; }

        private AsmRange<T> range;
        private readonly FileAccessor _fileAccessor;

        public AsmView(int offset, byte bitness, in AsmRange<T> range, FileAccessor fileAccessor, ViewKind kind = ViewKind.Assembly)
        {
            Offset = offset;
            Kind = kind;
            Bitness = bitness;
            this.range = range;
            _fileAccessor = fileAccessor;
        }

        public IView this[int index] => throw new InvalidOperationException("This view does not contain children");

        public TResult Accept<TResult>(ViewVisitor<TResult> visitor) => visitor.VisitAsm(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitAsm(this);

        public override string ToString()
        {
            return ViewFormatter.FormatAsm(this);
        }
    }
}
