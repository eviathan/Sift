using System.Globalization;
using System.Numerics;
using Sift.Core.Enums;

namespace Sift.App
{
    // NOTE: Spot's default theme (SpotCore/Resources/Themes/default.yaml) so the two apps feel like a family
    public static class Theme
    {
        // The window
        public const string Page = "#262C3D";
        public const string Panel = "#2A2D3D";
        public const string Card = "#222530";
        public const string Header = "#CBCACC";
        public const string Footer = "#000000";
        public const string Text = "#FFFFFF";
        public const string SecondaryText = "#8B96B2";
        public const string Dimmed = "#3C4661";
        public const string Accent = "#8CD973";
        public const string Warning = "#FF9F0A";
        public const string Error = "#FF8080";

        // The sequence
        public const string Playhead = "#FF9F0A";
        public const string Message = "#64D2FF";
        public const string Emit = "#8CD973";
        public const string Redirect = "#0A84FF";
        public const string Midi = "#9E73F2";
        public const string Log = "#8B96B2";

        // Component categories
        public const string Source = "#8CD973";
        public const string Flow = "#0A84FF";
        public const string Time = "#FFD60A";
        public const string Sound = "#9E73F2";
        public const string Motion = "#FF9F0A";
        public const string Debug = "#8B96B2";

        public static string CategoryColour(ComponentCategory category) => category switch
        {
            ComponentCategory.Source => Source,
            ComponentCategory.Flow => Flow,
            ComponentCategory.Time => Time,
            ComponentCategory.Sound => Sound,
            ComponentCategory.Motion => Motion,
            _ => Debug
        };

        // NOTE: Which category a node shows as when it has several, the one that says most about what it does
        public static int CategoryPriority(ComponentCategory category) => category switch
        {
            ComponentCategory.Sound => 5,
            ComponentCategory.Source => 4,
            ComponentCategory.Motion => 3,
            ComponentCategory.Time => 2,
            ComponentCategory.Flow => 1,
            _ => 0
        };

        public static Vector4 Rgba(string hex, float alpha = 1f)
        {
            var value = hex.TrimStart('#');

            return new Vector4(
                int.Parse(value[0..2], NumberStyles.HexNumber) / 255f,
                int.Parse(value[2..4], NumberStyles.HexNumber) / 255f,
                int.Parse(value[4..6], NumberStyles.HexNumber) / 255f,
                alpha);
        }

        public static Vector4 White(float alpha) =>
            new(1f, 1f, 1f, alpha);
    }
}
