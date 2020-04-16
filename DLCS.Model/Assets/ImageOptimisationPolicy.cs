using System.Collections.Generic;
using System.Diagnostics;

namespace DLCS.Model.Assets
{
    [DebuggerDisplay("{Id}")]
    public class ImageOptimisationPolicy
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> TechnicalDetails { get; set; }
    }
}