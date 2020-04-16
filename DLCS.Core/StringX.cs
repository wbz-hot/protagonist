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
            => str.SplitSeparatedString(",");

        /// <summary>
        /// Splits string containing csv values into IEnumerable{T}, using passed function to convert results.
        /// </summary>
        /// <param name="str">String to split</param>
        /// <param name="converter">Func to convert from string to target type.</param>
        /// <typeparam name="T">Type of converter values.</typeparam>
        /// <returns>String split by ',', or empty list.</returns>
        public static IEnumerable<T> SplitCsvString<T>(this string str, Func<string, T> converter)
            => str.SplitCsvString().Select(converter);
        
        /// <summary>
        /// Splits string containing separated values into IEnumerable{T}, using specified separator.
        /// </summary>
        /// <param name="str">String to split</param>
        /// <param name="separator">String to split by.</param>
        /// <returns>String split, or empty list.</returns>
        public static IEnumerable<string> SplitSeparatedString(this string str, string separator)
            => str?.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
    }
}