using System.ComponentModel;

namespace DLCS.Model.Customer
{
    /// <summary>
    /// Represents a specific CustomerOriginStrategy
    /// </summary>
    public enum OriginStrategy
    {
        /// <summary>
        /// Default origin strategy. Use unauthorised http request to fetch original source.
        /// </summary>
        [Description("default")]
        Default = 0,
        
        [Description("basic-http-authentication")]
        BasicHttp = 1,
        
        [Description("s3-ambient")]
        S3Ambient = 2,
        
        [Description("sftp")]
        SFTP = 3
    }
}