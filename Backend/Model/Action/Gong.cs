namespace Moba.Backend.Model.Action;

using Enum;

public class Gong : Base
{
    public Gong()
    {
        Name = "Gong";
    }

    public override ActionType Type => ActionType.Gong;
}