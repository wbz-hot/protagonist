﻿using System;
using System.Collections.Generic;
using DLCS.Core.Collections;
using DLCS.Core.Types;

namespace Orchestrator.Assets
{
    /// <summary>
    /// Represents an asset during orchestration.
    /// </summary>
    public class OrchestrationAsset
    {
        /// <summary>
        /// Get or set the AssetId for tracked Asset
        /// </summary>
        public AssetId AssetId { get; set; }

        /// <summary>
        /// Get boolean indicating whether asset is restricted or not.
        /// </summary>
        public bool RequiresAuth => !Roles.IsNullOrEmpty();

        /// <summary>
        /// Gets list of roles associated with Asset
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Version identifier, used to validate saves are against correct version
        /// </summary>
        public int Version { get; set; }
    }

    public class OrchestrationFile : OrchestrationAsset
    {
        /// <summary>
        /// Get or set Asset origin 
        /// </summary>
        public string Origin { get; set; }
    }

    public class OrchestrationImage : OrchestrationAsset
    {
        /// <summary>
        /// Get or set asset Width
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Get or set asset Height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets list of thumbnail sizes
        /// </summary>
        public List<int[]> OpenThumbs { get; set; } = new();
        
        /// <summary>
        /// Get or set Asset location in S3 
        /// </summary>
        public string? S3Location { get; set; }
        
        /// <summary>
        /// Get or set the OrchestrationStatus of object
        /// </summary>
        public OrchestrationStatus Status { get; set; } = OrchestrationStatus.Unknown;
    }
}