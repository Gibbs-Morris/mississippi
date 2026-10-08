using System;
using System.Collections.Generic;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Keeps a subscription decision's authentication state separate from transport updates.
/// </summary>
internal sealed class SubscriptionAuthenticationFeatures
    : IHttpAuthenticationFeature,
      IAuthenticateResultFeature
{
    private SubscriptionAuthenticationFeatures(
        ClaimsPrincipal principal,
        AuthenticateResult? result
    )
    {
        Principal = principal;
        Result = result;
    }

    /// <inheritdoc />
    public AuthenticateResult? AuthenticateResult
    {
        get => Result;
        set
        {
            Result = value;
            Principal = value?.Principal;
        }
    }

    /// <inheritdoc />
    public ClaimsPrincipal? User
    {
        get => Principal;
        set
        {
            Principal = value;
            Result = null;
        }
    }

    private ClaimsPrincipal? Principal { get; set; }

    private AuthenticateResult? Result { get; set; }

    /// <summary>
    ///     Preserves request features while giving a decision its own coupled authentication features.
    /// </summary>
    /// <param name="context">The retained connection HTTP context.</param>
    /// <returns>An HTTP context whose authentication changes cannot replace the transport principal.</returns>
    public static HttpContext CreateContext(
        HttpContext context
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        FeatureCollection features = new();
        foreach (KeyValuePair<Type, object> feature in context.Features)
        {
            features[feature.Key] = feature.Value;
        }

        SubscriptionAuthenticationFeatures authentication = new(
            context.User,
            context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult);
        features.Set<IHttpAuthenticationFeature>(authentication);
        features.Set<IAuthenticateResultFeature>(authentication);
        return new DefaultHttpContext(features);
    }
}