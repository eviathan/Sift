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
        public static UINode Pill(string label, Action onClick, string textColour = Theme.Text) =>
            UI.Box(new PropsBuilder()
                .Style(PillStyle(enabled: true, square: false))
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
                Padding = square ? null : new Thickness(Length.Px(0), Length.Px(10)),
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
