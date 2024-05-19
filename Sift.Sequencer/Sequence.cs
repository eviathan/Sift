using Sift.Sequencer.Grid;
using Sift.Sequencer.Models;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;
        public List<Tree> Trees { get; set; } = [];
        public Grid<Node> Grid { get; set; } = new();
        public List<GridWalker> Walkers { get; set; } = [
            new GridWalker(
                    new GridVector(1, 0, 0),
                    new GridVector(0, 0, 0)
                )
        ];

        private Pool<GridWalker> _walkerPool { get; set; } = new(100);

        public void Update(int tick)
        {
            // Console.WriteLine($"Tick: {tick}");

            // Update Walker Position
            foreach (var walker in Walkers)
            {
                // Check to see if there are any Nodes in the Grid on those positions
                if (Grid.TryGetValue(walker.Position, out var node))
                {
                    // If there are Iterate throught the events on the Nodes and Invoke them
                    foreach (var nodeEvent in node.Events)
                    {
                        nodeEvent.Invoke(node);
                    }

                    // // If there are any children grab walkers from a pool and set them in motion
                    // // NOTE THIS PROBABLY NEEDS A BETTER NAME
                    foreach (var child in node.Children)
                    {
                    }
                }

                walker.Traverse();
            }
        }

        public void AddNode(int x, int y, int z, Node node)
        {
            if (node == null)
                throw new ArgumentNullException("Cannot add a null node to the Grid!");

            // add it to the grid at
            Grid[x, y, z] = node;

            // We lease out a walker from the pool
            // if (_walkerPool.TryLeaseItems(out var walkers))
            // {
            //     var walker = walkers.First();
            //     // add the walker to the sequences list of walkers
            //     Walkers.Add(walker);
            // }
        }

        public void RemoveNode<TTreeNode>(int x, int y, int z)
            where TTreeNode : Node
        {
            // remove it from the grid at
            // release its walker from the pool
            // remove the walker to the sequences list of walkers
            throw new NotImplementedException();
        }

        public void ResetTrees()
        {
            foreach (var tree in Trees)
                tree.ResetTree();
        }
    }
}