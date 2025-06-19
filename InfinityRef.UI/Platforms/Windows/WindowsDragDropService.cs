using InfinityRef.UI.Interfaces;
using SkiaSharp;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace InfinityRef.UI.Platforms.Windows
{
    public class WindowsDragDropService : IDragDropService
    {
        private readonly List<string> supportedImageFormats = new List<string> { "png", "jpeg", "jpg", "gif", "bmp", "webp" };

        readonly IHttpClientFactory httpClientFactory;

        public WindowsDragDropService(IHttpClientFactory httpFactory)
          => httpClientFactory = httpFactory;

        /// <summary>
        /// Handles drag-and-drop events and attempts to extract and decode an image from the dropped data.
        /// Supports URI drops, HTML drops with image sources, and file drops from the file explorer.
        /// Returns a tuple indicating success and the decoded <see cref="SKBitmap"/> if available.
        /// </summary>
        /// <param name="dropEvent">The drag-and-drop event arguments.</param>
        /// <returns>A tuple containing a success flag and the decoded <see cref="SKBitmap"/> if successful; otherwise, null.</returns>
        public async Task<(bool Success, SKBitmap? Bitmap)> HandleDropAsync(DropEventArgs dropEvent)
        {
            var dataPackageView = dropEvent.PlatformArgs?.DragEventArgs?.DataView;
            if (dataPackageView == null)
            {
                return (false, null);
            }

            var formats = dataPackageView.AvailableFormats;

            // Handle URI‐drop (browser/outlook).
            if (formats.Contains(StandardDataFormats.Uri) || formats.Contains("UniformResourceLocator"))
            {
                try
                {
                    var winUri = await dataPackageView.GetUriAsync();
                    var url = winUri?.AbsoluteUri;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        using var client = httpClientFactory.CreateClient("ImageClient");
                        using var stream = await client.GetStreamAsync(url);
                        return (true, DecodeStream(stream));
                    }
                }
                catch { }
            }

            // Handle HTML‐drop (<img src="…">).
            if (formats.Contains(StandardDataFormats.Html))
            {
                try
                {
                    var html = await dataPackageView.GetHtmlFormatAsync();
                    var img = Regex.Match(html,
                                "<img[^>]+src=[\"'](?<u>[^\"']+)[\"']",
                                RegexOptions.IgnoreCase);
                    if (img.Success && Uri.IsWellFormedUriString(img.Groups["u"].Value, UriKind.Absolute))
                    {
                        using var client = httpClientFactory.CreateClient("ImageClient");
                        using var stream = await client.GetStreamAsync(img.Groups["u"].Value);
                        return (true, DecodeStream(stream));
                    }
                }
                catch { }
            }

            // Handle drops from file explorer.
            bool looksLikeFiles = formats.Any(f => f is "FileDrop" or "FileName" or "FileNameW" or "FileContents");

            if (looksLikeFiles)
            {
                try
                {
                    var items = await dataPackageView.GetStorageItemsAsync();
                    var storageFile = items.OfType<StorageFile>().FirstOrDefault(f => IsSupportedType(f.Path));

                    if (storageFile != null)
                    {
                        using var read = await storageFile.OpenReadAsync();
                        return (true, DecodeStream(read.AsStreamForRead()));
                    }
                }
                catch { }
            }

            return (false, null);
        }

        /// <summary>
        /// Analyzes the drag-and-drop operation to determine its validity and extract the source data.
        /// </summary>
        /// <remarks>This method checks the drag data for supported formats, such as URIs or file drops,
        /// and extracts the relevant source information. Supported file types are determined by the
        /// <c>IsSupportedType</c> method.</remarks>
        /// <param name="dragEvent">The <see cref="DragEventArgs"/> containing information about the drag event.</param>
        /// <returns>A tuple containing: <list type="bullet"> <item><term><see langword="true"/></term><description>if the drag
        /// operation contains valid data; otherwise, <see langword="false"/>.</description></item> <item><term>A <see
        /// cref="string"/></term><description>representing the source of the drag data, such as a URL or file path.
        /// Returns an empty string if the data is invalid.</description></item> </list> </returns>
        public async Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs dragEvent)
        {
            var dataPackageView = dragEvent.PlatformArgs?.DragEventArgs?.DataView;

            if (dataPackageView == null)
            {
                return (false, string.Empty);
            }

            var formats = dataPackageView.AvailableFormats;

            // Check for URI (browser).
            if (formats.Contains(StandardDataFormats.Uri))
            {
                var winUri = await dataPackageView.GetUriAsync();
                var url = winUri?.AbsoluteUri;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    return (true, url);
                }
            }

            // Check for file drop
            if (formats.Any(f => f is "FileDrop" or "FileName" or "FileNameW" or "FileContents"))
            {
                var items = await dataPackageView.GetStorageItemsAsync();
                var storageFile = items.OfType<StorageFile>().FirstOrDefault(f => IsSupportedType(f.Path));

                if (storageFile != null)
                {
                    return (true, storageFile.Path);
                }
            }

            return (false, string.Empty);
        }


        /// <summary>
        /// Decodes an image from the provided stream into an <see cref="SKBitmap"/> object.
        /// </summary>
        /// <remarks>The method reads the entire stream into memory before decoding the image. Ensure the
        /// stream contains valid image data supported by <see cref="SKBitmap.Decode(byte[])"/>.</remarks>
        /// <param name="stream">The input stream containing image data. The stream must be readable and contain valid image data.</param>
        /// <returns>An <see cref="SKBitmap"/> object representing the decoded image.</returns>
        private static SKBitmap DecodeStream(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return SKBitmap.Decode(memoryStream.ToArray());
        }

        /// <summary>
        /// Checks if the file type is supported for drag-and-drop operations.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file is supported, false otherwise.</returns>
        public bool IsSupportedType(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                return supportedImageFormats.Contains(ext);
            }
            return false;
        }
    }
}
