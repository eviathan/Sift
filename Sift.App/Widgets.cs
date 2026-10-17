using Paper.Core.Hooks;
using Paper.Core.Styles;
using Paper.Core.VirtualDom;
using Paper.Icons;

namespace Sift.App
{
    // NOTE: Small building blocks for the CSX views. They live in C# because the CSX generator treats any
    // `UINode Name(...)` declaration in a .csx file, local functions included, as a component
    public static class Widgets
    {
        public static UINode TextOf(string text, string className) =>
            new(ElementTypes.Text, new PropsBuilder().Text(text).ClassName(className).Build());

        public static UINode BoxOf(string className, IEnumerable<UINode> children) =>
            UI.Box(new PropsBuilder().ClassName(className).Children(children).Build());

        public static UINode Row(string label, string value) =>
            BoxOf("row", [TextOf(label, "row-label"), TextOf(value, "row-value")]);

        // NOTE: A component type, coloured as the grid draws it
        public static UINode Chip(string type)
        {
            var colour = type switch
            {
                "midi" => Theme.Midi,
                "emit" => Theme.Emit,
                "redirect" => Theme.Redirect,
                _ => Theme.Log
            };

            return UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Padding = new Thickness(Length.Px(2), Length.Px(7)),
                    BorderRadius = 8,
                    Background = new PaperColour(colour).WithAlpha(0.3f),
                })
                .Children(TextOf(type.ToUpperInvariant(), "chip-text"))
                .Build());
        }

        // NOTE: Spot's quiet rounded button: white on a faint fill. Styled here rather than in App.csss because Paper
        // squeezes flex items whose size comes from a class (it leaves gap out of a row's natural width)
        // NOTE: Sized from the label because Paper under-measures a box around text (it leaves the padding out)
        public static UINode Pill(string label, Action onClick, string textColour = Theme.Text) =>
            UI.Box(new PropsBuilder()
                .Style(PillStyle(enabled: true, square: false) with { Width = Length.Px(MathF.Ceiling(label.Length * 6.6f) + 20f) })
                .HoverStyle(new StyleSheet { Background = new PaperColour("#FFFFFF").WithAlpha(0.16f) })
                .OnClick(onClick)
                .Children(UI.Text(label, new StyleSheet { Color = new PaperColour(textColour), FontSize = Length.Px(12), FlexShrink = 0f }))
                .Build());

        public static UINode IconPill(IconRef icon, Action onClick, bool enabled = true) =>
            UI.Box(new PropsBuilder()
                .Style(PillStyle(enabled, square: true))
                .HoverStyle(enabled ? new StyleSheet { Background = new PaperColour("#FFFFFF").WithAlpha(0.16f) } : StyleSheet.Empty)
                .OnClick(() => { if (enabled) onClick(); })
                .Children(UI.Icon(icon, 14f, new PaperColour(Theme.Text), new StyleSheet { FlexShrink = 0f }))
                .Build());

        private static StyleSheet PillStyle(bool enabled, bool square) =>
            new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Px(24),
                Width = square ? Length.Px(24) : null,
                MinWidth = Length.Px(24),
                Margin = new Thickness(Length.Px(0), Length.Px(2)),
                BorderRadius = 12,
                Background = new PaperColour("#FFFFFF").WithAlpha(0.08f),
                Opacity = enabled ? null : 0.35f,
                Cursor = enabled ? Cursor.Pointer : null,
                FlexShrink = 0f,
            };

        public static UINode Segment(string label, float width, bool active, Action onClick) =>
            UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Display = Display.Flex,
                    AlignItems = AlignItems.Center,
                    JustifyContent = JustifyContent.Center,
                    Width = Length.Px(width),
                    Height = Length.Px(22),
                    Margin = new Thickness(Length.Px(2)),
                    FlexShrink = 0f,
                    BorderRadius = 11,
                    Background = active ? new PaperColour("#FFFFFF").WithAlpha(0.16f) : new PaperColour("#000000").WithAlpha(0f),
                    Cursor = Cursor.Pointer,
                })
                .OnClick(onClick)
                .Children(UI.Text(label, new StyleSheet
                {
                    Color = new PaperColour(active ? Theme.Text : Theme.SecondaryText),
                    FontSize = Length.Px(12),
                    FlexShrink = 0f,
                }))
                .Build());

        // NOTE: As in Spot, the play button turns the accent green while playing
        public static UINode PlayButton(bool playing, Action onClick) =>
            UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Display = Display.Flex,
                    AlignItems = AlignItems.Center,
                    JustifyContent = JustifyContent.Center,
                    Width = Length.Px(36),
                    Height = Length.Px(36),
                    BorderRadius = 18,
                    Background = playing ? new PaperColour(Theme.Accent) : new PaperColour("#FFFFFF").WithAlpha(0.1f),
                    Cursor = Cursor.Pointer,
                })
                .OnClick(onClick)
                .Children(UI.Icon(playing ? IconSets.Bs.BsStopFill : IconSets.Bs.BsPlayFill, 18f, new PaperColour(playing ? "#000000" : Theme.Text)))
                .Build());

        public static UINode FieldRow(string label, UINode control) =>
            UI.Box(new PropsBuilder().ClassName("row").Children(TextOf(label, "row-label"), control).Build());

        public static UINode Stepper(string value, Action decrement, Action increment, bool canDecrement = true, bool canIncrement = true) =>
            Spinner(value, IconSets.Bs.BsDash, decrement, IconSets.Bs.BsPlus, increment, canDecrement, canIncrement);

        // NOTE: Steps through a list of options, wrapping at the ends
        public static UINode Cycle(string value, Action previous, Action next) =>
            Spinner(value, IconSets.Bs.BsChevronLeft, previous, IconSets.Bs.BsChevronRight, next, true, true);

        private static UINode Spinner(string value, IconRef down, Action onDown, IconRef up, Action onUp, bool canDown, bool canUp) =>
            UI.Box(new StyleSheet { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center, FlexShrink = 0f },
                IconPill(down, onDown, canDown),
                UI.Text(value, new StyleSheet
                {
                    Color = new PaperColour(Theme.Text),
                    FontSize = Length.Px(12),
                    MinWidth = Length.Px(64),
                    TextAlign = TextAlign.Center,
                    FlexShrink = 0f,
                }),
                IconPill(up, onUp, canUp));

        // NOTE: Spot's switch: accent green when on
        public static UINode Toggle(bool on, Action<bool> onChange) =>
            UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Display = Display.Flex,
                    AlignItems = AlignItems.Center,
                    JustifyContent = on ? JustifyContent.FlexEnd : JustifyContent.FlexStart,
                    Width = Length.Px(34),
                    Height = Length.Px(20),
                    Padding = new Thickness(Length.Px(2)),
                    BorderRadius = 10,
                    Background = on ? new PaperColour(Theme.Accent) : new PaperColour("#FFFFFF").WithAlpha(0.14f),
                    Cursor = Cursor.Pointer,
                    FlexShrink = 0f,
                })
                .OnClick(() => onChange(!on))
                .Children(UI.Box(new StyleSheet
                {
                    Width = Length.Px(16),
                    Height = Length.Px(16),
                    BorderRadius = 8,
                    Background = new PaperColour("#FFFFFF"),
                }))
                .Build());

        // NOTE: Edits a local draft and commits it on Enter or when focus leaves, so typing is a single undo step.
        // Give it a key that changes with the committed value so the draft resets on undo
        public static UINode TextField(string value, Action<string> onCommit, string key) =>
            UI.Component(TextFieldComponent, new PropsBuilder().Set("value", value).Set("onCommit", onCommit).Build(), key);

        private static UINode TextFieldComponent(Props props)
        {
            var value = props.Get<string>("value") ?? "";
            var onCommit = props.Get<Action<string>>("onCommit");
            var (draft, setDraft, _) = Hooks.UseState(value);

            void Commit()
            {
                if (draft != value)
                    onCommit?.Invoke(draft);
            }

            return new UINode(ElementTypes.Input, new PropsBuilder()
                .Text(draft)
                .OnChange(setDraft)
                .OnSubmit(Commit)
                .OnBlur(Commit)
                .Style(new StyleSheet
                {
                    Width = Length.Px(150),
                    MinWidth = Length.Px(150),
                    MaxWidth = Length.Px(150),
                    FlexGrow = 0f,
                    Height = Length.Px(24),
                    Padding = new Thickness(Length.Px(2), Length.Px(8)),
                    BorderRadius = 6,
                    Background = new PaperColour("#000000").WithAlpha(0.25f),
                    Color = new PaperColour(Theme.Text),
                    FontSize = Length.Px(12),
                    FlexShrink = 0f,
                })
                .Build());
        }

        // NOTE: A component on a port, as a slightly lighter card inside the port's card
        public static UINode SubCard(IEnumerable<UINode> children) =>
            UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Column,
                    Padding = new Thickness(Length.Px(8)),
                    Gap = Length.Px(6),
                    BorderRadius = 8,
                    Background = new PaperColour("#FFFFFF").WithAlpha(0.04f),
                })
                .Children(children)
                .Build());

        public static UINode Warning(string message) =>
            UI.Box(new PropsBuilder()
                .Style(new StyleSheet
                {
                    Display = Display.Flex,
                    AlignItems = AlignItems.Center,
                    Height = Length.Px(32),
                    FlexShrink = 0f,
                    Padding = new Thickness(Length.Px(0), Length.Px(10)),
                    BorderRadius = 8,
                    Background = new PaperColour(Theme.Warning).WithAlpha(0.15f),
                })
                .Children(UI.Text(message, new StyleSheet { Color = new PaperColour(Theme.Warning), FontSize = Length.Px(12) }))
                .Build());

        public static UINode LayerRow(int layer, int nodeCount, bool selected, Action onClick) =>
            UI.Box(new PropsBuilder()
                .ClassName("layer-row")
                .Style(selected ? new StyleSheet { Background = new PaperColour("#FFFFFF").WithAlpha(0.08f) } : StyleSheet.Empty)
                .OnClick(onClick)
                .Children(
                    UI.Text("Layer " + layer, new StyleSheet
                    {
                        Color = new PaperColour(selected ? Theme.Accent : Theme.Text),
                        FontSize = Length.Px(12),
                    }),
                    TextOf(nodeCount == 1 ? "1 node" : nodeCount + " nodes", "row-label"))
                .Build());
    }
}
