namespace Moba.Backend.Model.Action;

using Enum;

/// <summary>
/// This action allows to sent commands to the digital model railway control center like Z21 from Roco.
/// Z21 and Roco are registered trademarks of Modelleisenbahn GmbH and are used solely for product identification purposes. Their mention is made without any promotional intent.
/// </summary>
public class Command : Base
{
    /// <summary>
    /// Initializes a new instance of the Command class with the specified byte array.
    /// Sets the default name to "New Command".
    /// </summary>
    /// <param name="bytes">The command bytes to be sent to the control center.</param>
    public Command(byte[] bytes)
    {
        Bytes = bytes;
        Name = "New Command";
    }

    /// <summary>
    /// Gets the action type, always returns ActionType.Command.
    /// </summary>
    public override ActionType Type => ActionType.Command;

    /// <summary>
    /// The raw byte array containing the command data to be sent to the digital control center.
    /// </summary>
    public byte[]? Bytes { get; set; }

    /// <summary>
    /// Executes the command by sending the byte array to the Z21 control center.
    /// If no Z21 connection is available or Bytes is empty, the command will be skipped with a debug message.
    /// </summary>
    /// <param name="context">Execution context containing the Z21 connection</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        if (context.Z21 != null && Bytes is { Length: > 0 })
        {
            await context.Z21.SendCommandAsync(Bytes);
        }
    }
}