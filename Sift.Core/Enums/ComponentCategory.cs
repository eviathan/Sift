namespace Sift.Core.Enums
{
    // NOTE: What a component is for, used by the UI to colour, group and draw it
    public enum ComponentCategory
    {
        // NOTE: Starts messages (emit, clock)
        Source,

        // NOTE: Decides where messages go (redirect, split, rotate, chance, gate…)
        Flow,

        // NOTE: Changes when messages go (delay)
        Time,

        // NOTE: Makes sound or changes it (midi, notes, chord, transpose)
        Sound,

        // NOTE: Changes the grid itself (move)
        Motion,

        Debug
    }
}
