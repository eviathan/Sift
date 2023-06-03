using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    public interface INode
    {
        INode? Parent { get; set; }
        List<INode> Children { get; }
        Position Position { get; }
        int DistanceFromParent { get; }

        void DidEnd();
        void DidStart();
    }
}