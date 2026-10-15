using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Recreates scoped handlers through the host's registrations while retaining its other scoped dependencies.
/// </summary>
internal sealed class SubscriptionAuthenticationHandlerFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscriptionAuthenticationHandlerFactory" /> class.
    /// </summary>
    /// <param name="services">The completed host registrations.</param>
    public SubscriptionAuthenticationHandlerFactory(
        IServiceCollection services
    ) =>
        Registrations = services.Where(service => typeof(IAuthenticationHandler).IsAssignableFrom(service.ServiceType))
            .ToList();

    private List<ServiceDescriptor> Registrations { get; }

    private static object? CreateHandler(
        ServiceDescriptor registration,
        Type handlerType,
        IServiceProvider services,
        object? serviceKey
    )
    {
        if (registration.IsKeyedService)
        {
            return registration.KeyedImplementationFactory is { } keyedFactory
                ? keyedFactory(services, serviceKey)
                : ActivatorUtilities.CreateInstance(services, registration.KeyedImplementationType ?? handlerType);
        }

        return registration.ImplementationFactory is { } factory
            ? factory(services)
            : ActivatorUtilities.CreateInstance(services, registration.ImplementationType ?? handlerType);
    }

    /// <summary>
    ///     Creates a scoped handler using the private decision provider for handler aliases.
    /// </summary>
    /// <param name="handlerType">The registered handler type.</param>
    /// <param name="services">The decision provider, which retains the host's non-handler dependencies.</param>
    /// <param name="serviceKey">The optional key used by a handler factory.</param>
    /// <returns>A new scoped handler, or null when the registration has another lifetime.</returns>
    internal IAuthenticationHandler? CreateScopedHandler(
        Type handlerType,
        IServiceProvider services,
        object? serviceKey = null
    )
    {
        ServiceDescriptor? registration = FindRegistration(handlerType, serviceKey);
        if (registration is not { Lifetime: ServiceLifetime.Scoped })
        {
            return null;
        }

        object? handler = CreateHandler(registration, handlerType, services, serviceKey);
        return handler as IAuthenticationHandler ??
               throw new InvalidOperationException(
                   $"The authentication handler factory for '{handlerType}' returned an incompatible instance.");
    }

    private ServiceDescriptor? FindRegistration(
        Type handlerType,
        object? serviceKey
    )
    {
        if (ReferenceEquals(serviceKey, KeyedService.AnyKey))
        {
            return null;
        }

        ServiceDescriptor? registration = Registrations.FindLast(service =>
            (service.ServiceType == handlerType) && Equals(service.ServiceKey, serviceKey));
        if (registration is not null || serviceKey is null)
        {
            return registration;
        }

        return Registrations.FindLast(service =>
            (service.ServiceType == handlerType) && ReferenceEquals(service.ServiceKey, KeyedService.AnyKey));
    }
}