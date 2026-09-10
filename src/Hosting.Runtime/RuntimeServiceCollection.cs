using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Identifies a nonterminal runtime scope independently of its mutable registrations.
/// </summary>
internal sealed class RuntimeServiceCollection : ServiceCollection
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RuntimeServiceCollection" /> class.
    /// </summary>
    /// <param name="attachment">The owning attachment reservation.</param>
    public RuntimeServiceCollection(
        ServiceDescriptor attachment
    ) =>
        ((IServiceCollection)this).Add(attachment);
}