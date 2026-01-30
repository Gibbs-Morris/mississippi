using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Triggers generation of a composite Inlet silo registration that
///     registers domain aggregates, projections, event sourcing, and Orleans silo infrastructure.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an assembly, the <c>InletSiloCompositeGenerator</c> will:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Generate <c>Add{App}Silo(WebApplicationBuilder)</c> that configures
///                 observability, Aspire resources, domain, event sourcing, and Orleans silo.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Generate <c>Use{App}Silo(WebApplication)</c> that configures
///                 health check endpoint mapping for Aspire orchestration.
///             </description>
///         </item>
///     </list>
///     <para>
///         Example usage:
///         <code>
///         [assembly: GenerateInletSiloComposite(AppName = "Spring")]
///         </code>
///         This generates <c>SpringSiloRegistrations.AddSpringSilo()</c> and
///         <c>SpringSiloRegistrations.UseSpringSilo()</c> which can replace
///         multiple manual registration calls in Program.cs.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenerateInletSiloCompositeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the application name used in generated type and method names.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For example, <c>AppName = "Spring"</c> generates:
    ///         <list type="bullet">
    ///             <item><c>SpringSiloRegistrations</c> (class name)</item>
    ///             <item><c>AddSpringSilo()</c> (extension method for WebApplicationBuilder)</item>
    ///             <item><c>UseSpringSilo()</c> (extension method for WebApplication)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public string? AppName { get; set; }

    /// <summary>
    ///     Gets or sets the Orleans stream provider name.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>mississippi-streaming</c>, which is the conventional name used across Mississippi.
    /// </remarks>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}