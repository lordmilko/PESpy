using PESpy.View;

namespace PESpy
{
    public unsafe class EntityState
    {
        public EntityKind Kind;
        public int StartOffset;

        //View specific
        public IView? View;
        public int LastChildIndex = -1;
        public bool HasChildren;
        public ViewChildList? Children;

        //Asm specific
        public int BytesWritten;
        internal IncrementResult LastIncrementResult;

        //Data specific
        public ViewByte* pStartViewByte;
        public string DataName;
        public int DataLength;

        //Asm / Data
        public bool IsAtStart;
        public bool IsAtEnd;
        public bool IsStreamer;

        public int ReverseResumeAddress;
        public ViewByte* ReverseViewByte;

        public EntityState(IView view, bool hasChildren = true)
        {
            View = view;
            Kind = EntityKind.View;
            StartOffset = view.Offset;
            HasChildren = hasChildren;
        }

        public EntityState(EntityKind kind, ViewByte* pStartViewByte, int startOffset)
        {
            Kind = kind;
            this.pStartViewByte = pStartViewByte;
            StartOffset = startOffset;
        }

        public override string ToString()
        {
            if (View != null)
            {
                if (View is IFieldView f)
                    return f.Name;

                return $"{View.Kind} (LastChild: {LastChildIndex})";
            }
            else
                return Kind.ToString();
        }
    }
}
