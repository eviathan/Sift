using Sift.Sequencer.Grid;
using Sift.Sequencer.Models;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.NodeEvents;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    // TODO: We need to figure out how to deal with the inital EmitOnPlay events
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;
        public Grid<Node> Grid { get; private set; } = new();

        private GridWalkerPool _walkerPool { get; set; }
        private List<GridWalker> _activeWalkers { get; set; } = [];
        private NodeEventArgs _eventArgs { get; set; } = new();

        public Sequence()
        {
            _eventArgs.Sequence = this;
            _walkerPool = new GridWalkerPool(p => new GridWalker(p), 100);
        }

        public void Initialise()
        {
            #region Test Data
            var originNode = new TestNode(
               position: new Vector3D(0, 0, 0),
               emitOnPlay: true
            )
            {
                Events = [
                   new EmitNodeEvent(new Dictionary<Vector3D, int?>
                    {
                        { new Vector3D(1, 0, 0), 1 },
                    })
               ]
            };

            AddNode(originNode);
            AddNode(new TestNode(new Vector3D(4, 0, 0)));
            AddNode(new TestNode(new Vector3D(5, 0, 0)));
            #endregion

            // Get all active invoke on play nodes and create walkers for them

        }

        public void Deinitialise()
        {
            _activeWalkers.Clear();
        }

        public void Update(int tick)
        {
            Console.WriteLine($"Tick: {tick}");

            foreach (var walker in _activeWalkers)
            {
                if (Grid.TryGetValue(walker.Position, out var node))
                {
                    _eventArgs.Node = node;

                    node.DidStart(_eventArgs);

                    foreach (var nodeEvent in node.Events)
                    {
                        nodeEvent.Invoke(_eventArgs);
                    }

                    node.DidEnd(_eventArgs);
                }

                walker.Traverse();
            }
        }

        public void AddNode(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("Cannot add a null node to the Grid!");

            Grid[node.Position.X, node.Position.Y, node.Position.Z] = node;
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

        public void DispatchWalker(Node node, Vector3D direction)
        {
            if (_walkerPool.TryGet(out var walker))
            {
                walker.Set(node.Position, direction);
                _activeWalkers.Add(walker);
            }
        }

        public void ResetWalker()
        {
            foreach (var walker in _activeWalkers)
                walker.Reset();
        }
    }
}