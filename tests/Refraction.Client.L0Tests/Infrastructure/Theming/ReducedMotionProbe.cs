using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure.Theming;

/// <summary>Observes the provider's named cascade in rendering tests.</summary>
internal sealed class ReducedMotionProbe : ComponentBase
{
    /// <summary>Initializes a new instance of the <see cref="ReducedMotionProbe" /> class.</summary>
    /// <remarks>The public constructor is required by the component activator.</remarks>
    public ReducedMotionProbe()
    {
    }

    /// <summary>Gets or sets a value indicating whether reduced motion was requested.</summary>
    [CascadingParameter(Name = "RefractionReducedMotion")]
    public bool IsReducedMotion { get; set; }
}