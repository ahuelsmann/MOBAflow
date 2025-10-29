namespace Moba.Backend.Model;

using Enum;

/// <summary>
/// Represents a locomotive.
/// </summary>
public class Locomotive
{
    public Locomotive()
    {
        Name = "New Locomotive";
    }

    public string Name { get; set; }

    /// <summary>
    /// The position is relevant for double traction and describes the position of the locomotive within a train.
    /// </summary>
    public uint? Pos { get; set; }

    public uint? DigitalAddress { get; set; }

    public string? Manufacturer { get; set; }

    public string? ArticleNumber { get; set; }

    public string? Series { get; set; }

    public ColorScheme? ColorPrimary { get; set; }

    public ColorScheme? ColorSecondary { get; set; }

    public bool IsPushing { get; set; }

    public Details? Details { get; set; }
}