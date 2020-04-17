using System.IO;
using System.Text.RegularExpressions;
using DLCS.Model.Assets;

namespace Engine.Ingest
{
    // This logic has been copied over from Deliverator implementation.
    public class TemplatedFolders
    {
        private static readonly Regex ImageNameRegex = new Regex(@"(..)(..)(..)(..)(.*)", RegexOptions.Compiled);
        public const string Root = "{root}";
        public const string Customer = "{customer}";
        public const string Space = "{space}";
        public const string Image = "{image}";

        /// <summary>
        /// Generate a folder template using provided details.
        /// </summary>
        /// <param name="template">The basic template, e.g. {root}\{customer}\{space}\{image}</param>
        /// <param name="root">The root of the template, used as {root} param.</param>
        /// <param name="asset">Used to populate {customer}, {space} and, optionally, {image} properties.</param>
        /// <param name="replaceImage">If true {image} is replaced, else it is left unreplaced</param>
        /// <returns>New string with replacements made.</returns>
        public static string GenerateTemplate(string template, string root, Asset asset, bool replaceImage = true)
        {
            var replacements = template
                .Replace(Root, root)
                .Replace(Customer, asset.Customer.ToString())
                .Replace(Space, asset.Space.ToString());

            return replaceImage
                ? replacements.Replace(Image, SplitImageName(asset.GetUniqueName(), Path.DirectorySeparatorChar))
                : replacements;
        }

        /// <summary>
        /// Generate a folder template using provided details, ensuring path separator is for Unix.
        /// </summary>
        /// <param name="template">The basic template, e.g. {root}\{customer}\{space}\{image}</param>
        /// <param name="root">The root of the template, used as {root} param.</param>
        /// <param name="asset">Used to populate {customer}, {space} and {image} properties.</param>
        /// <returns>New string with replacements made.</returns>
        public static string GenerateTemplateForUnix(string template, string root, Asset asset)
        {
            var result = GenerateTemplate(template, root, asset);
            return result.Replace("\\", "/");
        }

        private static string SplitImageName(string name, char separator)
        {
            if (name.Length <= 8) return name;
            
            var match = ImageNameRegex.Match(name);

            return string.Concat(
                match.Groups[1].Value, separator,
                match.Groups[2].Value, separator,
                match.Groups[3].Value, separator,
                match.Groups[4].Value, separator,
                match.Groups[1].Value,
                match.Groups[2].Value,
                match.Groups[3].Value,
                match.Groups[4].Value,
                match.Groups[5].Value);

        }
    }
}