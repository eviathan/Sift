using Microsoft.Extensions.DependencyInjection;
using Sift.Core.Components;
using Sift.Core.Events;
using Sift.Core.Factories;
using Sift.Core.Interfaces;
using Sift.Core.MIDI;
using Sift.Core.Serialisation;

namespace Sift.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddSiftCore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Sequence>();
            serviceCollection.AddSingleton<Sequencer>();
            serviceCollection.AddSingleton<SequencerRuntime>();
            serviceCollection.AddSingleton<EventScheduler>();
            serviceCollection.AddSingleton<SpatialPropagator>();
            serviceCollection.AddSingleton<RuntimeEventHub>();
            serviceCollection.AddSingleton<ComponentRegistry>();
            serviceCollection.AddSingleton<ComponentFactory>();
            serviceCollection.AddSingleton<IMidiOutput, DryWetMidiOutput>();
            serviceCollection.AddSingleton<SequenceSerialiser>();

            return serviceCollection;
        }
    }
}
