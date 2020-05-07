﻿using System;
using System.Text.RegularExpressions;
using DLCS.Model.Assets;
using DLCS.Repository.Storage;

namespace Engine.Ingest.Timebased
{
    public class TranscoderTemplates
    {
        // technical details will contain a comma separated list of presets to use with the extension in brackets at the end
        // e.g. Wellcome Standard MP4(mp4),Wellcome Standard WebM(webm)
        private static readonly Regex PresetRegex = new Regex(@"^(.*?)\((.*?)\)$", RegexOptions.Compiled);

        /// <summary>
        /// Get the destination path where transcoded asset should be output to and the cleaned up presetName. 
        /// </summary>
        /// <param name="mediaType">The media-type/content-type for asset.</param>
        /// <param name="asset">The asset being ingested.</param>
        /// <param name="preset">The preset id from ImageOptimisationPolicy</param>
        /// <returns></returns>
        public static (string? template, string? presetName) ProcessPreset(string mediaType, Asset asset, string preset)
        {
            var match = PresetRegex.Match(preset);

            if (!match.Success) return (null, null);

            var presetName = match.Groups[1].Value;
            var presetExtension = match.Groups[2].Value;
            var template = GetDestinationTemplate(mediaType);
            
            var path = template
                .Replace("{asset}", asset.GetStorageKey())
                .Replace("{extension}", presetExtension);
            return (path, presetName);
        }

        private static string GetDestinationTemplate(string mediaType)
        {
            // audio: {customer}/{space}/{image}/full/max/default.{extension} (mediatype like audio/)
            // video: {customer}/{space}/{image}/full/full/max/max/0/default.{extension} (mediatype like video/)
            if (mediaType.StartsWith("audio/"))
            {
                return "{asset}/full/max/default.{extension}";
            }
            
            if (mediaType.StartsWith("video/"))
            {
                return "{asset}/full/full/max/max/0/default.{extension}";
            }

            throw new InvalidOperationException($"Unable to determine target location for mediaType '{mediaType}'");
        }
    }
}