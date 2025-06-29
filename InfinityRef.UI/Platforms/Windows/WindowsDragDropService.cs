using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using Microsoft.UI.Xaml;
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
        /// Handles a drop event and attempts to retrieve image data from the dropped content.
        /// </summary>
        /// <remarks>This method supports multiple types of drop content, including: <list type="bullet">
        /// <item><description>URIs (e.g., from browsers or email clients).</description></item> <item><description>HTML
        /// content containing an <c>&lt;img&gt;</c> tag with a valid <c>src</c> attribute.</description></item>
        /// <item><description>Files dropped from file explorers, provided they are of a supported
        /// type.</description></item> </list> If the dropped content does not match any of these formats or an error
        /// occurs during processing, the method returns <c>(false, null)</c>.</remarks>
        /// <param name="dropEvent">The event arguments containing information about the drop operation.</param>
        /// <returns>A tuple containing a boolean indicating success and a byte array with the image data if successful;
        /// otherwise, <see langword="null"/>.</returns>
        public async Task<(bool Success, byte[]? ImageData, Position2D DropPoint)> HandleDropAsync(DropEventArgs dropEvent)
        {
            var nativeArgs = dropEvent.PlatformArgs?.DragEventArgs;
            var dataPackageView = nativeArgs?.DataView;
            if (dataPackageView == null)
            {
                return (false, null, Position2D.Empty);
            }

            var formats = dataPackageView.AvailableFormats;

            // Get drop point in screen coordinates.
            var uiElement = dropEvent.PlatformArgs?.Sender;
            var point = nativeArgs?.GetPosition(uiElement);
            var dropPoint = Position2D.Empty;

            if (point.HasValue)
            {
                dropPoint = new Position2D((float)point.Value.X, (float)point.Value.Y);
            }

            // Handle URI‐drop (browser/outlook).
            if (formats.Contains(StandardDataFormats.Uri) || formats.Contains("UniformResourceLocator"))
            {
                try
                {
                    var winUri = await dataPackageView.GetUriAsync();
                    var url = winUri?.AbsoluteUri;
                    if (url is string && (url.StartsWith("http://") || url.StartsWith("https://")))
                    {
                        // Ensure the URL is well-formed and absolute.
                        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                        {
                            return (false, null, dropPoint);
                        }
                        // Attempt to fetch the image data from the URL.
                        using var client = httpClientFactory.CreateClient("ImageClient");
                        var bytes = await client.GetByteArrayAsync(url);
                        return (true, bytes, dropPoint);
                    }

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        using var client = httpClientFactory.CreateClient("ImageClient");
                        using var stream = await client.GetStreamAsync(url);
                        return (true, await ReadAllBytesAsync(stream), dropPoint);
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
                        return (true, await ReadAllBytesAsync(stream), dropPoint);
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
                        return (true, await ReadAllBytesAsync(read.AsStreamForRead()), dropPoint);
                    }
                }
                catch { }
            }

            return (false, null, dropPoint);
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
        public async Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(Microsoft.Maui.Controls.DragEventArgs dragEvent)
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
        /// Asynchronously reads all bytes from the specified stream and returns them as a byte array.
        /// </summary>
        /// <remarks>The method reads the entire content of the stream asynchronously into memory.</remarks>
        /// <param name="stream">The input stream to read from. The stream must support reading.</param>
        /// <returns>A byte array containing all the data read from the stream.</returns>
        private async Task<byte[]> ReadAllBytesAsync(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
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
