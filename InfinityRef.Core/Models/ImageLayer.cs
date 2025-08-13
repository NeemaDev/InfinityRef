namespace InfinityRef.Core.Models
{
    public class ImageLayer : Layer
    {
        public ImageLayer(byte[] bytes, Position2D position, Dimension imageDimension)
        {
            ImageBytes = bytes;
            Position = position;
            Dimension = imageDimension;
        }

        public byte[] ImageBytes { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public bool Grayscale { get; set; }
    }
}
