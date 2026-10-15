using System;
using System.Linq;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Preserves native authentication registrations while routing private decision contexts.
/// </summary>
internal static class ConnectionAuthenticationRegistrations
{
    /// <summary>
    ///     Adds context-aware routing without replacing host-defined authentication implementations.
    /// </summary>
    /// <param name="services">The host's service registrations.</param>
    internal static void AddRouter(
        IServiceCollection services
    )
    {
        services.AddAuthenticationCore();
        ServiceDescriptor? registration = services.LastOrDefault(service =>
            !service.IsKeyedService && (service.ServiceType == typeof(IAuthenticationService)));
        if (registration is null ||
            (registration.ImplementationType is { } implementation && !IsFrameworkService(implementation)) ||
            (registration.ImplementationInstance is { } instance && !IsFrameworkService(instance.GetType())))
        {
            return;
        }

        services.Remove(registration);
        services.Add(
            ServiceDescriptor.Describe(
                typeof(IAuthenticationService),
                provider => CreateAuthentication(registration, provider),
                registration.Lifetime));
    }

    private static IAuthenticationService CreateAuthentication(
        ServiceDescriptor registration,
        IServiceProvider provider
    )
    {
        IAuthenticationService authentication = CreateOriginal(registration, provider);
        return authentication is AuthenticationService framework && IsFrameworkService(framework.GetType())
            ? new ConnectionAuthenticationRouter(framework)
            : authentication;
    }

    private static IAuthenticationService CreateOriginal(
        ServiceDescriptor registration,
        IServiceProvider provider
    )
    {
        if (registration.ImplementationInstance is IAuthenticationService instance)
        {
            return instance;
        }

        return registration.ImplementationFactory is { } factory
            ? (IAuthenticationService)factory(provider)
            : (IAuthenticationService)ActivatorUtilities.CreateInstance(provider, registration.ImplementationType!);
    }

    private static bool IsFrameworkService(
        Type implementation
    ) =>
        (implementation.Assembly == typeof(AuthenticationService).Assembly) &&
        typeof(AuthenticationService).IsAssignableFrom(implementation);
}