using CommunityToolkit.Mvvm.Input;
using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace InfinityRef.UI.ViewModels
{
    public class MainViewModel
    {
        private readonly ISaveLoadService saveLoadService;
        private readonly Core.Interfaces.IFilePicker filePicker;
        private CanvasViewModel? currentCanvas;

        public MainViewModel(ISaveLoadService saveLoadService, Core.Interfaces.IFilePicker filePicker)
        {
            this.saveLoadService = saveLoadService;
            this.filePicker = filePicker;

            LoadCanvasCommand = new RelayCommand(async () => await LoadCanvasAsync());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public RelayCommand LoadCanvasCommand { get; }
        public CanvasViewModel? CurrentCanvas
        {
            get => currentCanvas ?? null;
            set
            {
                if (currentCanvas != value)
                {
                    currentCanvas = value;
                    OnPropertyChanged(nameof(CurrentCanvas));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Sets the specified canvas as the active canvas.
        /// </summary>
        /// <param name="canvas">The canvas to set as active. Cannot be <see langword="null"/>.</param>
        public void SetActiveCanvas(CanvasViewModel canvasVm)
        {
            CurrentCanvas = canvasVm;
        }

        /// <summary>
        /// Handles the addition of a new image layer to the current canvas.
        /// </summary>
        /// <remarks>This method creates a new image layer using the provided <paramref name="imageData"/>
        /// and adds it to the current canvas. The operation is performed synchronously, but the method returns a
        /// completed task to support asynchronous workflows.</remarks>
        /// <param name="imageData">The image data in byte array format to be used for creating the new layer. Cannot be null or empty.</param>
        /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task HandleDrop(byte[] imageData, Position2D dropPoint, Dimension imageDimension)
        {
            var layer = new ImageLayer(imageData, dropPoint, imageDimension);
            CurrentCanvas?.AddLayer(layer);

            Debug.WriteLine("Layer added. Total layers: " + CurrentCanvas?.LayerCount);
            return Task.CompletedTask;
        }

        private async Task LoadCanvasAsync()
        {
        }

    }
}
