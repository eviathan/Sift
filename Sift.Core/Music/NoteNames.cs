using System.Text.RegularExpressions;

namespace Sift.Core.Music
{
    public static partial class NoteNames
    {
        private static readonly string[] NAMES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

        // NOTE: Middle C (60) is C4
        public static string Name(int note) =>
            NAMES[((note % 12) + 12) % 12] + (Math.DivRem(note, 12, out var remainder) - (remainder < 0 ? 1 : 0) - 1);

        // NOTE: "C4 E4 G#4 Bb3 72", separated by spaces or commas. Names or MIDI numbers, sharps (#) and flats (b)
        public static IReadOnlyList<int> ParseList(string text)
        {
            var notes = new List<int>();

            foreach (var token in text.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries))
                notes.Add(Parse(token));

            return notes;
        }

        public static int Parse(string token)
        {
            if (int.TryParse(token, out var number))
                return number is >= 0 and <= 127 ? number : throw new FormatException($"'{token}' is outside 0-127.");

            var match = NotePattern().Match(token);

            if (!match.Success)
                throw new FormatException($"'{token}' isn't a note, use names like C4, F#3, Bb2 or numbers 0-127.");

            var semitone = char.ToUpperInvariant(match.Groups[1].Value[0]) switch
            {
                'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11, _ => 0
            };

            semitone += match.Groups[2].Value switch { "#" => 1, "b" => -1, _ => 0 };

            var note = (int.Parse(match.Groups[3].Value) + 1) * 12 + semitone;

            return note is >= 0 and <= 127 ? note : throw new FormatException($"'{token}' is outside the MIDI range.");
        }

        [GeneratedRegex(@"^([A-Ga-g])(#|b)?(-?\d)$")]
        private static partial Regex NotePattern();
    }
}
