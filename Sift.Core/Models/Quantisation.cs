namespace Sift.Core.Models
{
    public struct Quantisation
    {
        private const int MAX_VALUE = 1024;

        public int _numerator;
        public int Numerator 
        {
            readonly get => _numerator;
            set
            {
                if(value <= MAX_VALUE && value >= 0)
                    _numerator = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int _denominator;
        public int Denominator 
        {
            readonly get => _denominator;
            set
            {
                if(value <= MAX_VALUE && value >= 0)
                    _denominator = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Quantisation(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }
    }
}