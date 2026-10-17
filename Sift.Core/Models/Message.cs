namespace Sift.Core.Models
{
    public class Message
    {
        public string Payload { get; set; } = string.Empty;

        // NOTE: Semitones added to every note played by the message from here on, accumulated by Transpose components
        public int Transpose { get; init; }

        public Message WithTranspose(int semitones) =>
            new() { Payload = Payload, Transpose = Transpose + semitones };
    }
}
