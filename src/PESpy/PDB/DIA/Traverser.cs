namespace PESpy.PDB
{
    internal abstract class Traverser
    {
        protected ISECT _targetSeg;
        protected int _targetOff;

        protected ISECT _bestSeg;
        protected int _bestOff;

        protected Traverser(OffSeg targetOffSeg, OffSegSym bestOffSeg)
        {
            _targetSeg = targetOffSeg.seg;
            _targetOff = targetOffSeg.off;
            _bestSeg = bestOffSeg.seg;
            _bestOff = bestOffSeg.off;
        }

        public abstract bool next(out TraverserResult result);
    }
}
