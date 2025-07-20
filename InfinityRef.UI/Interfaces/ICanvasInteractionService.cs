using SkiaSharp;

namespace InfinityRef.UI.Interfaces
{
    public interface ICanvasInteractionService
    {
        float CurrentScale { get; }
        SKPoint CanvasTranslate { get; }

        // Pinch/Zoom.
        void StartPinch(float scale, SKPoint translate);
        void UpdatePinch(float pinchScale, SKPoint pinchCenter, SKSize viewSize);

        // Pan.
        void StartPan(SKPoint translate);
        void UpdatePan(SKPoint move);

        // Mouse-Wheel Zoom.
        void UpdateWheelZoom(float zoomFactor, SKPoint pointer);

        // Hit-testing & drop conversions.
        SKPoint DeviceToCanvas(SKPoint devicePoint);
    }
}
