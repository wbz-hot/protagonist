using System.IO;
using System.Text;

namespace DLCS.Test.Helpers
{
    public static class MemoryStreamHelpers
    {
        /// <summary>
        /// Return a MemoryStream containing specified string.
        /// </summary>
        public static MemoryStream ToMemoryStream(this string content)
            => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}