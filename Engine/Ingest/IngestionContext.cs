using DLCS.Model.Assets;
using Engine.Ingest.Workers;

namespace Engine.Ingest
{
    /// <summary>
    /// Context for an in-flight ingestion request.
    /// </summary>
    public class IngestionContext
    {
        public Asset Asset { get; }
            
        public AssetFromOrigin AssetFromOrigin { get; }
            
        public IngestionContext(Asset asset, AssetFromOrigin assetFromOrigin)
        {
            Asset = asset;
            AssetFromOrigin = assetFromOrigin;
        }
    }
}