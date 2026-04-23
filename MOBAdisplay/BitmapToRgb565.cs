using System.Drawing;

namespace Moba.Display;

public static class BitmapToRgb565
{
    public static byte[] Convert(Bitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;

        byte[] buffer = new byte[width * height * 2];
        int pos = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = bmp.GetPixel(x, y);

                ushort rgb =
                    (ushort)(((c.R & 0xF8) << 8) |
                             ((c.G & 0xFC) << 3) |
                             (c.B >> 3));

                buffer[pos++] = (byte)(rgb >> 8);
                buffer[pos++] = (byte)(rgb & 0xFF);
            }
        }

        return buffer;
    }
}