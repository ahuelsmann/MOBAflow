namespace Moba.Backend.Model.Action;

using Moba.Backend.Enum;

public class Command : Base
{
    public Command(byte[] bytes)
    {
        Bytes = bytes;
        Name = "New Command";
    }

    public override ActionType Type => ActionType.Command;

    public byte[]? Bytes { get; set; }

    public string? BytesString { get; set; }
}
