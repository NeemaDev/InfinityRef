namespace InfinityRef.Core.Models
{
    public class CanvasContainer : Layer
    {
        public CanvasContainer()
        {
            Canvas = new Canvas();
        }

        public Canvas Canvas { get; set; }
    }
}
