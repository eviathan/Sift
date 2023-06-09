using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Sequencer.Nodes
{
    public enum NodeEventType
    {
        Start,
        End
    }

    public class NodeEvent
    {
        public Node Node { get; set; }
        public long Time { get; set; }
        public NodeEventType Type { get; set; }

        public NodeEvent() { }

        public NodeEvent(Node node, long time, NodeEventType type)
        {
            Node = node;
            Time = time;
            Type = type;
        }
    }
}