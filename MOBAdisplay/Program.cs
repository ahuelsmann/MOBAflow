using Moba.Display;

using System.Drawing;

class Program
{
    static void Main()
    {
        using var bmp = new Bitmap("test.png"); // 240x280
        var frame = BitmapToRgb565.Convert(bmp);

        FrameSender.SendFrame(frame, "192.168.1.50");
        Console.WriteLine("Frame gesendet.");
    }
}