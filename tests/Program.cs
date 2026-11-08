using System;
using System.Linq;
using System.Reflection;

namespace KingmakerWakingRune.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            int passed = 0, failed = 0;
            var tests = typeof(Program).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => m.Name.StartsWith("Test", StringComparison.Ordinal) && m.GetParameters().Length == 0);
            foreach (MethodInfo test in tests)
            {
                try
                {
                    test.Invoke(null, null);
                    passed++;
                }
                catch (TargetInvocationException ex)
                {
                    failed++;
                    Console.WriteLine($"FAIL {test.DeclaringType.Name}.{test.Name}: {ex.InnerException?.Message}");
                }
            }
            Console.WriteLine($"Tests: {passed} passed, {failed} failed.");
            return failed == 0 ? 0 : 1;
        }
    }

    internal static class Assert
    {
        public static void Equal<T>(T expected, T actual, string what)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{what}: expected {expected}, got {actual}");
            }
        }
    }
}
