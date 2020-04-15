using System.Collections.Generic;

namespace DLCS.Model.Assets
{
    public class ThumbnailPolicy
    { 
        public string Id { get; set; }
        public string Name { get; set; }
        public List<int> Sizes { get; set; }
    }
}
