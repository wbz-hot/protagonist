namespace DLCS.Model.Assets
{
    public class ImageLocation : IEntity
    {
        public string Id { get; set; }
        public string S3 { get; set; }
        public string Nas { get; set; }
        
        public void PrepareForDatabase()
        {
            Nas ??= string.Empty;
            S3 ??= string.Empty;
        }
    }
}