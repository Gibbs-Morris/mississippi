using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Triggers generation of a composite Inlet server registration that
///     registers API endpoints, SignalR hub, observability, and Orleans client.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an assembly, the <c>InletServerCompositeGenerator</c> will:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Generate <c>Add{App}Server(WebApplicationBuilder)</c> that configures
///                 observability, Orleans client, API controllers, and real-time infrastructure.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Generate <c>Use{App}Server(WebApplication)</c> that configures
///                 middleware and maps endpoints (OpenAPI, controllers, Inlet hub, health, SPA fallback).
///             </description>
///         </item>
///     </list>
///     <para>
///         Example usage:
///         <code>
///         [assembly: GenerateInletServerComposite(AppName = "Spring")]
///         </code>
///         This generates <c>SpringServerRegistrations.AddSpringServer()</c> and
///         <c>SpringServerRegistrations.UseSpringServer()</c> which can replace
///         multiple manual registration calls in Program.cs.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenerateInletServerCompositeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the application name used in generated type and method names.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For example, <c>AppName = "Spring"</c> generates:
    ///         <list type="bullet">
    ///             <item><c>SpringServerRegistrations</c> (class name)</item>
    ///             <item><c>AddSpringServer()</c> (extension method for WebApplicationBuilder)</item>
    ///             <item><c>UseSpringServer()</c> (extension method for WebApplication)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public string? AppName { get; set; }

    /// <summary>
    ///     Gets or sets the SignalR hub path for Inlet real-time updates.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>/hubs/inlet</c>, which is the conventional path used across Mississippi samples.
    /// </remarks>
    public string HubPath { get; set; } = "/hubs/inlet";

    /// <summary>
    ///     Gets or sets the API route prefix for projection endpoints.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>/api</c>, which is the conventional prefix for API routes.
    /// </remarks>
    public string ApiPrefix { get; set; } = "/api";
}
