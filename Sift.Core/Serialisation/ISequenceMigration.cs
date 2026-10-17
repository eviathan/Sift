using System.Text.Json.Nodes;

namespace Sift.Core.Serialisation
{
    // NOTE: Upgrades a saved sequence from FromVersion to FromVersion + 1 by rewriting its JSON, before it's read.
    // Register one per format change so files saved by any earlier version still load
    public interface ISequenceMigration
    {
        int FromVersion { get; }

        void Apply(JsonObject root);
    }
}
