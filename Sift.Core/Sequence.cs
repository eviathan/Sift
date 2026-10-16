using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core
{
    // NOTE: The document, everything here is what gets saved/loaded
    public class Sequence
    {
        public ushort Tempo { get; set; } = 40;
        public Quantisation Quantisation { get; set; } = new Quantisation(1, 4);

        public Grid<Node> Grid { get; } = new();

        public long TicksPerCell => Quantisation.ToTicks();
    }
}
