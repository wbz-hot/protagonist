using System.Collections.Generic;

namespace DLCS.Model.Policies
{
    public class ThumbnailPolicy
    { 
        public string Id { get; set; }
        public string Name { get; set; }
        public List<int> Sizes { get; set; }
    }
}
