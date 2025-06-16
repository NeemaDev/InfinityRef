using CommunityToolkit.Mvvm.Input;
using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.UI.ViewModels
{
    public class MainViewModel
    {
        private readonly INavigationService navigationService;
        private readonly ISaveLoadService saveLoadService;
        private readonly Core.Interfaces.IFilePicker filePicker;

        public RelayCommand LoadCanvasCommand { get; }

        public MainViewModel(INavigationService navigationService, ISaveLoadService saveLoadService, Core.Interfaces.IFilePicker filePicker)
        {
            this.navigationService = navigationService;
            this.saveLoadService = saveLoadService;
            this.filePicker = filePicker;

            LoadCanvasCommand = new RelayCommand(async () => await LoadCanvasAsync());
        }
        public Task HandleDrop(SKBitmap skBitmap)
        {
            var activeCanvas = navigationService.GetActiveCanvas();
            var layer = new ImageLayer() { Bitmap = skBitmap };
            activeCanvas.AddLayer(layer);

            return Task.CompletedTask;
        }

        private async Task LoadCanvasAsync()
        {
            var filePath = await filePicker.PickFileAsync(new[] { ".irf" });
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var canvas = await saveLoadService.LoadCanvas(filePath);
                if (canvas != null)
                {
                    navigationService.OpenCanvas(canvas);
                }
            }
        }


    }
}
