using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Snapshots;

namespace Sift.Core.Events
{
    // NOTE: Sequence is assigned by the RuntimeEventHub and increases by one per event. Components can publish their own
    // events by deriving from this
    public abstract record RuntimeEvent
    {
        public long Sequence { get; init; }

        public Time Tick { get; init; }
    }

    // NOTE: Position at Timestamp (Stopwatch ticks), extrapolate with TicksPerSecond to draw a smooth playhead.
    // Stopping clears everything in flight
    public sealed record TransportChanged(bool IsPlaying, int Tempo, double Position, double TicksPerSecond, long Timestamp) : RuntimeEvent;

    // NOTE: EndPosition is the target's cell, or where the ray gives up when TargetNodeId is null.
    // The message is at Origin + Direction * (tick - EmitTime) / TicksPerCell
    public sealed record MessageEmitted(
        Guid MessageId,
        Guid SourceNodeId,
        Vector3D Origin,
        Direction Direction,
        Time EmitTime,
        long TicksPerCell,
        string Payload,
        Guid? TargetNodeId,
        Vector3D EndPosition,
        Time ArrivalTime) : RuntimeEvent;

    public sealed record MessageRetargeted(Guid MessageId, Guid? TargetNodeId, Vector3D EndPosition, Time ArrivalTime) : RuntimeEvent;

    public sealed record MessageDelivered(Guid MessageId, Guid NodeId, Direction Port) : RuntimeEvent;

    public sealed record MessageDissipated(Guid MessageId, Vector3D Position) : RuntimeEvent;

    public sealed record NodeTriggeredByPlay(Guid NodeId) : RuntimeEvent;

    // NOTE: InvocationId pairs Started with Finished, a component can have several invocations running at once
    public sealed record ComponentStarted(Guid InvocationId, Guid NodeId, Direction Port, Guid ComponentId) : RuntimeEvent;

    public sealed record ComponentFinished(Guid InvocationId, Guid NodeId, Direction Port, Guid ComponentId, ComponentOutcome Outcome, string? Error) : RuntimeEvent;

    public sealed record NoteOn(Guid NodeId, int Channel, int Note, int Velocity) : RuntimeEvent;

    public sealed record NoteOff(Guid NodeId, int Channel, int Note) : RuntimeEvent;

    public sealed record NodeAdded(NodeSnapshot Node, CommandSource Source) : RuntimeEvent;

    public sealed record NodeRemoved(Guid NodeId, CommandSource Source) : RuntimeEvent;

    public sealed record NodeMoved(Guid NodeId, Vector3D From, Vector3D To, CommandSource Source) : RuntimeEvent;

    // NOTE: Anything other than a move, e.g. properties, components or component config
    public sealed record NodeChanged(NodeSnapshot Node, CommandSource Source) : RuntimeEvent;

    public sealed record CommandRejected(IGridCommand Command, CommandSource Source, string Error) : RuntimeEvent;

    public sealed record HistoryChanged(bool CanUndo, bool CanRedo) : RuntimeEvent;

    // NOTE: The subscriber fell behind and events were lost, take a new snapshot to resync
    public sealed record EventsDropped(long Count) : RuntimeEvent;
}
