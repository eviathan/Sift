namespace Sift.Core.Serialisation
{
    public class SequenceFormatException : Exception
    {
        public SequenceFormatException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
