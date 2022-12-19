using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VergilBot.Modules
{
    public class ThreadLocalRandom
    {
        private static readonly Random globalRandom = new Random();
        private static readonly object globalLock = new object();
        private static readonly ThreadLocal<Random> threadRandom = new ThreadLocal<Random>(NewRandom);

        /// <summary>
        /// Creates a new instance of Random. The seed is derived
        /// from a global (static) instance of Random, rather
        /// than time.
        /// </summary>
        public static Random NewRandom()
        {
            lock (globalLock)
            {
                return new Random(globalRandom.Next());
            }
        }

        /// <summary>
        /// Returns an instance of Random which can be used freely
        /// within the current thread.
        /// </summary>
        public static Random Instance { get { return threadRandom.Value; } }

        public static int Next(int minValue, int maxValue)
        { 
            return Instance.Next(minValue, maxValue);
        }
    }
}
