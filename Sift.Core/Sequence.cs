using Microsoft.Extensions.Logging;
using Sift.Core.Grid;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;
using Sift.Core.Nodes;

// NOTE: Currently our walkers are condinuing to walk in the linear direction and not terminating/ geting readded to the pool when they hit a node.
namespace Sift.Core
{
    public class Sequence
    {
        private readonly ILogger<Sequence> _logger;

        public ushort Tempo { get; private set; } = 40;
        public Quantisation Quantisation { get; set; } = new Quantisation(1, 4);

        public Grid<Node> Grid { get; private set; } = new();

        private GridWalkerPool _walkerPool { get; set; }
        private List<GridWalker> _activeWalkers { get; set; } = [];
        private List<(Node Node, Vector3D Direction)> _queuedWalkers { get; set; } = [];


        public Sequence(ILogger<Sequence> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _walkerPool = new GridWalkerPool(p => new GridWalker(p), 100);
        }

        public void Initialise()
        {
        }

        public void Deinitialise()
        {
            
        }

        public void Update(UInt64 tick)
        {
            _logger.LogInformation($"Tick: {tick}");

            if (tick == 0)
            {
                var initialNodes = Grid.Values.Where(x => x.EmitOnPlay && x.IsActive).ToList();
                foreach (var node in initialNodes)
                {

                    // foreach (var e in node.Bus)
                    //     e.InvokeAsync(_eventArgs);
                }
            }
            else
            {
                // DO THING HERE
            }
        }
    }
}