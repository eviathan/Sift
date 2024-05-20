using Sift.Sequencer.Grid;
using Sift.Sequencer.Models;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;
        public Grid<Node> Grid { get; set; } = new();
        public List<GridWalker> Walkers { get; set; } = [];

        private GridWalkerPool _walkerPool { get; set; }
        private NodeEventArgs _eventArgs { get; set; } = new();

        public Sequence()
        {
            _walkerPool = new GridWalkerPool(p => new GridWalker(p), 100);
        }

        public void Update(int tick)
        {
            Console.WriteLine($"Tick: {tick}");

            foreach (var walker in Walkers)
            {
                if (Grid.TryGetValue(walker.Position, out var node))
                {
                    node.DidStart();

                    _eventArgs.Node = node;

                    foreach (var nodeEvent in node.Events)
                    {
                        nodeEvent.Invoke(_eventArgs);
                    }

                    // TODO:
                    // // If there are any children grab walkers from a pool and set them in motion
                    // // NOTE THIS PROBABLY NEEDS A BETTER NAME

                    node.DidEnd();
                }

                walker.Traverse();
            }
        }

        public void AddNode(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("Cannot add a null node to the Grid!");

            Grid[node.Position.X, node.Position.Y, node.Position.Z] = node;

            if (node.IsRoot && _walkerPool.TryGet(out var walker))
            {
                walker.Set(
                    new Vector3D(1, 0, 0),
                    new Vector3D(0, 0, 0)
                );

                Walkers.Add(walker);
            }
        }

        public void RemoveNode<TTreeNode>(int x, int y, int z)
            where TTreeNode : Node
        {
            // remove it from the grid at
            Grid.Remove(x, y, z);

            // release its walker from the pool
            // remove the walker to the sequences list of walkers
            throw new NotImplementedException();
        }

        public void ResetWalker()
        {
            foreach (var walker in Walkers)
                walker.Reset();
        }
    }
}