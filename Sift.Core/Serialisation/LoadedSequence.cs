using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core.Serialisation
{
    // NOTE: A sequence read from a file, ready to swap in with SequencerRuntime.Replace
    public sealed record LoadedSequence(int Tempo, Quantisation Quantisation, IReadOnlyList<Node> Nodes)
    {
        public static LoadedSequence Empty(int tempo = 120) =>
            new(tempo, new Quantisation(1, 16), []);
    }
}
