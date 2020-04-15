using System;
using System.Collections.Generic;
using System.Linq;

namespace DLCS.Core
{
    /// <summary>
    /// A collection of extension methods for working with strings.
    /// </summary>
    public static class StringX
    {
        /// <summary>
        /// Splits string containing csv values into IEnumerable{string}, removing empty entries.
        /// </summary>
        /// <param name="str">String to split</param>
        /// <returns>String split by ',', or empty list.</returns>
        public static IEnumerable<string> SplitCsvString(this string str)
            => str?.Trim().Split(",", StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();

        /// <summary>
        /// Splits string containing csv values into IEnumerable{T}, using passed function to convert results.
        /// </summary>
        /// <param name="str">String to split</param>
        /// <param name="converter">Func to convert from string to target type.</param>
        /// <typeparam name="T">Type of converter values.</typeparam>
        /// <returns>String split by ',', or empty list.</returns>
        public static IEnumerable<T> SplitCsvString<T>(this string str, Func<string, T> converter)
            => str.SplitCsvString().Select(converter);
    }
}