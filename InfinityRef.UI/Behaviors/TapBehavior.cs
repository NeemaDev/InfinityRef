using InfinityRef.UI.Interfaces;
using InfinityRef.UI.ViewModels;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;
using System.Windows.Input;

namespace InfinityRef.Behaviors
{
    public class TapBehavior : Behavior<SKCanvasView>
    {
        public static readonly BindableProperty CanvasInteractionServiceProperty
            = BindableProperty.Create(nameof(CanvasInteractionService), typeof(ICanvasInteractionService), typeof(TapBehavior), default(ICanvasInteractionService));

        public static readonly BindableProperty CanvasViewModelProperty
            = BindableProperty.Create(nameof(CanvasViewModel), typeof(CanvasViewModel), typeof(TapBehavior), default(CanvasViewModel));

        public static readonly BindableProperty RepaintCanvasCommandProperty
            = BindableProperty.Create(nameof(RepaintCanvasCommand), typeof(ICommand), typeof(TapBehavior));


        private SKCanvasView? canvasView;

        public ICanvasInteractionService CanvasInteractionService
        {
            get => (ICanvasInteractionService)GetValue(CanvasInteractionServiceProperty);
            set => SetValue(CanvasInteractionServiceProperty, value);
        }

        public CanvasViewModel CanvasViewModel
        {
            get => (CanvasViewModel)GetValue(CanvasViewModelProperty);
            set => SetValue(CanvasViewModelProperty, value);
        }

        public ICommand RepaintCanvasCommand
        {
            get => (ICommand)GetValue(RepaintCanvasCommandProperty);
            set => SetValue(RepaintCanvasCommandProperty, value);
        }

        protected override void OnAttachedTo(SKCanvasView canvas)
        {
            base.OnAttachedTo(canvas);

            canvasView = canvas;
            canvas.EnableTouchEvents = true;
            canvas.Touch += OnCanvasTouched;

            BindingContext = canvas.BindingContext;
            canvas.BindingContextChanged += OnBindingContextChanged;
        }

        protected override void OnDetachingFrom(SKCanvasView canvas)
        {
            canvas.Touch -= OnCanvasTouched;
            canvas.BindingContextChanged -= OnBindingContextChanged;

            canvasView = null;
            base.OnDetachingFrom(canvas);
        }

        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            if (sender is BindableObject bindable)
            {
                BindingContext = bindable.BindingContext;
            }
        }

        void OnCanvasTouched(object sender, SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Pressed)
            {
                // convert device → canvas coords if you have a service
                var devicePt = e.Location;

                if (CanvasInteractionService != null)
                {
                    devicePt = CanvasInteractionService.DeviceToCanvas(devicePt);
                }

                if (CanvasViewModel != null)
                {
                    var hitLayer = CanvasViewModel.GetLayer(devicePt);

                    if (hitLayer != null)
                    {
                        // Clear any selections.
                        CanvasViewModel.ClearSelection();

                        // Select the hit layer.
                        hitLayer.IsSelected = true;
                    }
                    else
                    {
                        // If no layer was hit, clear any selections
                        CanvasViewModel.ClearSelection();
                    }
                }
                else
                {
                    Debug.WriteLine("[TapBehavior] CanvasViewModel is null, cannot select layer.");
                }

                if (RepaintCanvasCommand?.CanExecute(e) ?? false)
                {
                    RepaintCanvasCommand.Execute(e);
                }
            }

            // Mark handled so pan/zoom or other gestures don’t also fire
            e.Handled = true;
        }

    }
}
