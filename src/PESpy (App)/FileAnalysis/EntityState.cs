using PESpy.View;

namespace PESpy
{
    public class EntityState
    {
        public EntityKind Kind;
        public int StartOffset;

        //View specific
        public IView? View;
        public int LastChildIndex = -1;
        public ViewChildList? Children;

        //Asm specific
        public int BytesWritten;
        public bool IsDOSStub;
        public bool IsComplete;
        internal IncrementResult LastIncrementResult;

        public EntityState(IView view)
        {
            View = view;
            Kind = EntityKind.View;
            StartOffset = view.Offset;
        }

        public EntityState(EntityKind kind, int startOffset)
        {
            Kind = kind;
            StartOffset = startOffset;
        }

        public override string ToString()
        {
            if (View != null)
            {
                return $"{View.Kind} (LastChild: {LastChildIndex})";
            }
            else
                return Kind.ToString();
        }
    }
}
