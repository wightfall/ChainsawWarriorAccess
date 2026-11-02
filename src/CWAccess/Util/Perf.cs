using System;
using System.Diagnostics;

namespace CWAccess
{
    /// <summary>
    /// Debug command "perf [seconds]": how long the mod's work takes each frame and how often the garbage collector
    /// runs meanwhile, written to the log.
    /// </summary>
    internal static class Perf
    {
        private static bool on;
        private static float until;
        private static int frames;
        private static long ticks;
        private static long maxTicks;
        private static int collections;
        private static long memory;

        public static bool Measuring => on;

        public static void Start(float now, float seconds)
        {
            on = true;
            until = now + seconds;
            frames = 0;
            ticks = 0;
            maxTicks = 0;
            collections = GC.CollectionCount(0);
            memory = GC.GetTotalMemory(false);
            Log.Info("perf: measuring for " + seconds + " s");
        }

        public static long Now()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <summary>One frame of the mod took (Now() - started).</summary>
        public static void Frame(long started, float now)
        {
            long t = Stopwatch.GetTimestamp() - started;
            frames++;
            ticks += t;
            if (t > maxTicks) maxTicks = t;
            if (now < until) return;
            on = false;
            double us = 1000000.0 / Stopwatch.Frequency;
            Log.Info(string.Format("perf: {0} frames, mod work per frame {1:0.0} us on average, {2:0} us at most, {3} garbage collections, heap {4:+0;-0} KB",
                frames, frames > 0 ? ticks * us / frames : 0, maxTicks * us, GC.CollectionCount(0) - collections, (GC.GetTotalMemory(false) - memory) / 1024));
        }
    }
}
