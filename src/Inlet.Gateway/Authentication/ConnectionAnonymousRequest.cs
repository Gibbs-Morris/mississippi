using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Copies anonymous poll request metadata without retaining its scope or body.
/// </summary>
internal sealed class ConnectionAnonymousRequest
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionAnonymousRequest" /> class.
    /// </summary>
    /// <param name="context">The current anonymous transport request.</param>
    public ConnectionAnonymousRequest(
        HttpContext context
    )
    {
        HttpRequest request = context.Request;
        CertificateData = context.Features.Get<ITlsConnectionFeature>()?.ClientCertificate?.RawData;
        Scheme = request.Scheme;
        Method = request.Method;
        PathBase = request.PathBase.Value ?? string.Empty;
        Path = request.Path.Value ?? string.Empty;
        QueryString = request.QueryString.Value ?? string.Empty;
        Protocol = request.Protocol;
        Headers = request.Headers.ToDictionary(
            header => header.Key,
            header => new StringValues(header.Value.ToArray()),
            StringComparer.OrdinalIgnoreCase);
    }

    private byte[]? CertificateData { get; }

    private IReadOnlyDictionary<string, StringValues> Headers { get; }

    private string Method { get; }

    private string Path { get; }

    private string PathBase { get; }

    private string Protocol { get; }

    private string QueryString { get; }

    private string Scheme { get; }

    /// <summary>
    ///     Loads copied credentials into the certificate holder owned by the private decision.
    /// </summary>
    /// <param name="certificate">The decision-owned holder.</param>
    internal void ApplyTlsCertificate(
        ConnectionTlsCertificateFeature certificate
    ) =>
        certificate.LoadCertificate(CertificateData);

    /// <summary>
    ///     Creates independent metadata for one private authentication decision.
    /// </summary>
    /// <returns>The poll's copied headers and route.</returns>
    internal IHttpRequestFeature CreateFeature() =>
        new HttpRequestFeature
        {
            Scheme = Scheme,
            Method = Method,
            PathBase = PathBase,
            Path = Path,
            QueryString = QueryString,
            Protocol = Protocol,
            Headers = new HeaderDictionary(
                Headers.ToDictionary(
                    header => header.Key,
                    header => new StringValues(header.Value.ToArray()),
                    StringComparer.OrdinalIgnoreCase)),
        };
}