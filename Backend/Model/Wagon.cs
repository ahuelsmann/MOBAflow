namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class Wagon
{
    public Wagon()
    {
        Name = "New Wagon";
    }

    public string Name { get; set; }
    public uint Pos { get; set; }
    public uint? DigitalAddress { get; set; }
    public string? Manufacturer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Series { get; set; }
    public ColorScheme? ColorPrimary { get; set; }
    public ColorScheme? ColorSecondary { get; set; }
    public Details? Details { get; set; }
}