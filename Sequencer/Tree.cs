using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Tree
    {
        public Node RootNode { get; private set; }
        public PriorityQueue<NodeEvent, long> Events { get; private set; } = new PriorityQueue<NodeEvent, long>();

        public Tree(Node rootNode)
        {
            RootNode = rootNode;
            RootNode
                .GetEvents()
                .ForEach(EnqueueEvents);
        }

        public void Update(SequencerContext context) 
        {
            while(Events.TryPeek(out var nodeEvent, out var eventTime) && eventTime <= context.Time)
            {
                Events.Dequeue();

                if(nodeEvent.Type == NodeEventType.Start)
                {
                    nodeEvent.Node.DidStart();

                    var endEvent = new NodeEvent
                    {
                        Node = nodeEvent.Node,
                        Time = eventTime + nodeEvent.Node.Duration, 
                        Type = NodeEventType.End
                    };
                    Events.Enqueue(endEvent, endEvent.Time);

                    foreach(var childNode in nodeEvent.Node.Children)
                    {
                        childNode.UpdateTiming(context);

                        var startEvent = new NodeEvent
                        {
                            Node = childNode,
                            Time = eventTime + childNode.DistanceFromParent,
                            Type = NodeEventType.Start
                        };
                        Events.Enqueue(startEvent, startEvent.Time);
                    }
                }
                else
                {
                    nodeEvent.Node.DidEnd();
                }
            }
        }

        public void ResetTree()
        {
            Events.Clear();
            RootNode
                .GetEvents()
                .ForEach(EnqueueEvents);
        }

        private void Traverse ()
        {
            // This should traverse the tree and enqueue any active nodeevents
        }

        private void EnqueueEvents(NodeEvent e)
        {   
            Events.Enqueue(e, 0L);
        }
    }
}