namespace Mythwake.Core.Math
{
    /// <summary>Deterministic PCG32 random generator. Identical output on every platform for the same seed.</summary>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private ulong _state;
        private readonly ulong _increment;

        public Pcg32(ulong seed, ulong stream = 54)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            var old = _state;
            _state = unchecked(old * Multiplier + _increment);
            var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            var rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform integer in [0, maxExclusive).</summary>
        public int Range(int maxExclusive)
        {
            if (maxExclusive <= 1)
                return 0;
            var bound = (uint)maxExclusive;
            var threshold = unchecked((uint)-bound) % bound;
            while (true)
            {
                var r = NextUInt();
                if (r >= threshold)
                    return (int)(r % bound);
            }
        }

        public int Range(int minInclusive, int maxExclusive) => minInclusive + Range(maxExclusive - minInclusive);

        /// <summary>True with probability permille/1000.</summary>
        public bool Chance(int permille) => permille >= 1000 || (permille > 0 && Range(1000) < permille);
    }
}
