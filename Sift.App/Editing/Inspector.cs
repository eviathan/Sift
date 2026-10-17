using Paper.Core.Styles;
using Paper.Core.VirtualDom;
using Paper.Icons;
using Sift.Core.Enums;
using Direction = Sift.Core.Enums.Direction;
using Sift.Core.Grid;
using Sift.Core.Snapshots;

namespace Sift.App.Editing
{
    // NOTE: Builds the sidebar's editors for a node, its ports and their components. In C# rather than CSX because it's
    // logic-heavy, Sidebar.csx just places what this returns
    public static class Inspector
    {
        private static readonly Direction[] PORTS = [Direction.Up, Direction.Right, Direction.Down, Direction.Left, Direction.Forwards, Direction.Backwards];

        public static IEnumerable<UINode> Node(SiftHost host, NodeSnapshot node)
        {
            var id = node.Id;
            var position = node.Position;

            yield return Widgets.TextOf("NODE", "heading");
            yield return Widgets.BoxOf("card", [
                Widgets.FieldRow("Name", Widgets.TextField(node.Name ?? "", name => host.RenameNode(id, name), $"{id}:{node.Name}")),
                Widgets.Row("Position", $"{position.X}, {position.Y}  (layer {position.Z})"),
                Widgets.FieldRow("Starts on play", Widgets.Toggle(node.EmitOnPlay, on => host.SetEmitOnPlay(id, on))),
                Widgets.FieldRow("Active", Widgets.Toggle(node.IsActive, on => host.SetActive(id, on))),
                UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, MarginTop = Length.Px(4) },
                    Widgets.Pill("Layer up", () => host.MoveNodeLayer(id, 1)),
                    Widgets.Pill("Layer down", () => host.MoveNodeLayer(id, -1)),
                    UI.Box(new StyleSheet { FlexGrow = 1f }),
                    Widgets.Pill("Delete", () => host.DeleteNode(id), Theme.Error)),
                Widgets.TextOf("Drag the tile to move it", "note"),
            ]);

            yield return Widgets.TextOf("PORTS", "heading");
            yield return Widgets.TextOf("A port's components run when a message", "note");
            yield return Widgets.TextOf("arrives on that side, and emit out of it", "note");

            foreach (var port in PORTS)
                yield return Port(host, node, port);
        }

        public static IEnumerable<UINode> EmptyCell(SiftHost host, Vector3D cell)
        {
            yield return Widgets.TextOf("EMPTY CELL", "heading");
            yield return Widgets.BoxOf("card", [
                Widgets.Row("Position", $"{cell.X}, {cell.Y}  (layer {cell.Z})"),
                UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row },
                    Widgets.Pill("Add node", () => host.AddNodeAt(cell))),
                Widgets.TextOf("Or double-click the cell", "note"),
            ]);
        }

        private static UINode Port(SiftHost host, NodeSnapshot node, Direction port)
        {
            var id = node.Id;
            var components = node.Ports.TryGetValue(port, out var list) ? list : [];
            var children = new List<UINode>
            {
                Widgets.FieldRow(PortLabel(port), Widgets.IconPill(host.AddingToPort == port ? IconSets.Bs.BsX : IconSets.Bs.BsPlus, () => host.ToggleAddMenu(port)))
            };

            if (host.AddingToPort == port)
            {
                children.Add(UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, FlexWrap = FlexWrap.Wrap },
                    host.ComponentTypes.Select(type => Widgets.Pill(ConfigFields.Humanise(Capitalise(type)), () => host.AddComponent(id, port, type), TypeColour(type))).ToArray()));
            }

            foreach (var component in components)
                children.Add(Component(host, id, port, component));

            if (components.Count == 0 && host.AddingToPort != port)
                children.Add(Widgets.TextOf("Empty", "note"));

            return Widgets.BoxOf("card", children);
        }

        private static UINode Component(SiftHost host, Guid nodeId, Direction port, ComponentSnapshot component)
        {
            var children = new List<UINode>
            {
                UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.SpaceBetween },
                    Widgets.Chip(component.Type),
                    Widgets.IconPill(IconSets.Bs.BsX, () => host.RemoveComponent(nodeId, port, component.Id)))
            };

            foreach (var field in ConfigFields.Describe(component.Config))
                children.Add(Widgets.FieldRow(field.Label, Editor(host, nodeId, port, component, field)));

            return Widgets.SubCard(children);
        }

        private static UINode Editor(SiftHost host, Guid nodeId, Direction port, ComponentSnapshot component, ConfigField field)
        {
            void Set(object? value) =>
                host.UpdateComponent(nodeId, port, component.Id, ConfigFields.With(component.Config, field.Name, value));

            switch (field.Kind)
            {
                case FieldKind.Integer:
                    var number = (int)(field.Value ?? 0);
                    return Widgets.Stepper(ConfigFields.Format(field, number),
                        () => Set(number - 1), () => Set(number + 1),
                        number > field.Minimum, number < field.Maximum);

                case FieldKind.Boolean:
                    return Widgets.Toggle(field.Value is true, on => Set(on));

                case FieldKind.Choice:
                    var index = Math.Max(0, field.Options.ToList().IndexOf(field.Value!));
                    var count = field.Options.Count;
                    return Widgets.Cycle(ConfigFields.Format(field, field.Value),
                        () => Set(field.Options[(index - 1 + count) % count]),
                        () => Set(field.Options[(index + 1) % count]));

                case FieldKind.Text:
                    var text = field.Value as string ?? "";
                    return Widgets.TextField(text, value => Set(value), $"{component.Id}:{field.Name}:{text}");

                default:
                    return Widgets.TextOf("Not editable here", "note");
            }
        }

        private static string PortLabel(Direction port) => port switch
        {
            Direction.Forwards => "Forwards (layer above)",
            Direction.Backwards => "Backwards (layer below)",
            _ => port.ToString()
        };

        private static string Capitalise(string text) =>
            text == "midi" ? "MIDI" : text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

        private static string TypeColour(string type) => type switch
        {
            "midi" => Theme.Midi,
            "emit" => Theme.Emit,
            "redirect" => Theme.Redirect,
            _ => Theme.Text
        };
    }
}
