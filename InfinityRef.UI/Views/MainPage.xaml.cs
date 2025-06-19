using InfinityRef.UI.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;
#if WINDOWS
  using Windows.Storage;                           // StorageFile
  using System.Runtime.InteropServices;
  using System.Runtime.InteropServices.WindowsRuntime;
  using Microsoft.Maui.ApplicationModel.DataTransfer;
  using System.Text.RegularExpressions;

#elif MACCATALYST
  // macOS UIHostingController-based drag‐drop gives UniformTypeIdentifiers
  using UniformTypeIdentifiers;
#endif

namespace InfinityRef
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel mainViewModel;
        private readonly IHttpClientFactory httpClientFactory;
        string filePath = string.Empty;

        public MainPage(MainViewModel viewModel, IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();
            BindingContext = viewModel;
            mainViewModel = viewModel;
            this.httpClientFactory = httpClientFactory;
        }

        private async void DragOver(object sender, DragEventArgs e)
        {
#if WINDOWS
            string hoveredFilePath = string.Empty;
            var dragEventArgs = e.PlatformArgs?.DragEventArgs;
            if (dragEventArgs is not null) 
            {
                var dataView = dragEventArgs.DataView;
                var ui = dragEventArgs.DragUIOverride;
                var isValidFilePath = await IsValidFileDrop(e);

                ui.Caption          = isValidFilePath.IsValid ? "Drop the file!" : "Invalid file type";
                ui.IsCaptionVisible = !isValidFilePath.IsValid;
                ui.IsGlyphVisible   = isValidFilePath.IsValid;

                if (isValidFilePath.IsValid)
                {
                    Debug.WriteLine("File dragged over: " + isValidFilePath.FilePath);
                }
            }
#endif
        }

        private async void OnDrop(object sender, DropEventArgs e)
        {
            // Handle drop for URIs from browsers.
            if (await TryHandleUriDrop(e))
            {
                return;
            }
            // Handle drop for embedded bitmaps (e.g., from clipboard or other apps).
            if (await TryHandleEmbeddedBitmap(e))
            {
                return;
            }
            // Handle drop from file system.
            if (await TryHandleFileDrop(e))
            {
                return;
            }


#if WINDOWS
            var winDp = e.PlatformArgs?.DragEventArgs?.DataView;
            if (winDp is not null)
            {
                var formats = winDp.AvailableFormats;
                if(formats.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Uri))
                {
                    try
                    {
                        var winUri = await winDp.GetUriAsync();  
                        if(winUri is not null)
                        {
                            var netUri = new System.Uri(winUri.ToString(), UriKind.Absolute);

                            var client = httpClientFactory.CreateClient("ImageClient");
                            using var httpStream = await client.GetStreamAsync(netUri);
                            using var memoryStream = new MemoryStream();
                            await httpStream.CopyToAsync(memoryStream);

                            var bitmapData = SKBitmap.Decode(memoryStream.ToArray());
                            await mainViewModel.HandleDrop(bitmapData);
                            CanvasView.InvalidateSurface();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving URI: {ex.Message}");
                    }
                }
            }
#endif
            // 1. Handle source from other winui/uwp apps.
            var imageSource = await e.Data.GetImageAsync();
            if (imageSource != null)
            {
                Stream? stream = null;

                if (imageSource is StreamImageSource streamImageSource)
                {
                    stream = await streamImageSource.Stream(CancellationToken.None);
                }
                else if (imageSource is FileImageSource fileImageSource)
                {
                    stream = File.OpenRead(fileImageSource.File);
                }
                else if (imageSource is UriImageSource uriImageSource)
                {
                    var client = httpClientFactory.CreateClient("ImageClient");
                    stream = await client.GetStreamAsync(uriImageSource.Uri);
                }

                if (stream != null)
                {
                    using (stream)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            var skbmp = SKBitmap.Decode(memoryStream.ToArray());
                            await mainViewModel.HandleDrop(skbmp);
                        }
                    }
                }
                CanvasView.InvalidateSurface();
                return;

            }

            // 2. Platform-specific handling for file drops from filesystem.
            // For MACCATALYST: Need to implement additional helper for drag-and-drop.
            FileResult? file = null;

#if WINDOWS
            // WinUI gives a DataView with StorageItems
            //var storageItems = await e.PlatformArgs!.DragEventArgs!.DataView.GetStorageItemsAsync();
            //var sf = storageItems.OfType<StorageFile>()
                                 //.FirstOrDefault(f => IsImagePath(f.Path));
            //if (sf != null)
                //file = new FileResult(sf.Path);

            var dp = e.PlatformArgs?.DragEventArgs?.DataView;
              if (dp == null)  
                return;

              // 1) Look at what formats the drag actually contains
              var availableFormats = dp.AvailableFormats;
              Debug.WriteLine("Drag formats: " + string.Join(", ", availableFormats));

                bool looksLikeFileDrop =
                   availableFormats.Contains("FileDrop")
                || availableFormats.Contains("FileName")
                || availableFormats.Contains("FileNameW")
                || availableFormats.Contains("FileContents")
                || availableFormats.Contains("FileGroupDescriptorW");

              // 2) Only attempt storage‐item queries if StorageItems is advertised
              if (looksLikeFileDrop)
              {
                try
                {
                  // 3) Safe to call
                  var items = await dp.GetStorageItemsAsync();
                  var sf    = items
                               .OfType<StorageFile>()
                               .FirstOrDefault(f => IsImagePath(f.Path));

                  if (sf != null)
                    file = new FileResult(sf.Path);
                }
                catch (COMException ex)
                {
                  Debug.WriteLine($"GetStorageItemsAsync failed: {ex.Message}");
                  // fall back to other formats or ignore
                }
              }
#endif

            if (file == null)
            {
                return;
            }
            else
            {
                // 3) Decode via stream
                using var fs = File.OpenRead(file.FullPath);
                using var ms = new MemoryStream();
                await fs.CopyToAsync(ms);
                ms.Position = 0;
                var bmp = SKBitmap.Decode(ms.ToArray());

                // 4) Hand it to your VM
                await mainViewModel.HandleDrop(bmp);

                // 5) Refresh the canvas
                CanvasView.InvalidateSurface();
            }

        }

        private async Task<bool> TryHandleFileDrop(DropEventArgs e)
        {
#if WINDOWS   
            var dp    = e.PlatformArgs?.DragEventArgs?.DataView;
            var fmts  = dp?.AvailableFormats;
            if (dp == null || fmts == null)
            return false;

            bool looksLikeFiles = fmts.Any(f =>
                f == "FileDrop"
            || f == "FileName" 
            || f == "FileNameW"
            || f == "FileContents"
            || f == "FileGroupDescriptorW");
            if (!looksLikeFiles)
            return false;

            try
            {
            var items = await dp.GetStorageItemsAsync();
            var sf    = items.OfType<StorageFile>()
                                .FirstOrDefault(f => IsImagePath(f.Path));
            if (sf == null)
                return false;

            using var ras = await sf.OpenReadAsync();
            using var ms  = new MemoryStream();
            await ras.AsStreamForRead().CopyToAsync(ms);
            await PaintFromBytesAsync(ms.ToArray());
            return true;
            }
            catch (COMException ex)
            {
            Debug.WriteLine("File drop failed: " + ex.Message);
            return false;
            }
#else
            return false;
#endif
        }

        private async Task<bool> TryHandleEmbeddedBitmap(DropEventArgs e)
        {
            try
            {
                var src = await e.Data.GetImageAsync();
                if (src == null) return false;

                using var bmp = await DecodeImageSourceAsync(src);
                await mainViewModel.HandleDrop(bmp);
                CanvasView.InvalidateSurface();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryHandleUriDrop(DropEventArgs e)
        {
#if WINDOWS
            var dp = e.PlatformArgs?.DragEventArgs?.DataView;            
            var fmts = dp?.AvailableFormats;

            if (dp == null || fmts == null ||
                (!fmts.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Uri) &&
                !fmts.Contains("UniformResourceLocator") &&
                !fmts.Contains("text/x-moz-url")))
            return false;

            try
            {
            var winUri = await dp.GetUriAsync();            // WinRT Uri
            if (winUri == null) return false;

            var sysUri = new Uri(winUri.AbsoluteUri, UriKind.Absolute);

            var client = httpClientFactory.CreateClient("ImageClient");
            using var stream = await client.GetStreamAsync(sysUri);
            await PaintFromStreamAsync(stream);
            return true;
            }
            catch (Exception ex)
            {
            Debug.WriteLine("URI drop failed: " + ex.Message);
            return false;
            }
#else
            return false;
#endif
        }

        bool IsImagePath(string path) =>
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
         || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
         || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

        void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {

        }

        void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {

        }

        async Task<(bool IsValid, string FilePath)> IsValidFileDrop(DragEventArgs e)
        {
#if WINDOWS
            var fmts = e.PlatformArgs?.DragEventArgs?.DataView.AvailableFormats;

            // Handle URI drops (browser)
            if (fmts.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Uri) 
               || fmts.Contains("UniformResourceLocator")
               || fmts.Contains("text/x-moz-url"))
            {
                try
                {
                  var winUri = await e.PlatformArgs?.DragEventArgs?.DataView.GetUriAsync();             // WinRT Uri
                  var url    = winUri?.AbsoluteUri;                 // string
                  if (!string.IsNullOrWhiteSpace(url))
                    return (true, url);
                }
                catch { /* ignore */ }
            }

            // Handle HTML fragment (<img src="…">)
            if (fmts.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Html) 
            || fmts.Contains("HTML Format"))
            {
            try
            {
                var html = await e.PlatformArgs?.DragEventArgs?.DataView.GetHtmlFormatAsync();
                // Regex to pull first src="…".
                var m = Regex.Match(html,"<img[^>]+src=[\"'](?<src>[^\"']+)[\"']", RegexOptions.IgnoreCase);
                var src = m.Success ? m.Groups["src"].Value : null;
                if (Uri.TryCreate(src, UriKind.Absolute, out _)){
                    return (true, src!);
                }
            }
            catch { /* ignore */ }
            }


            // Handle file drop from file system.
            bool looksLikeFiles = fmts.Any(f =>
                f == "FileDrop"
            || f == "FileName"
            || f == "FileNameW"
            || f == "FileContents"
            || f == "FileGroupDescriptorW");
            if (!looksLikeFiles)
            return (false, string.Empty);

            try
            {
            var items = await e.PlatformArgs?.DragEventArgs?.DataView.GetStorageItemsAsync();
            var sf    = items.OfType<StorageFile>()
                                .FirstOrDefault(f => IsImagePath(f.Path));
            if (sf != null)
            {
                return (true,  sf.Path);
            }
            }
            catch (COMException)
            {
            // not a real storage drop
            }
#endif
            return (false, string.Empty);
        }

        async Task PaintFromStreamAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            await PaintFromBytesAsync(ms.ToArray());
        }

        async Task PaintFromBytesAsync(byte[] bytes)
        {
            using var bmp = SKBitmap.Decode(bytes);
            await mainViewModel.HandleDrop(bmp);
            CanvasView.InvalidateSurface();
        }

        async Task<SKBitmap> DecodeImageSourceAsync(ImageSource src)
        {
            Stream? st = src switch
            {
                StreamImageSource sis => await sis.Stream(CancellationToken.None),
                FileImageSource fis => File.OpenRead(fis.File),
                UriImageSource uis => await httpClientFactory.CreateClient().GetStreamAsync(uis.Uri),
                _ => null
            };
            if (st == null)
                throw new InvalidOperationException("Could not extract image stream.");

            using (st)
            {
                var ms = new MemoryStream();
                await st.CopyToAsync(ms);
                return SKBitmap.Decode(ms.ToArray());
            }
        }
    }
}
