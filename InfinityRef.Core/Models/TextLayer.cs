using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class TextLayer : Layer
    {
        public TextLayer(string text, SKColor color, float fontSize, string fontFamily, SKTextAlign textAlign)
        {
            Text = text;
            Color = color;
            FontSize = fontSize;
            FontFamily = fontFamily;
            TextAlign = textAlign;
        }

        private string Text { get; set; }
        private SKColor Color { get; set; }
        private float FontSize { get; set; }
        private string FontFamily { get; set; }
        private SKTextAlign TextAlign { get; set; }

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
        }
    }
}
