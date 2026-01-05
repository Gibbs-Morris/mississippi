// <copyright file="MemberList.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Cascade.Server.ViewModels;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Organisms;

/// <summary>
///     Organism component for displaying channel members.
/// </summary>
public sealed partial class MemberList : ComponentBase
{
    /// <summary>
    ///     Gets or sets the list of members to display. State flows DOWN via Parameter.
    /// </summary>
    [Parameter]
    public IReadOnlyList<MemberViewModel>? Members { get; set; }
}