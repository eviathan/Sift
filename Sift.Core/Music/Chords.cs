namespace Sift.Core.Music
{
    public enum ChordQuality
    {
        Major,
        Minor,
        Diminished,
        Augmented,
        Sus2,
        Sus4,
        Major7,
        Minor7,
        Dominant7
    }

    public static class Chords
    {
        public static IReadOnlyList<int> Intervals(ChordQuality quality) => quality switch
        {
            ChordQuality.Major => [0, 4, 7],
            ChordQuality.Minor => [0, 3, 7],
            ChordQuality.Diminished => [0, 3, 6],
            ChordQuality.Augmented => [0, 4, 8],
            ChordQuality.Sus2 => [0, 2, 7],
            ChordQuality.Sus4 => [0, 5, 7],
            ChordQuality.Major7 => [0, 4, 7, 11],
            ChordQuality.Minor7 => [0, 3, 7, 10],
            ChordQuality.Dominant7 => [0, 4, 7, 10],
            _ => [0]
        };
    }
}
