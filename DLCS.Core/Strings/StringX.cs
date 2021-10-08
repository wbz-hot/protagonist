﻿namespace DLCS.Core.Strings
{
    public static class StringX
    {
        /// <summary>
        /// Check if string has content (is not null, empty or whitespace)
        /// </summary>
        /// <param name="str">String to check</param>
        /// <returns>true if string contains content; else false</returns>
        public static bool HasText(this string str) => !string.IsNullOrWhiteSpace(str);
    }
}