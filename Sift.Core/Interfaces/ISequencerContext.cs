using Sift.Core.Enums;
using Sift.Core.Models;

namespace Sift.Core.Interfaces
{
    // NOTE: What components can do to the running sequence. Safe to call from any thread
    public interface ISequencerContext
    {
        Time Now { get; }

        // NOTE: Pass the context's DispatchTime as 'at' to emit on the exact tick the component was invoked,
        // omit it (e.g. from a looping component) to emit at the current tick
        void Emit(Guid sourceNodeId, Direction direction, Message message, Time? at = null);

        // NOTE: Applied at the start of the next tick. Dropped if playback has stopped
        Task Submit(IGridCommand command);

        // NOTE: Waits in musical time so tempo changes are followed. Completes with the tick it was due on, emit 'at' that
        // tick to stay exact. Cancelled when playback stops or the component is removed
        Task<Time> WaitUntilAsync(Time time, CancellationToken cancellationToken);

        Task<Time> WaitTicksAsync(long ticks, CancellationToken cancellationToken);
    }
}
