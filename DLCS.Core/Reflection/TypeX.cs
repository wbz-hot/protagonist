using System;
using System.Collections;

namespace DLCS.Core.Reflection
{
    /// <summary>
    /// A collection of extension methods on Type for helping with reflection. 
    /// </summary>
    public static class TypeX
    {
        /// <summary>
        /// Check if type implements IEnumerable
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <returns>true of type is IEnumerable, else false.</returns>
        public static bool IsEnumerable(this Type type)
            => type != null && type.GetInterface(nameof(IEnumerable)) != null;
    }
}