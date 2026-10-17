using Sift.Core.Commands;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Nodes;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    public class EditingTests
    {
        [Fact]
        public void Commands_apply_immediately_while_stopped_and_undo_and_redo()
        {
            using var sift = new SiftFixture();
            var runtime = sift.Runtime;
            var node = new Node(new(1, 1, 1));

            runtime.Submit(new AddNode(node)).Wait();
            runtime.Submit(new MoveNode(node.Id, new(4, 4, 4))).Wait();
            Assert.Equal(new Vector3D(4, 4, 4), node.Position);

            Assert.True(runtime.Undo().Result);
            Assert.Equal(new Vector3D(1, 1, 1), node.Position);

            Assert.True(runtime.Undo().Result);
            Assert.Equal(0, sift.Sequence.Grid.Count);
            Assert.False(runtime.Undo().Result);

            runtime.Redo().Wait();
            runtime.Redo().Wait();
            Assert.Equal(1, sift.Sequence.Grid.Count);
            Assert.Equal(new Vector3D(4, 4, 4), node.Position);
        }

        [Fact]
        public void Removing_a_component_can_be_undone()
        {
            using var sift = new SiftFixture();
            var node = sift.Add(new Node(new(0, 0, 0)));
            var component = sift.Components.Create(new LogConfig());

            sift.Runtime.Submit(new AddComponent(node.Id, Direction.Up, component)).Wait();
            sift.Runtime.Submit(new RemoveComponent(node.Id, Direction.Up, component.Id)).Wait();
            Assert.Empty(node.GetPort(Direction.Up).Components);

            sift.Runtime.Undo().Wait();
            Assert.Contains(component, node.GetPort(Direction.Up).Components);
        }

        [Fact]
        public void Adding_onto_an_occupied_cell_is_rejected()
        {
            using var sift = new SiftFixture();
            sift.Add(new Node(new(4, 4, 4)));

            var result = sift.Runtime.Submit(new AddNode(new Node(new(4, 4, 4))));

            Assert.True(result.IsFaulted);
            Assert.Equal(1, sift.Sequence.Grid.Count);
        }

        [Fact]
        public void Component_commands_are_dropped_while_stopped()
        {
            using var sift = new SiftFixture();
            var node = sift.Add(new Node(new(0, 0, 0)));

            var result = ((ISequencerContext)sift.Runtime).Submit(new MoveNode(node.Id, new(9, 9, 9)));

            Assert.True(result.IsCanceled);
            Assert.Equal(new Vector3D(0, 0, 0), node.Position);
        }

        [Fact]
        public void Component_config_is_validated_on_create_and_update()
        {
            using var sift = new SiftFixture();

            Assert.ThrowsAny<ArgumentException>(() => sift.Components.Create(new MidiConfig(Note: 200)));
            Assert.ThrowsAny<ArgumentException>(() => sift.Components.Create(new MidiConfig(Channel: 0)));

            var component = sift.Components.Create(new MidiConfig());
            var node = sift.Add(sift.Build(new Node(new(0, 0, 0)), Direction.Left));
            node.GetPort(Direction.Left).Add(component);

            Assert.True(sift.Runtime.Submit(new UpdateComponentConfig(node.Id, Direction.Left, component.Id, new MidiConfig(Velocity: 0))).IsFaulted);
            Assert.Equal(100, ((MidiConfig)component.Config).Velocity);

            Assert.True(sift.Runtime.Submit(new UpdateComponentConfig(node.Id, Direction.Left, component.Id, new MidiConfig(Velocity: 50))).IsCompletedSuccessfully);
            Assert.Equal(50, ((MidiConfig)component.Config).Velocity);
        }

        [Fact]
        public void Unknown_config_types_are_rejected()
        {
            using var sift = new SiftFixture();

            Assert.Throws<InvalidOperationException>(() => sift.Components.Create(new UnregisteredConfig()));
        }

        private sealed record UnregisteredConfig : IComponentConfig;
    }
}
