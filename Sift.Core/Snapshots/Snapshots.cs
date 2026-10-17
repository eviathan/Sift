using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;

namespace Sift.Core.Snapshots
{
    // NOTE: Immutable copies of runtime state, safe to hand to the UI thread. Component configs are immutable records so are shared

    public sealed record NodeInfo(Guid Id, string? Name, Vector3D Position, bool IsActive, bool EmitOnPlay);

    public sealed record ComponentSnapshot(Guid Id, string Type, ComponentCategory Category, IComponentConfig Config);

    public sealed record NodeSnapshot(
        Guid Id,
        string? Name,
        Vector3D Position,
        bool IsActive,
        bool EmitOnPlay,
        IReadOnlyDictionary<Direction, IReadOnlyList<ComponentSnapshot>> Ports);

    public sealed record InFlightSnapshot(
        Guid MessageId,
        Guid SourceNodeId,
        Vector3D Origin,
        Direction Direction,
        Time EmitTime,
        long TicksPerCell,
        string Payload,
        Guid? TargetNodeId,
        Vector3D EndPosition,
        Time ArrivalTime);

    // NOTE: Apply events with a Sequence greater than LastEventSequence on top of this
    public sealed record SequenceSnapshot(
        long LastEventSequence,
        Time Tick,
        bool IsPlaying,
        int Tempo,
        Quantisation Quantisation,
        long TicksPerCell,
        IReadOnlyList<NodeSnapshot> Nodes,
        IReadOnlyList<InFlightSnapshot> InFlight,
        bool CanUndo,
        bool CanRedo);
}
