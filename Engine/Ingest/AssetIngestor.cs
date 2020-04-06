using System;
using System.Threading.Tasks;

namespace Engine.Ingest
{
    public abstract class AssetIngestor
    {
        // TODO - this should be of a known type, normalised from queue or http
        public async Task Ingest(object thingToIngest)
        {
            // load data

            await FamilySpecificIngest(thingToIngest, "");
            
            throw new NotImplementedException();
        }

        // TODO - what needs pushed to this method?
        protected abstract Task FamilySpecificIngest(object thingToIngest, object configVars);
    }
}