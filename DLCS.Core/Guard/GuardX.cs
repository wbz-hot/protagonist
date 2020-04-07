using System;

namespace DLCS.Core.Guard
{
    /// <summary>
    /// A collection of guard extensions.
    /// </summary>
    public static class GuardX
    {
        /// <summary>
        /// Throw <see cref="ArgumentNullException"/> if provided value is null.
        /// </summary>
        /// <param name="argument">Argument to check.</param>
        /// <param name="argName">Name of argument.</param>
        /// <typeparam name="T">Type of argument to check.</typeparam>
        /// <returns>Passed argument, if not null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if provided argument is null.</exception>
        public static T ThrowIfNull<T>(this T argument, string argName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argName);
            }

            return argument;
        }

        /// <summary>
        /// Throw <see cref="ArgumentNullException"/> if provided value is null, empty or whitespace.
        /// </summary>
        /// <param name="argument">Argument to check.</param>
        /// <param name="argName">Name of argument.</param>
        /// <returns>Passed string, if not null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if provided argument is null.</exception>
        public static string ThrowIfNullOrWhiteSpace(this string argument, string argName)
        {
            if (string.IsNullOrWhiteSpace(argument)) throw new ArgumentNullException(argName);

            return argument;
        }
    }
}