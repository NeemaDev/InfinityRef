using CommunityToolkit.Mvvm.Input;
using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.Services;
using InfinityRef.UI.ViewModels;
using SkiaSharp;
using System.Diagnostics;

namespace InfinityRef
{
    public partial class MainPage : ContentPage
    {
        private readonly IDragDropService dragDropService;

        public MainPage(MainViewModel viewModel,
                        DragDropService dragDropService,
                        ICanvasInteractionService canvasInteractionService)
        {
            CanvasInteractionService = canvasInteractionService;
            RenderCanvasCommand = new RelayCommand(RenderCanvas);

            MainViewModel = viewModel;
            this.dragDropService = dragDropService;

            // Set the first canvas.
            var canvasVm = new CanvasViewModel(new Canvas(), new LayerStackService());
            MainViewModel.SetActiveCanvas(canvasVm);

            // Initializing Component at the end due to behaviour needing the bindings early.
            InitializeComponent();

            Debug.WriteLine("Binding Context set in MainPage constructor");
            BindingContext = viewModel;

        }

        public ICanvasInteractionService CanvasInteractionService { get; private set; }
        public MainViewModel MainViewModel { get; private set; }
        public RelayCommand RenderCanvasCommand { get; }

        private void RenderCanvas()
        {
            if (CanvasView is not null)
            {
                CanvasView.InvalidateSurface();
            }
        }

        private void DrawRulers(SKCanvas canvas, int width, int height)
        {
            const float spacing = 50;   // 50px between ticks
            const float tickLen = 10;   // tick length in px
            const float textSize = 12;   // font size in px
            const float textGap = 2;    // gap between tick end and text

            using var tickPaint = new SKPaint
            {
                Color = SKColors.DarkGray,
                StrokeWidth = 1,
                IsAntialias = true,
            };

            using var typeface = SKTypeface.Default;
            using var font = new SKFont(typeface, textSize);

            var align = SKTextAlign.Left;

            // -- horizontal ticks + labels along the top edge --
            for (float x = 0; x <= width; x += spacing)
            {
                // draw the tick
                canvas.DrawLine(x, 0, x, tickLen, tickPaint);

                // label = the pixel-x coordinate
                var label = ((int)x).ToString();

                // draw centered under the tick
                float textX = x;
                float textY = tickLen + textGap + textSize;
                canvas.DrawText(label, textX, textY, align, font, tickPaint);
            }

            // -- vertical ticks + labels along the left edge --
            // align text to left so we offset by half the text width
            for (float y = 0; y <= height; y += spacing)
            {
                // draw the tick
                canvas.DrawLine(0, y, tickLen, y, tickPaint);

                // label = the pixel-y coordinate
                var label = ((int)y).ToString();

                // draw beside the tick, vertically centered on the line
                float textX = tickLen + textGap;

                // we offset baseline so text sits −(textSize/2) above the y
                float textY = y + (textSize / 2);
                canvas.DrawText(label, textX, textY, align, font, tickPaint);
            }
        }
    }
}
