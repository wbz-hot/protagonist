using System.Collections.Generic;
using System.Linq;

namespace DLCS.Core
{
    /// <summary>
    /// Methods to help with getting file extensions for known content-types.
    /// </summary>
    /// <remarks>This has been copied over from previous solution.</remarks>
    public class MIMEHelper
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension =
            new Dictionary<string, string>
            {
                {"application/pdf", "pdf"},
                {"audio/wav", "wav" },
                {"audio/mp3", "mp3" },
                {"audio/x-mpeg-3", "mp3" },
                {"video/mpeg", "mpg" },
                {"video/mp2", "mp2" },
                {"video/mp4", "mp4" },
                {"image/bmp", "bmp"},
                {"image/cgm", "cgm"},
                {"image/gif", "gif"},
                {"image/ief", "ief"},
                {"image/jp2", "jp2"},
                {"image/jpeg", "jpg"},
                {"image/jpg", "jpg"},  // common typo that is probably worth supporting even though it's invalid
                {"image/jpx", "jp2" },
                {"image/pict", "pic"},
                {"image/png", "png"},
                {"image/svg+xml", "svg"},
                {"image/tiff", "tiff"},
                {"image/tif", "tiff" }  // common typo that is probably worth supporting even though it's invalid
            };
        
        /// <summary>
        /// Get file extension for known MIME types.
        /// </summary>
        /// <param name="contentType">ContentType to get extension for.</param>
        /// <returns>Extension, if known.</returns>
        public static string? GetExtensionForContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return null;
            
            if (contentType.Contains(";"))
            {
                contentType = contentType.SplitSeparatedString(";").FirstOrDefault();
            }

            return ContentTypeToExtension.TryGetValue(contentType, out var extension)
                ? extension
                : null;
        }
    }
}