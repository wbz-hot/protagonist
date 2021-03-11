﻿using System;
using System.Collections.Generic;
using IIIF;
using IIIF.ImageApi;

namespace DLCS.Repository.Assets
{
    public static class ThumbnailCalculator
    {
        public static SizeCandidate GetCandidates(List<Size> sizes, ImageRequest imageRequest, bool allowResize)
        {
            return allowResize
                ? GetLongestEdgeAndSize(sizes, imageRequest)
                : GetLongestEdge(sizes, imageRequest);
        }
        
        private static SizeCandidate GetLongestEdge(List<Size> sizes, ImageRequest imageRequest)
        {
            int? longestEdge = null;
            if (imageRequest.Size.Width > 0 && imageRequest.Size.Height > 0)
            {
                // We don't actually need to check imageRequest.Size.Confined (!w,h) because same logic applies...
                longestEdge = Math.Max(imageRequest.Size.Width ?? 0, imageRequest.Size.Height ?? 0);
            }
            else
            {
                // we need to know the sizes of things...
                if (imageRequest.Size.Width > 0)
                {
                    foreach (var size in sizes)
                    {
                        if (size.Width == imageRequest.Size.Width)
                        {
                            longestEdge = size.MaxDimension;
                            break;
                        }
                    }
                }
                if (imageRequest.Size.Height > 0)
                {
                    foreach (var size in sizes)
                    {
                        if (size.Height == imageRequest.Size.Height)
                        {
                            longestEdge = size.MaxDimension;
                            break;
                        }
                    }
                }

                if (imageRequest.Size.Max)
                {
                    longestEdge = sizes[0].MaxDimension;
                }
            }

            return new SizeCandidate(longestEdge);
        }
        
        private static ResizableSize GetLongestEdgeAndSize(List<Size> sizes, ImageRequest imageRequest)
        {
            // TODO - handle there being none "open"?
            
            var sizeCandidate = GetLongestEdge(sizes, imageRequest);
            if (sizeCandidate.LongestEdge.HasValue)
            {
                // We have found a matching size, use that.
                return new ResizableSize(sizeCandidate.LongestEdge.Value);
            }
            
            // calculate the size using requested dimensions
            Size idealSize;
            var sizeParameter = imageRequest.Size;
            if (sizeParameter.Confined)
            {
                var widthIsLarger = imageRequest.Size.Width > imageRequest.Size.Height;
                var width = widthIsLarger ? imageRequest.Size.Width.Value : (int?) null;
                var height = widthIsLarger ? (int?) null : imageRequest.Size.Height.Value;
                idealSize = Size.Resize(sizes[0], width, height);
            }
            else
            {
                idealSize = Size.Resize(sizes[0], sizeParameter.Width, sizeParameter.Height);
            }

            // iterate through all of the known sizes until we find a smaller one
            int count = 0;
            Size? larger = null;
            foreach (var s in sizes)
            {
                // Iterate until we find one that is smaller
                if (!idealSize.IsConfinedWithin(s))
                {
                    break;
                }

                larger = s;
                count++;
            }

            return new ResizableSize
            {
                Ideal = idealSize,
                LargerSize = larger,
                SmallerSize = count == sizes.Count ? null : sizes[count]
            };
        }
    }
    
    /// <summary>
    /// Size candidate for a single longest edge.
    /// </summary>
    public class SizeCandidate
    {
        public int? LongestEdge { get; }
        
        public bool KnownSize { get; }
        
        public SizeCandidate(int? longestEdge)
        {
            LongestEdge = longestEdge;
            KnownSize = longestEdge.HasValue;
        }

        public SizeCandidate()
        {
        }
    }

    /// <summary>
    /// Size candidates for size where no exact match is found.
    /// </summary>
    public class ResizableSize : SizeCandidate
    {
        public Size? LargerSize { get; set;}
        public Size? SmallerSize { get; set;}
        public Size Ideal { get; set; }

        public ResizableSize(int? longestEdge) : base(longestEdge)
        {
        }

        public ResizableSize()
        {
        }
    }
}