using System;
using System.Collections.Generic;

namespace launcher {
    public static class RandomUtil {
        private static readonly Random r = new Random();

        public static void shuffleArray<T>(T[] a) {
            for (var i = a.Length - 1; i > 0; --i) {
                var swapIndex = r.Next(i + 1);
                var swap = a[swapIndex];
                a[swapIndex] = a[i];
                a[i] = swap;
            }
        }

        public static void shuffleList<T>(IList<T> l) {
            for (var i = l.Count; i > 0; --i) {
                var swapIndex = r.Next(i + 1);
                var swap = l[swapIndex];
                l[swapIndex] = l[i];
                l[i] = swap;
            }
        }

        public static int rand(int lowerBound, int upperBound) {
            return r.Next(lowerBound, upperBound);
        }

        public static double rand(double lowerBound, double upperBound) {
            return r.NextDouble() * (upperBound - lowerBound) + lowerBound;
        }

        public static double rand() {
            return r.NextDouble();
        }
    }
}
