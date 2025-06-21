using static InfinityRef.Core.Models.Enums;

namespace InfinityRef.Core.Models
{
    public class TextLayer : Layer
    {
        public TextLayer(string text, ColorData color, float fontSize, string fontFamily, HorizontalTextAlignment textAlign)
        {
            Text = text;
            Color = color;
            FontSize = fontSize;
            FontFamily = fontFamily;
            TextAlign = textAlign;
        }

        public string Text { get; set; }
        public ColorData Color { get; set; }
        public float FontSize { get; set; }
        public string FontFamily { get; set; }
        public HorizontalTextAlignment TextAlign { get; set; }

    }
}
