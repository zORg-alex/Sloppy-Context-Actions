using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class ContextActionPerformance
    {
        private const double SlowLookupSeconds = 0.25d;
        private static readonly HashSet<string> ReportedOperations = new();

        public static IDisposable Measure(string operation, string optimizationContext)
        {
            return new Measurement(operation, optimizationContext);
        }

        private sealed class Measurement : IDisposable
        {
            private readonly string _operation;
            private readonly string _optimizationContext;
            private readonly long _startedAt;

            public Measurement(string operation, string optimizationContext)
            {
                _operation = operation;
                _optimizationContext = optimizationContext;
                _startedAt = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                double seconds =
                    (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency;
                if (seconds < SlowLookupSeconds || !ReportedOperations.Add(_operation)) return;

                int milliseconds = Mathf.RoundToInt((float)(seconds * 1000d));
                UnityEngine.Debug.LogError(
                    $"Sloppy Context Actions: '{_operation}' took {milliseconds} ms. " +
                    "This editor lookup exceeded the 250 ms budget and should be optimized. " +
                    "This warning is emitted once per Unity domain reload.\n\n" +
                    "Copy the following prompt into Codex or your preferred LLM:\n" +
                    $"Optimize the Unity Editor lookup '{_operation}' in the Sloppy Context Actions plugin. " +
                    $"It took {milliseconds} ms in this project. {_optimizationContext} " +
                    "Preserve behavior, profile the actual bottleneck, cache reusable results, and invalidate " +
                    "the cache only on relevant events such as project changes, script compilation, package " +
                    "changes, or assembly reload. Avoid expensive work in OnGUI and EditorApplication.update. " +
                    "Also identify providers or searches unused by this project that can be disabled or removed. " +
                    "Explain the tradeoffs and verify the optimized editor code in the current Unity version.");
            }
        }
    }

    internal static class ContextActionTypeCache
    {
        private static readonly Dictionary<string, Type> Types = new();

        public static Type Find(string fullName)
        {
            if (Types.TryGetValue(fullName, out Type cachedType)) return cachedType;

            using (ContextActionPerformance.Measure(
                       "Loaded type lookup: " + fullName,
                       "The lookup currently searches every loaded AppDomain assembly on a cache miss."))
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType(fullName, throwOnError: false);
                    if (type == null) continue;

                    Types[fullName] = type;
                    return type;
                }

                Types[fullName] = null;
                return null;
            }
        }
    }
}
