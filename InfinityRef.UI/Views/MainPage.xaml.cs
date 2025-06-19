using InfinityRef.UI.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;
#if WINDOWS
  using Windows.Storage;                           // StorageFile
  using System.Runtime.InteropServices;

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
            if (e.PlatformArgs?.DragEventArgs is not null) 
            {
                var WindowsDragEventArgs = e.PlatformArgs.DragEventArgs;
                var dragUI = WindowsDragEventArgs.DragUIOverride;

                var DraggedOverItems = await WindowsDragEventArgs.DataView.GetStorageItemsAsync();
                e.AcceptedOperation = DataPackageOperation.None;

                if (DraggedOverItems.Count > 0)
                {
                    foreach (var item in DraggedOverItems)
                    {
                        if (item is Windows.Storage.StorageFile file)
                        {
                            string fileExtension = file.FileType.ToLower();
                            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png") // Check any other type of file you want to accept
                            {
                                dragUI.Caption = "Drop the file!";
                                dragUI.IsCaptionVisible = false;
                                dragUI.IsGlyphVisible = false;

                                filePath = file.Path;
                                Debug.WriteLine($"We now have file {file.Path} dragged over!");
                            }
                            else
                            {
                                dragUI.Caption = "Invalid file type";
                                dragUI.IsCaptionVisible = true;
                                dragUI.IsGlyphVisible = false;
                            }
                        }
                    }
                }
            }
#endif
        }

        private async void OnDrop(object sender, DropEventArgs e)
        {
            //FileDropImage.Source = filePath;

            // Handle drag-and-drop from browser to app.
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
    }
}
