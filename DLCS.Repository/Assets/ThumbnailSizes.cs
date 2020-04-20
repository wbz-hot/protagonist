using System;
using System.Collections.Generic;
using DLCS.Core.Guard;
using IIIF;
using Newtonsoft.Json;

namespace DLCS.Repository.Assets
{
    /// <summary>
    /// Model representing auth/open thumbnail sizes
    /// </summary>
    /// <remarks>This is saved as s.json in s3.</remarks>
    public class ThumbnailSizes
    {
        [JsonProperty("o")]
        public List<int[]> Open { get; }
            
        [JsonProperty("a")]
        public List<int[]> Auth { get; }

        [JsonIgnore]
        public int Count { get; private set; }

        [JsonIgnore]
        public Size MaxAvailable { get; private set; }

        [JsonConstructor]
        public ThumbnailSizes(List<int[]> open, List<int[]> auth)
        {
            Open = open;
            Auth = auth;
            Count = (open?.Count ?? 0) + (auth?.Count ?? 0);
        }

        /// <summary>
        /// Create new ThumbnailSizes list with specified count as internal list capacity.
        /// </summary>
        /// <param name="sizesCount"></param>
        public ThumbnailSizes(int sizesCount = 4)
        {
            Open = new List<int[]>(sizesCount);
            Auth = new List<int[]>(sizesCount);
        }

        /// <summary>
        /// Set the maximum available thumb size. Used to put thumbs in correct bucket when calling Add();
        /// </summary>
        /// <param name="maxAvailableThumb"></param>
        public void SetMaxAvailableSize(Size maxAvailableThumb)
            => MaxAvailable = maxAvailableThumb.ThrowIfNull(nameof(maxAvailableThumb));

        /// <summary>
        /// Add a new Size to the thumbs list, working out if Auth or Open.
        /// SetMaxAvailableSize() must have been called
        /// </summary>
        /// <param name="thumb">Thumbnail size to add</param>
        /// <exception cref="InvalidOperationException">Thrown if MaxAvailableSize has not been set.</exception>
        public void Add(Size thumb)
        {
            if (MaxAvailable == null)
            {
                throw new InvalidOperationException("Attempt to Add thumb but MaxAvailable has not been set.");
            }
            
            if (thumb.IsConfinedWithin(MaxAvailable))
            {
                AddOpen(thumb);
            }
            else
            {
                AddAuth(thumb);
            }
        }

        public void AddAuth(Size size)
        {
            Count++;
            Auth.Add(size.ToArray());
        }

        public void AddOpen(Size size)
        {
            Count++;
            Open.Add(size.ToArray());
        }
    }
}