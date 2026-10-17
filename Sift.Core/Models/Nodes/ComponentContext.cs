using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Snapshots;

namespace Sift.Core.Models.Nodes
{
    public sealed class ComponentContext
    {
        // NOTE: A copy taken at dispatch, change the node through Sequencer.Submit
        public NodeInfo Node { get; }

        // NOTE: The port this component sits on, which is also the direction it emits in
        public Direction Port { get; }

        // NOTE: True when triggered by playback starting rather than by a message arriving on Port
        public bool IsPlay { get; }

        public Message Message { get; }

        public Time DispatchTime { get; }

        public ISequencerContext Sequencer { get; }

        // NOTE: Shared by the node's components for this playback
        public NodeState State { get; }

        // NOTE: Where a message passing straight through continues to, the opposite side from where it arrived
        public Direction StraightOn => Port.Opposite();

        public ComponentContext(NodeInfo node, Direction port, bool isPlay, Message message, Time dispatchTime, ISequencerContext sequencer, NodeState state)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Port = port;
            IsPlay = isPlay;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DispatchTime = dispatchTime;
            Sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }
    }
}
