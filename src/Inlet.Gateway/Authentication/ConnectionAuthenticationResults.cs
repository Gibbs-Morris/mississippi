using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Attributes cached native handler outcomes without rerunning forwarding or claims transformation.
/// </summary>
internal static class ConnectionAuthenticationResults
{
    /// <summary>
    ///     Captures individual outcomes whose identities belong to the established composite caller.
    /// </summary>
    /// <param name="context">The original connection request with its complete feature set.</param>
    /// <param name="principal">The authenticated composite caller.</param>
    /// <param name="schemes">The schemes selected by the endpoint.</param>
    /// <returns>The attributable per-scheme results.</returns>
    internal static async Task<IReadOnlyDictionary<string, AuthenticateResult>> CaptureAsync(
        HttpContext context,
        ClaimsPrincipal principal,
        IReadOnlyList<string> schemes
    )
    {
        Dictionary<string, AuthenticateResult> results = new(StringComparer.Ordinal);
        AuthenticationHandlerProvider? handlers = GetFrameworkHandlers(context);
        if (handlers is null || (schemes.Count <= 1))
        {
            return new ReadOnlyDictionary<string, AuthenticateResult>(results);
        }

        foreach (string scheme in schemes)
        {
            IAuthenticationHandler? handler = await handlers.GetHandlerAsync(context, scheme);
            if (!CanReadCachedResult(handler))
            {
                continue;
            }

            AuthenticateResult result = await handler!.AuthenticateAsync();
            if (!result.Succeeded || ContainsOnlyEstablishedIdentities(result.Principal, principal))
            {
                results[scheme] = CopyResult(result);
            }
        }

        return new ReadOnlyDictionary<string, AuthenticateResult>(results);
    }

    /// <summary>
    ///     Checks that selected identities still belong to the bound caller.
    /// </summary>
    /// <param name="selected">The individual scheme principal.</param>
    /// <param name="established">The established composite principal.</param>
    /// <returns>Whether every identity belongs to the caller by reference.</returns>
    internal static bool ContainsOnlyEstablishedIdentities(
        ClaimsPrincipal selected,
        ClaimsPrincipal established
    ) =>
        selected.Identities.All(identity => established.Identities.Any(current => ReferenceEquals(current, identity)));

    /// <summary>
    ///     Copies ticket properties without substituting identities from another scheme.
    /// </summary>
    /// <param name="result">The original handler outcome.</param>
    /// <returns>The outcome with independent mutable properties.</returns>
    internal static AuthenticateResult CopyResult(
        AuthenticateResult result
    )
    {
        if (result.Succeeded)
        {
            return AuthenticateResult.Success(
                new(result.Principal, result.Properties.Clone(), result.Ticket.AuthenticationScheme));
        }

        return result.Failure is not null
            ? AuthenticateResult.Fail(result.Failure, result.Properties?.Clone())
            : AuthenticateResult.NoResult();
    }

    private static bool CanReadCachedResult(
        IAuthenticationHandler? handler
    )
    {
        if (handler is null)
        {
            return false;
        }

        Type? nativeBase = handler.GetType();
        while (nativeBase is not null &&
               (!nativeBase.IsGenericType ||
                (nativeBase.GetGenericTypeDefinition() != typeof(AuthenticationHandler<>))))
        {
            nativeBase = nativeBase.BaseType;
        }

        return nativeBase is not null &&
               !HasCustomTargetResolution(handler.GetType(), nativeBase) &&
               nativeBase.GetProperty(nameof(AuthenticationHandler<AuthenticationSchemeOptions>.Options))
                       ?.GetValue(handler) is
                   AuthenticationSchemeOptions
                   {
                       ForwardAuthenticate: null, ForwardDefault: null, ForwardDefaultSelector: null,
                   };
    }

    private static AuthenticationHandlerProvider? GetFrameworkHandlers(
        HttpContext context
    )
    {
        AuthenticationService? authentication = ConnectionAuthenticationRouter.GetFrameworkAuthentication(
            context.RequestServices.GetService<IAuthenticationService>());
        return authentication is AuthenticationService framework &&
               (authentication.GetType().Assembly == typeof(AuthenticationService).Assembly) &&
               (framework.Transform.GetType() == typeof(NoopClaimsTransformation)) &&
               framework.Handlers is AuthenticationHandlerProvider handlers &&
               (handlers.GetType() == typeof(AuthenticationHandlerProvider))
            ? handlers
            : null;
    }

    private static bool HasCustomTargetResolution(
        Type handlerType,
        Type nativeBase
    )
    {
        for (Type? current = handlerType; current is not null && (current != nativeBase); current = current.BaseType)
        {
            if (current.GetTypeInfo()
                .DeclaredMethods.Any(method => (method.Name == "ResolveTarget") && method.IsVirtual))
            {
                return true;
            }
        }

        return false;
    }
}