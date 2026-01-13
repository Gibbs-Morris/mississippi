using System;
using System.Collections.Generic;

using Cascade.Contracts.Projections;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Organisms.MessageList;

/// <summary>
///     Organism component for displaying a scrollable list of chat messages grouped by sender.
/// </summary>
public sealed partial class MessageList : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether messages are currently loading.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Gets or sets the list of messages to display.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ChannelMessageItem>? Messages { get; set; }

    private IEnumerable<MessageGroup> GroupedMessages => GroupMessagesBySender();

    private IEnumerable<MessageGroup> GroupMessagesBySender()
    {
        if (Messages is null || (Messages.Count == 0))
        {
            yield break;
        }

        List<ChannelMessageItem> currentGroup = [];
        string currentSender = string.Empty;
        DateTimeOffset groupStartTime = default;
        foreach (ChannelMessageItem message in Messages)
        {
            // Start a new group if sender changes or more than 5 minutes have passed
            bool shouldStartNewGroup = (message.SentBy != currentSender) ||
                                       ((message.SentAt - groupStartTime).TotalMinutes > 5);
            if (shouldStartNewGroup && (currentGroup.Count > 0))
            {
                yield return new()
                {
                    SentBy = currentSender,
                    FirstSentAt = groupStartTime,
                    Messages = [..currentGroup],
                };
                currentGroup.Clear();
            }

            if (currentGroup.Count == 0)
            {
                currentSender = message.SentBy;
                groupStartTime = message.SentAt;
            }

            currentGroup.Add(message);
        }

        // Yield the last group
        if (currentGroup.Count > 0)
        {
            yield return new()
            {
                SentBy = currentSender,
                FirstSentAt = groupStartTime,
                Messages = [..currentGroup],
            };
        }
    }
}