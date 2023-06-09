using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    // NOTE: We might be able to move all of the GridNode code into this
    // abstract class as its probably a bit redundant backing it with an interface
    public abstract class Node
    {
        public Node? Parent { get; set; }
        public List<Node> Children { get; private set; }
        public Position Position { get; private set; }
        public long DistanceFromParent { get; private set; }
        public long StartTime { get; private set; }
        public long Duration { get; private set; } = 500L;


        public Node(
            Position position,
            Node? parent = null,
            List<Node>? children = null
        )
        {
            Position = position;
            Parent = parent;

            DistanceFromParent = Parent != null 
                ? Position.Distance(Parent.Position)
                : 0;

            Children = children ?? new List<Node>();
            foreach (var child in Children)
                child.Parent = this;
        }

        public void UpdateTiming(SequencerContext context)
        {
            var parentEndTime = Parent != null 
                ? Parent.StartTime + Parent.Duration
                : 0L;

            StartTime = parentEndTime + DistanceFromParent;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }

        public virtual List<NodeEvent> GetEvents()
        {
            return new List<NodeEvent>
            {
                new NodeEvent
                {
                    Node = this,
                    Time = StartTime,
                    Type = NodeEventType.Start
                },
                new NodeEvent
                {
                    Node = this,
                    Time = StartTime + Duration,
                    Type = NodeEventType.End
                }
            };
        }
    }
}