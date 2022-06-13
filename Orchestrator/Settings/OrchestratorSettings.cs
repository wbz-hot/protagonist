﻿using System;
using System.Collections.Generic;
using System.Linq;
using DLCS.Core.Types;
using DLCS.Model.Templates;
using DLCS.Repository.Caching;
using Version = IIIF.ImageApi.Version;

namespace Orchestrator.Settings
{
    public class OrchestratorSettings
    {
        /// <summary>
        /// PathBase to host app on.
        /// </summary>
        public string PathBase { get; set; }

        /// <summary>
        /// Regex for S3-origin, for objects uploaded directly to DLCS.
        /// </summary>
        public string S3OriginRegex { get; set; }

        /// <summary>
        /// URI template for auth services
        /// </summary>
        public string AuthServicesUriTemplate { get; set; }

        /// <summary>
        /// Timeout for critical orchestration path. How long to wait to achieve lock when orchestrating asset.
        /// If timeout breached, multiple orchestrations can happen for same item.
        /// </summary>
        public int CriticalPathTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Which image-server is handling downstream tile requests
        /// </summary>
        /// <remarks>
        /// Ideally the Orchestrator should be agnostic to this but, for now at least, the downstream image server will
        /// be useful to know for toggling some functionality (for now at least)
        /// </remarks>
        public ImageServer ImageServer { get; set; } = ImageServer.Cantaloupe;

        /// <summary>
        /// Configuration for specifying paths to images stored on fast disk.
        /// </summary>
        public Dictionary<ImageServer, ImageServerConfig> ImageServerPathConfig { get; set; } = new();

        /// <summary>
        /// Folder template for downloading resources to.
        /// </summary>
        public string ImageFolderTemplateOrchestrator { get; set; }

        /// <summary>
        /// If true, requests for info.json will cause image to be orchestrated.
        /// </summary>
        public bool OrchestrateOnInfoJson { get; set; }

        /// <summary>
        /// If <see cref="OrchestrateOnInfoJson"/> is true, this is the max number of requests that will be honoured
        /// </summary>
        public int OrchestrateOnInfoJsonMaxCapacity { get; set; } = 50;

        /// <summary>
        /// String used for salting requests to API
        /// </summary>
        public string ApiSalt { get; set; }

        /// <summary>
        /// Default Presentation API Version to conform to when returning presentation resources  
        /// </summary>
        public string DefaultIIIFPresentationVersion { get; set; } = "3.0";

        /// <summary>
        /// Get default Presentation API Version to conform to when returning resources from as enum.
        /// Defaults to V3 if unsupported, or unknown version specified
        /// </summary>
        public IIIF.Presentation.Version GetDefaultIIIFPresentationVersion()
            => DefaultIIIFPresentationVersion[0] == '2' ? IIIF.Presentation.Version.V2 : IIIF.Presentation.Version.V3;

        /// <summary>
        /// Default Image API Version to conform to when returning image description resources
        /// </summary>
        public string DefaultIIIFImageVersion { get; set; } = "3.0";

        /// <summary>
        /// Get default IIIF Image API Version to conform to when returning resources as enum.
        /// Defaults to V3 if unsupported, or unknown version specified
        /// </summary>
        public IIIF.ImageApi.Version GetDefaultIIIFImageVersion()
            => DefaultIIIFImageVersion[0] == '2' ? IIIF.ImageApi.Version.V2 : IIIF.ImageApi.Version.V3;

        /// <summary>
        /// Root URL for dlcs api
        /// </summary>
        public Uri ApiRoot { get; set; }

        /// <summary>
        /// The thumbnail that is the default target size for rendering manifests such as NQs. We won't necessarily
        /// render a thumbnail of this size but will aim to get as close as possible.
        /// </summary>
        public int TargetThumbnailSize { get; set; } = 200;

        public ProxySettings Proxy { get; set; }

        public CacheSettings Caching { get; set; }

        public AuthSettings Auth { get; set; }

        public NamedQuerySettings NamedQuery { get; set; }

        /// <summary>
        /// Get the local folder path for Asset. This is where it will be orchestrated to, or found on fast disk after
        /// orchestration.
        /// </summary>
        public string GetImageLocalPath(AssetId assetId)
            => TemplatedFolders.GenerateFolderTemplate(ImageFolderTemplateOrchestrator, assetId);

        /// <summary>
        /// Get the full redirect path for ImageServer. Includes path prefix and parsed location where image-server can
        /// access Asset file.
        /// This will return the endpoint for highest supported ImageApiVersion 
        /// </summary>
        public string GetImageServerPath(AssetId assetId)
        {
            var imageServerConfig = ImageServerPathConfig[ImageServer];
            return GetImageServerFilePathInternal(assetId, imageServerConfig,
                imageServerConfig.DefaultVersionPathTemplate);
        }

        /// <summary>
        /// Get the full redirect path for ImageServer for specified ImageApi version. Includes path prefix and parsed
        /// location where image-server can access Asset file.
        /// </summary>
        /// <returns>Path for image-server if image-server can handle requested version, else null</returns>
        public string? GetImageServerPath(AssetId assetId, Version targetVersion)
        {
            var imageServerConfig = ImageServerPathConfig[ImageServer];

            return imageServerConfig.VersionPathTemplates.TryGetValue(targetVersion, out var pathTemplate)
                ? GetImageServerFilePathInternal(assetId, imageServerConfig, pathTemplate)
                : null;
        }

        private static string GetImageServerFilePathInternal(AssetId assetId, ImageServerConfig imageServerConfig,
            string versionTemplate)
        {
            var imageServerFilePath = TemplatedFolders.GenerateTemplate(imageServerConfig.PathTemplate, assetId,
                imageServerConfig.Separator);

            return $"{versionTemplate}{imageServerFilePath}";
        }
    }

    public class ProxySettings
    {
        /// <summary>
        /// Get the root path that thumb handler is listening on
        /// </summary>
        public string ThumbsPath { get; set; } = "thumbs";

        /// <summary>
        /// Whether resizing thumbs is supported
        /// </summary>
        public bool CanResizeThumbs { get; set; }

        /// <summary>
        /// Get the root path that thumb handler is listening on
        /// </summary>
        public string ThumbResizePath { get; set; } = "thumbs";

        /// <summary>
        /// Get the root path for serving images
        /// </summary>
        public string ImagePath { get; set; } = "iiif-img";

        /// <summary>
        /// A collection of resize config for serving resized thumbs rather than handling requests via image-server
        /// </summary>
        public Dictionary<string, ThumbUpscaleConfig> ThumbUpscaleConfig { get; set; } = new();
    }

    /// <summary>
    /// Represents resize logic for a set of assets
    /// </summary>
    public class ThumbUpscaleConfig
    {
        /// <summary>
        /// Regex to validate image Id against, the entire asset Id will be used (e.g. 2/2/my-image-name)
        /// </summary>
        public string AssetIdRegex { get; set; }

        /// <summary>
        /// The maximum % size difference for upscaling.
        /// </summary>
        public int UpscaleThreshold { get; set; }
    }

    public class AuthSettings
    {
        /// <summary>
        /// Format of authToken, used to generate token id.
        /// {0} is replaced with customer id
        /// </summary>
        public string CookieNameFormat { get; set; } = "dlcs-token-{0}";

        /// <summary>
        /// A list of domains to set on auth cookie.
        /// </summary>
        public List<string> CookieDomains { get; set; } = new();

        /// <summary>
        /// If true the current domain is automatically added to auth token domains.
        /// </summary>
        public bool UseCurrentDomainForCookie { get; set; } = true;
    }

    /// <summary>
    /// Settings related to NamedQuery generation and serving
    /// </summary>
    public class NamedQuerySettings
    {
        /// <summary>
        /// String format for generating keys for PDF object storage.
        /// Supported replacements are {customer}/{queryname}/{args}
        /// </summary>
        public string PdfStorageTemplate { get; set; } = "{customer}/pdf/{queryname}/{args}";

        /// <summary>
        /// String format for generating keys for Zip object storage.
        /// Supported replacements are {customer}/{queryname}/{args}
        /// </summary>
        public string ZipStorageTemplate { get; set; } = "{customer}/zip/{queryname}/{args}";

        /// <summary>
        /// Number of seconds after which an "InProcess" control file is considered stale for.
        /// After this time has elapsed it will be recreated.
        /// </summary>
        public int ControlStaleSecs { get; set; } = 600;

        /// <summary>
        /// URL root of fireball service for PDF generation
        /// </summary>
        public Uri FireballRoot { get; set; }

        /// <summary>
        /// Folder template for creating local Zip file
        /// </summary>
        public string ZipFolderTemplate { get; set; }
    }

    /// <summary>
    /// Enum representing image server used for serving image requests
    /// </summary>
    public enum ImageServer
    {
        /// <summary>
        /// Cantaloupe image server
        /// </summary>
        Cantaloupe,

        /// <summary>
        /// IIP Image Server
        /// </summary>
        IIPImage
    }

    /// <summary>
    /// Represents redirect configuration for redirecting ImageServer requests
    /// </summary>
    public class ImageServerConfig
    {
        /// <summary>
        /// Directory separator character to use when specifying path to image.
        /// Used when constructing {image-dir} template replacement.
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// Path template for sending requests to image server.
        /// Supports {customer}, {space}, {image-dir} and {image} replacements.
        /// </summary>
        /// <remarks>
        /// This is the template used to construct requests to image servers. 
        /// </remarks>
        public string PathTemplate { get; set; }

        /// <summary>
        /// The prefix for forwarding requests to this image-server by supported Image Api version. The final URL sent
        /// to the image-server is {image-server-root}{url-prefix}{path-template}{image-request}
        /// </summary>
        public Dictionary<Version, string> VersionPathTemplates { get; set; }

        private string? defaultVersionPathTemplate;

        /// <summary>
        /// The default version path template to use for non-versioned requests.
        /// This is the VersionPathTemplates list with highest version.
        /// </summary>
        public string DefaultVersionPathTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(defaultVersionPathTemplate))
                {
                    // On first request find the value with highest Version
                    defaultVersionPathTemplate = VersionPathTemplates.MaxBy(t => t.Key switch
                    {
                        Version.Unknown => 1,
                        Version.V2 => 2,
                        Version.V3 => 3,
                        _ => throw new ArgumentOutOfRangeException(nameof(VersionPathTemplates),
                            "Unknown IIIFImageVersion")
                    }).Value;
                }

                return defaultVersionPathTemplate;
            }
        }
    }
}