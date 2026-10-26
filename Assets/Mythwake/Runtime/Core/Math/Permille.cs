namespace Mythwake.Core.Math
{
    /// <summary>Fixed-point helpers. Percentages are stored as permille (1000 = 100 %).</summary>
    public static class Permille
    {
        public const int One = 1000;

        public static long Apply(long value, int permille) => value * permille / One;

        public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        public static long Clamp(long value, long min, long max) => value < min ? min : value > max ? max : value;
    }
}
