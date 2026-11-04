using System.Diagnostics;

namespace OdxDiagnosticParser.Tester.Tests;

/// <summary>
/// Lightweight, self-contained test assertion runner for testing all parser functions directly inside the Tester project.
/// </summary>
public class TestRunner
{
    public int TotalPassed { get; private set; }
    public int TotalFailed { get; private set; }
    public int TotalExecuted => TotalPassed + TotalFailed;
    public List<string> FailureMessages { get; } = new();

    public void RunTest(string testName, Action testAction)
    {
        Console.Write($"  [RUN] {testName,-60}");
        var sw = Stopwatch.StartNew();

        try
        {
            testAction();
            sw.Stop();
            TotalPassed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" [PASS] ({sw.ElapsedMilliseconds} ms)");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            sw.Stop();
            TotalFailed++;
            var errorMsg = $"{testName}: {ex.Message}";
            FailureMessages.Add(errorMsg);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" [FAIL] ({sw.ElapsedMilliseconds} ms)");
            Console.WriteLine($"        -> Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    public void PrintSummary()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        if (TotalFailed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  TEST RESULTS: ALL {TotalPassed} TESTS PASSED SUCCESSFULLY!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  TEST RESULTS: {TotalPassed} PASSED, {TotalFailed} FAILED out of {TotalExecuted} tests.");
            Console.WriteLine("\n  Failures:");
            foreach (var fail in FailureMessages)
            {
                Console.WriteLine($"    * {fail}");
            }
            Console.ResetColor();
        }
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    // Assertion helpers
    public static void AssertTrue(bool condition, string message = "Expected true, got false.")
    {
        if (!condition) throw new Exception(message);
    }

    public static void AssertFalse(bool condition, string message = "Expected false, got true.")
    {
        if (condition) throw new Exception(message);
    }

    public static void AssertNotNull(object? obj, string message = "Expected non-null value.")
    {
        if (obj == null) throw new Exception(message);
    }

    public static void AssertEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception(message ?? $"Equality failed. Expected: '{expected}', Actual: '{actual}'.");
        }
    }

    public static void AssertNotEmpty<T>(IEnumerable<T> collection, string message = "Collection is empty.")
    {
        if (collection == null || !collection.Any())
        {
            throw new Exception(message);
        }
    }

    public static void AssertThrows<TException>(Action action, string message = "Expected exception was not thrown.") where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return; // Expected
        }
        catch (Exception ex)
        {
            throw new Exception($"Expected exception '{typeof(TException).Name}', but caught '{ex.GetType().Name}': {ex.Message}");
        }

        throw new Exception(message);
    }
}

