using System.Collections;
using System.Collections.Generic;

namespace AnnoRDA.IO.Encryption
{
    public class LinearCongruentialGenerator : IEnumerator<short>
    {
        private readonly int multiplier;
        private readonly int increment;
        private readonly int seed;

        private int x;

        public LinearCongruentialGenerator(int seed, int multiplier = 214013, int increment = 2531011)
        {
            this.multiplier = multiplier;
            this.increment = increment;
            this.seed = seed;

            this.Reset();
        }

        public void Dispose()
        {
        }

        public short Current {
            get { return (short)(this.x >> 16 & short.MaxValue); }
        }

        object IEnumerator.Current {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            this.x = this.multiplier * this.x + this.increment;
            return true;
        }

        public void Reset()
        {
            this.x = this.seed;
        }
    }
}
