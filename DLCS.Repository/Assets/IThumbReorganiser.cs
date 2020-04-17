using System.Collections.Generic;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Storage;

namespace DLCS.Repository.Assets
{
    public interface IThumbReorganiser
    {
        Task EnsureNewLayout(ObjectInBucket rootKey);

        Task CreateNewThumbs(Asset asset, IEnumerable<ThumbOnDisk> thumbsToProcess);
    }
}