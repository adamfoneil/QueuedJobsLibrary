namespace Notification.Shared.Requests
{
    /// <summary>
    /// a request to zip all blobs at a given path
    /// </summary>
    public class ZipRequest
    {
        public string FolderName { get; set; }
    }
}
