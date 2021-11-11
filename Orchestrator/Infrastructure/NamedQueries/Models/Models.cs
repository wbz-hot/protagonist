﻿using System.IO;

namespace Orchestrator.Infrastructure.NamedQueries.Models
{
    /// <summary>
    /// Represents the result of a request to generate a PDF from NQ
    /// </summary>
    public class PersistedProjectionFromNamedQuery
    {
        /// <summary>
        /// Stream containing generated projection.
        /// </summary>
        public Stream DataStream { get; } = Stream.Null;

        /// <summary>
        /// Overall status of request
        /// </summary>
        public PersistedProjectionStatus Status { get; } = PersistedProjectionStatus.Unknown;

        /// <summary>
        /// Whether this result object has data
        /// </summary>
        public bool IsEmpty => DataStream == Stream.Null;

        /// <summary>
        /// Whether this request could not be satisfied as a result of a bad request
        /// </summary>
        public bool IsBadRequest { get; private init; }

        public static PersistedProjectionFromNamedQuery BadRequest() => new() { IsBadRequest = true };

        public PersistedProjectionFromNamedQuery()
        {
        }

        public PersistedProjectionFromNamedQuery(PersistedProjectionStatus status)
        {
            DataStream = Stream.Null;
            Status = status;
        }

        public PersistedProjectionFromNamedQuery(Stream? pdfStream, PersistedProjectionStatus status)
        {
            DataStream = pdfStream ?? Stream.Null;
            Status = status;
        }
    }
    
    public enum PersistedProjectionStatus
    {
        /// <summary>
        /// Default status
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Projected asset is available to view, either having been freshly created or streamed
        /// </summary>
        Available,
        
        /// <summary>
        /// Projected asset is in the process of being created.
        /// </summary>
        InProcess,
        
        /// <summary>
        /// Projected asset cannot be found.
        /// </summary>
        NotFound,
        
        /// <summary>
        /// There was an error in handling the request.
        /// </summary>
        Error
    }
}