using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Nodes;

namespace Sift.CLI
{
    // TODO: Replace with loading a .sift.json once serialisation lands
    public static class DemoScene
    {
        // Emitter --3 cells--> Bouncer A <--2 cells--> Bouncer B (ping-pongs forever)
        public static void Build(Sequence sequence, ComponentFactory components)
        {
            var emitter = new Node(new Vector3D(0, 0, 0)) { Name = "Emitter", EmitOnPlay = true };
            emitter.GetPort(Direction.Right).Add(components.Create(new EmitConfig()));

            var bouncerA = new Node(new Vector3D(3, 0, 0)) { Name = "Bouncer A" };
            bouncerA.GetPort(Direction.Left)
                .Add(components.Create(new LogConfig()))
                .Add(components.Create(new RedirectConfig(Direction.Up)));
            bouncerA.GetPort(Direction.Up)
                .Add(components.Create(new LogConfig()))
                .Add(components.Create(new RedirectConfig(Direction.Up)));

            var bouncerB = new Node(new Vector3D(3, 2, 0)) { Name = "Bouncer B" };
            bouncerB.GetPort(Direction.Down)
                .Add(components.Create(new LogConfig()))
                .Add(components.Create(new RedirectConfig(Direction.Down)));

            sequence.Grid.AddNode(emitter);
            sequence.Grid.AddNode(bouncerA);
            sequence.Grid.AddNode(bouncerB);
        }
    }
}
