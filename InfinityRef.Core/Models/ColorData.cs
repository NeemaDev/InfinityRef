namespace InfinityRef.Core.Models
{
    public struct ColorData
    {
        // Simple RGBA container.
        public ColorData(byte r, byte g, byte b, byte a = 255)
        {
            R = r; G = g; B = b; A = a;
        }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
    }
}
