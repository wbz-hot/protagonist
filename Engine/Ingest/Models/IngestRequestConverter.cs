using System;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using IIIF.ImageApi;
using Newtonsoft.Json;

namespace Engine.Ingest.Models
{
    public static class IngestRequestConverter
    {
        /// <summary>
        /// Convert to IngestAssetRequest object.
        /// </summary>
        /// <param name="incomingRequest">Event to convert</param>
        /// <returns>IngestAssetRequest</returns>
        /// <exception cref="InvalidOperationException">Thrown if IncomingIngestEvent doesn't contain any Asset data</exception>
        public static IngestAssetRequest ConvertToInternalRequest(this IncomingIngestEvent incomingRequest)
        {
            incomingRequest.ThrowIfNull(nameof(incomingRequest));

            if (string.IsNullOrEmpty(incomingRequest.AssetJson))
            {
                throw new InvalidOperationException("Cannot convert IncomingIngestEvent that has no Asset Json");
            }

            try
            {
                var formattedJson = incomingRequest.AssetJson.Replace("\r\n", string.Empty);
                var asset = JsonConvert.DeserializeObject<Asset>(formattedJson);
                return new IngestAssetRequest(asset, incomingRequest.Created);
            }
            catch (JsonReaderException e)
            {
                var ex = new InvalidOperationException("Unable to deserialize Asset Json from IncomingIngestEvent", e);
                ex.Data.Add("AssetJson", incomingRequest.AssetJson);
                throw ex;
            }
        }
    }
}