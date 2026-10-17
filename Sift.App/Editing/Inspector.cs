using Paper.Core.Styles;
using Paper.Core.VirtualDom;
using Paper.Icons;
using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Snapshots;
using Direction = Sift.Core.Enums.Direction;

namespace Sift.App.Editing
{
    // NOTE: Builds the sidebar's editors for a node, its ports and their components. In C# rather than CSX because it's
    // logic-heavy, Sidebar.csx just places what this returns
    public static class Inspector
    {
        private const float PAD_CELL = 34f;
        private const float PAD_GAP = 4f;

        public static IEnumerable<UINode> Node(SiftHost host, NodeSnapshot node)
        {
            var id = node.Id;
            var position = node.Position;
            var colour = NodeColour(node);

            yield return Widgets.Section(null, [
                Widgets.HStack(
                    Widgets.Swatch(colour, 12f, 4f),
                    UI.Box(new StyleSheet { Width = Length.Px(6) }),
                    Widgets.TextField(node.Name ?? "", name => host.RenameNode(id, name), $"{id}:{node.Name}", title: true),
                    Widgets.Spacer(),
                    Widgets.GhostIcon(IconSets.Bs.BsArrowUpShort, () => host.MoveNodeLayer(id, 1), size: 18f),
                    Widgets.GhostIcon(IconSets.Bs.BsArrowDownShort, () => host.MoveNodeLayer(id, -1), size: 18f),
                    Widgets.GhostIcon(IconSets.Bs.BsTrash3, () => host.DeleteNode(id), colour: Theme.Error, size: 14f)),
                Widgets.TextOf($"{position.X}, {position.Y}  ·  Layer {position.Z}  ·  drag the tile to move it", "note"),
                Widgets.FieldRow("Starts on play", Widgets.Toggle(node.EmitOnPlay, on => host.SetEmitOnPlay(id, on))),
                Widgets.FieldRow("Active", Widgets.Toggle(node.IsActive, on => host.SetActive(id, on))),
            ]);

            var port = host.SelectedPort;
            var components = node.Ports.TryGetValue(port, out var list) ? list : [];

            yield return Widgets.Section("PORTS", [
                PortPad(host, node, port, colour),
                Widgets.TextOf("A side runs when a message arrives on it", "note"),
            ]);

            var portContent = new List<UINode>();

            if (host.AddingToPort == port)
                portContent.Add(AddMenu(host, id, port));

            foreach (var component in components)
                portContent.Add(Component(host, id, port, component));

            if (components.Count == 0 && host.AddingToPort != port)
                portContent.Add(Widgets.TextOf("Nothing here yet", "note"));

            yield return Widgets.Section(PortTitle(port).ToUpperInvariant(), portContent,
                host.AddingToPort == port
                    ? Widgets.GhostIcon(IconSets.Bs.BsXLg, () => host.ToggleAddMenu(port), size: 13f, active: true)
                    : Widgets.IconTextButton(IconSets.Bs.BsPlusLg, "Add", () => host.ToggleAddMenu(port)));
        }

        public static IEnumerable<UINode> EmptyCell(SiftHost host, Vector3D cell)
        {
            yield return Widgets.Section("EMPTY CELL", [
                Widgets.Row("Position", $"{cell.X}, {cell.Y}  ·  Layer {cell.Z}"),
                Widgets.HStack(Widgets.IconTextButton(IconSets.Bs.BsPlusLg, "Add node", () => host.AddNodeAt(cell))),
                Widgets.TextOf("Or double-click the cell", "note"),
            ]);
        }

        // NOTE: The node's six sides laid out like the tile: up/down/left/right round it, the layers above and below to
        // the side. Each shows the colour of what's on it
        private static UINode PortPad(SiftHost host, NodeSnapshot node, Direction selected, string nodeColour)
        {
            UINode Cell(Direction? port, IconRef? icon = null) =>
                port is { } side ? PortButton(host, node, side, side == selected, icon!.Value) : UI.Box(CellStyle());

            var centre = UI.Box(CellStyle(),
                UI.Box(new StyleSheet
                {
                    Width = Length.Px(22),
                    Height = Length.Px(22),
                    BorderRadius = 6,
                    Background = new PaperColour(nodeColour),
                }));

            UINode Row(params UINode[] cells) =>
                UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, MarginBottom = Length.Px(PAD_GAP) }, cells);

            var compass = UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Column, FlexShrink = 0f },
                Row(Cell(null), Cell(Direction.Up, IconSets.Bs.BsArrowUpShort), Cell(null)),
                Row(Cell(Direction.Left, IconSets.Bs.BsArrowLeftShort), centre, Cell(Direction.Right, IconSets.Bs.BsArrowRightShort)),
                Row(Cell(null), Cell(Direction.Down, IconSets.Bs.BsArrowDownShort), Cell(null)));

            var layers = UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Column, MarginLeft = Length.Px(18), FlexShrink = 0f },
                Labelled(Cell(Direction.Forwards, IconSets.Bs.BsChevronDoubleUp), "Layer above"),
                UI.Box(new StyleSheet { Height = Length.Px(PAD_CELL + PAD_GAP) }),
                Labelled(Cell(Direction.Backwards, IconSets.Bs.BsChevronDoubleDown), "Layer below"));

            return Widgets.HStack(compass, layers);
        }

        private static UINode Labelled(UINode cell, string label) =>
            UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center, MarginBottom = Length.Px(PAD_GAP) },
                cell,
                UI.Text(label, new StyleSheet { Color = new PaperColour(Theme.SecondaryText), FontSize = Length.Px(11), MarginLeft = Length.Px(8), FlexShrink = 0f }));

        private static UINode PortButton(SiftHost host, NodeSnapshot node, Direction port, bool selected, IconRef icon)
        {
            var components = node.Ports.TryGetValue(port, out var list) ? list : [];
            var colour = components.Count > 0 ? Theme.CategoryColour(components[0].Category) : null;

            return UI.Box(new PropsBuilder()
                .Style(CellStyle() with
                {
                    BorderRadius = 9,
                    Background = selected
                        ? new PaperColour(Theme.Accent).WithAlpha(0.2f)
                        : new PaperColour("#FFFFFF").WithAlpha(colour != null ? 0.09f : 0.04f),
                    Border = selected ? new BorderEdges(new Border(1.5f, new PaperColour(Theme.Accent))) : null,
                    Cursor = Cursor.Pointer,
                })
                .HoverStyle(new StyleSheet { Background = new PaperColour("#FFFFFF").WithAlpha(0.14f) })
                .OnClick(() => host.SelectPort(port))
                .Children(UI.Icon(icon, 20f, new PaperColour(colour ?? (selected ? Theme.Text : Theme.Dimmed))))
                .Build());
        }

        private static StyleSheet CellStyle() =>
            new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(PAD_CELL),
                Height = Length.Px(PAD_CELL),
                MarginRight = Length.Px(PAD_GAP),
                FlexShrink = 0f,
            };

        // NOTE: Every component type, grouped and coloured by category
        private static UINode AddMenu(SiftHost host, Guid nodeId, Direction port)
        {
            var groups = new List<UINode>();

            foreach (var (category, types) in host.ComponentTypesByCategory)
            {
                var colour = Theme.CategoryColour(category);

                groups.Add(UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Column, MarginBottom = Length.Px(8) },
                    Widgets.HStack(Widgets.Swatch(colour, 7f, 2f), UI.Text(ConfigFields.Humanise(category.ToString()).ToUpperInvariant(), new StyleSheet
                    {
                        Color = new PaperColour(colour),
                        FontSize = Length.Px(10),
                        FontWeight = FontWeight.Bold,
                        MarginLeft = Length.Px(6),
                    })),
                    UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, FlexWrap = FlexWrap.Wrap, MarginTop = Length.Px(5) },
                        types.Select(type => Widgets.Pill(DisplayName(type), () => host.AddComponent(nodeId, port, type))).ToArray())));
            }

            return UI.Box(new StyleSheet
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Padding = new Thickness(Length.Px(10)),
                BorderRadius = 10,
                Background = new PaperColour("#000000").WithAlpha(0.2f),
            }, groups.ToArray());
        }

        // NOTE: A component as a block with a bar in its category's colour down the left, its settings below its name
        private static UINode Component(SiftHost host, Guid nodeId, Direction port, ComponentSnapshot component)
        {
            var colour = Theme.CategoryColour(component.Category);
            var children = new List<UINode>
            {
                Widgets.HStack(
                    UI.Text(DisplayName(component.Type), new StyleSheet { Color = new PaperColour(Theme.Text), FontSize = Length.Px(13), FontWeight = FontWeight.Bold, FlexShrink = 0f }),
                    UI.Text("  " + ConfigFields.Humanise(component.Category.ToString()).ToLowerInvariant(), new StyleSheet { Color = new PaperColour(colour), FontSize = Length.Px(11), FlexShrink = 0f }),
                    Widgets.Spacer(),
                    Widgets.GhostIcon(IconSets.Bs.BsXLg, () => host.RemoveComponent(nodeId, port, component.Id), colour: Theme.SecondaryText, size: 12f))
            };

            foreach (var field in ConfigFields.Describe(component.Config))
                children.Add(Widgets.FieldRow(field.Label, Editor(host, nodeId, port, component, field)));

            return UI.Box(new StyleSheet
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Gap = Length.Px(4),
                PaddingLeft = Length.Px(10),
                PaddingTop = Length.Px(2),
                PaddingBottom = Length.Px(4),
                MarginBottom = Length.Px(6),
                BorderLeft = new Border(2f, new PaperColour(colour)),
            }, children.ToArray());
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
                    var index = Math.Max(0, field.Options.ToList().FindIndex(x => Equals(x, field.Value)));
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

        private static string NodeColour(NodeSnapshot node) =>
            SiftHost.PORT_ORDER.SelectMany(x => node.Ports.TryGetValue(x, out var components) ? components : []).FirstOrDefault() is { } primary
                ? Theme.CategoryColour(primary.Category)
                : Theme.Dimmed;

        private static string PortTitle(Direction port) => port switch
        {
            Direction.Forwards => "Layer above",
            Direction.Backwards => "Layer below",
            _ => port + " side"
        };

        public static string DisplayName(string type) => type switch
        {
            "midi" => "MIDI note",
            _ => char.ToUpperInvariant(type[0]) + type[1..]
        };
    }
}
