namespace Test.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exception thrown when a Touchstone test assertion fails. Kept framework-agnostic so
    /// Test.Shared depends only on Touchstone.Core (no xUnit/NUnit types leak into the shared library).
    /// </summary>
    public sealed class TestAssertionException : Exception
    {
        /// <summary>
        /// Instantiate the exception.
        /// </summary>
        /// <param name="message">Failure message.</param>
        public TestAssertionException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Minimal, dependency-free assertion helpers. Each throws <see cref="TestAssertionException"/>
    /// on failure, which every Touchstone host (CLI, xUnit, NUnit) surfaces as a failed test.
    /// </summary>
    public static class TestAssert
    {
        /// <summary>
        /// Assert that a condition is true.
        /// </summary>
        public static void True(bool condition, string message = null)
        {
            if (!condition) throw new TestAssertionException(message ?? "Expected condition to be true.");
        }

        /// <summary>
        /// Assert that a condition is false.
        /// </summary>
        public static void False(bool condition, string message = null)
        {
            if (condition) throw new TestAssertionException(message ?? "Expected condition to be false.");
        }

        /// <summary>
        /// Assert that a reference is not null.
        /// </summary>
        public static void NotNull(object value, string message = null)
        {
            if (value == null) throw new TestAssertionException(message ?? "Expected value to be non-null.");
        }

        /// <summary>
        /// Assert that a reference is null.
        /// </summary>
        public static void Null(object value, string message = null)
        {
            if (value != null) throw new TestAssertionException(message ?? $"Expected null but got [{value}].");
        }

        /// <summary>
        /// Assert that two values are equal using the default equality comparer.
        /// </summary>
        public static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new TestAssertionException(message ?? $"Expected [{Format(expected)}] but got [{Format(actual)}].");
        }

        /// <summary>
        /// Assert that two values are not equal using the default equality comparer.
        /// </summary>
        public static void NotEqual<T>(T notExpected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new TestAssertionException(message ?? $"Expected value to differ from [{Format(notExpected)}].");
        }

        /// <summary>
        /// Assert that a string is null or empty.
        /// </summary>
        public static void NullOrEmpty(string value, string message = null)
        {
            if (!string.IsNullOrEmpty(value)) throw new TestAssertionException(message ?? $"Expected null or empty but got [{value}].");
        }

        /// <summary>
        /// Assert that a string is not null or empty.
        /// </summary>
        public static void NotNullOrEmpty(string value, string message = null)
        {
            if (string.IsNullOrEmpty(value)) throw new TestAssertionException(message ?? "Expected a non-empty string.");
        }

        /// <summary>
        /// Assert that a string contains a substring.
        /// </summary>
        public static void Contains(string substring, string actual, string message = null)
        {
            if (actual == null || !actual.Contains(substring))
                throw new TestAssertionException(message ?? $"Expected string to contain [{substring}] but got [{actual}].");
        }

        /// <summary>
        /// Assert that a string starts with a prefix.
        /// </summary>
        public static void StartsWith(string prefix, string actual, string message = null)
        {
            if (actual == null || !actual.StartsWith(prefix))
                throw new TestAssertionException(message ?? $"Expected string to start with [{prefix}] but got [{actual}].");
        }

        /// <summary>
        /// Assert that a collection has the expected count.
        /// </summary>
        public static void Count(int expected, ICollection collection, string message = null)
        {
            if (collection == null) throw new TestAssertionException(message ?? "Expected a non-null collection.");
            if (collection.Count != expected)
                throw new TestAssertionException(message ?? $"Expected count [{expected}] but got [{collection.Count}].");
        }

        /// <summary>
        /// Assert that the provided asynchronous action throws an exception of the specified type
        /// (or a subtype). Returns the thrown exception for further inspection.
        /// </summary>
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string message = null)
            where TException : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new TestAssertionException(message ?? $"Expected exception of type [{typeof(TException).Name}] but got [{ex.GetType().Name}]: {ex.Message}");
            }

            throw new TestAssertionException(message ?? $"Expected exception of type [{typeof(TException).Name}] but none was thrown.");
        }

        /// <summary>
        /// Assert that the provided asynchronous action throws any exception. Returns it for inspection.
        /// </summary>
        public static async Task<Exception> ThrowsAnyAsync(Func<Task> action, string message = null)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex;
            }

            throw new TestAssertionException(message ?? "Expected an exception but none was thrown.");
        }

        /// <summary>
        /// Fail unconditionally with a message.
        /// </summary>
        public static void Fail(string message)
        {
            throw new TestAssertionException(message ?? "Test failed.");
        }

        private static string Format(object value)
        {
            return value == null ? "null" : value.ToString();
        }
    }
}
