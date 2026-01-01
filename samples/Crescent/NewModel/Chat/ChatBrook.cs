using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Brook definition for the Chat aggregate.
/// </summary>
[BrookName("CRESCENT", "NEWMODEL", "CHAT")]
internal sealed class ChatBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CRESCENT.NEWMODEL.CHAT";
}