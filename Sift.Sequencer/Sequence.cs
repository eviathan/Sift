using Sift.Sequencer.Grid;
using Sift.Sequencer.Models;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.NodeEvents;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;
        public Grid<Node> Grid { get; private set; } = new();

        private GridWalkerPool _walkerPool { get; set; }
        private List<GridWalker> _activeWalkers { get; set; } = [];
        private List<(Node Node, Vector3D Direction)> _queuedWalkers { get; set; } = [];
        private NodeEventArgs _eventArgs { get; set; } = new();


        public Sequence()
        {
            _eventArgs.Sequence = this;
            _walkerPool = new GridWalkerPool(p => new GridWalker(p), 100);
        }

        public void Initialise()
        {
            var originNode = new TestNode(
               position: new Vector3D(0, 0, 0),
               emitOnPlay: true
            )
            {
                Events = [
                   new EmitNodeEvent(new Dictionary<Vector3D, int?>
                    {
                        { new Vector3D(1, 0, 0), 1 },
                    }),
                    new LogNodeEvent()
               ]
            };

            var nextNode = new TestNode(new Vector3D(4, 0, 0))
            {
                Events = [
                    new EmitNodeEvent(new Dictionary<Vector3D, int?>
                    {
                        { new Vector3D(0, 1, 0), 1 },
                    }),
                    new LogNodeEvent()
                ]
            };

            var anotherNode = new TestNode(new Vector3D(4, 4, 0))
            {
                Events = [
                    new MIDINodeEvent(),
                    new EmitNodeEvent(new Dictionary<Vector3D, int?>
                    {
                        { new Vector3D(-1, 0, 0), 1 },
                    }),
                    new LogNodeEvent()
                ]
            };

            var yetAnotherNode = new TestNode(new Vector3D(0, 4, 0))
            {
                Events = [
                    new EmitNodeEvent(new Dictionary<Vector3D, int?>
                    {
                        { new Vector3D(0, -1, 0), 1 },
                    }),
                    new LogNodeEvent()
                ]
            };

            AddNode(originNode);
            AddNode(nextNode);
            AddNode(anotherNode);
            AddNode(yetAnotherNode);
        }

        public void Deinitialise()
        {
            _activeWalkers.Clear();
        }

        public void Update(int tick)
        {
            Console.WriteLine($"Tick: {tick}");

            if (tick == 0)
            {
                var initialNodes = Grid.Values.Where(x => x.EmitOnPlay && x.IsActive).ToList();
                foreach (var node in initialNodes)
                {
                    _eventArgs.Node = node;

                    foreach (var e in node.Events)
                        e.Invoke(_eventArgs);
                }
            }
            else
            {
                TraverseWalkers();
            }

            ActivateWalkers();
        }

        private void ActivateWalkers()
        {
            foreach (var queuedWalker in _queuedWalkers)
            {
                if (_walkerPool.TryGet(out var walker))
                {
                    walker.Set(queuedWalker.Node.Position, queuedWalker.Direction);
                    _activeWalkers.Add(walker);
                }
            }
            _queuedWalkers.Clear();
        }

        private void TraverseWalkers()
        {
            foreach (var walker in _activeWalkers)
            {
                walker.Traverse();

                if (Grid.TryGetNode(walker.Position, out var node))
                {
                    _eventArgs.Node = node;

                    node.DidStart(_eventArgs);

                    foreach (var nodeEvent in node.Events)
                        nodeEvent.Invoke(_eventArgs);

                    node.DidEnd(_eventArgs);
                }
            }
        }

        public void EnqueueWalker(Node node, Vector3D direction)
        {
            _queuedWalkers.Add((Node: node, Direction: direction));
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
            // release its walker from the pool
            // remove the walker to the sequences list of walkers
            // throw new NotImplementedException();
            Grid.Remove(x, y, z);
        }

        public void ResetWalker()
        {
            foreach (var walker in _activeWalkers)
                walker.Reset();
        }
    }
}