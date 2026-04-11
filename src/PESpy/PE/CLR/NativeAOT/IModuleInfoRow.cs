using ClrDebug;
using PESpy.View;

namespace PESpy.NativeAOT
{
    public interface IModuleInfoRow : IViewable, IValue
    {
        ReadyToRunSectionType SectionId { get; }

        long Start { get; }

        long End { get; }

        int Length { get; }

        VA<IValue> Data { get; }
    }
}
