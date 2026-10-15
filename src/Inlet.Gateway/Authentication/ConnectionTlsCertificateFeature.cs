using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Features;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Owns an independent public certificate for a private subscription decision.
/// </summary>
internal sealed class ConnectionTlsCertificateFeature
    : ITlsConnectionFeature,
      IDisposable
{
    /// <inheritdoc />
    public X509Certificate2? ClientCertificate { get; set; }

    private X509Certificate2? OwnedCertificate { get; set; }

    /// <inheritdoc />
    public void Dispose() => OwnedCertificate?.Dispose();

    /// <inheritdoc />
    public Task<X509Certificate2?> GetClientCertificateAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ClientCertificate);
    }

    /// <summary>
    ///     Replaces the decision's owned certificate using copied public data.
    /// </summary>
    /// <param name="certificateData">The poll's public certificate bytes, or null when absent.</param>
    internal void LoadCertificate(
        byte[]? certificateData
    )
    {
        OwnedCertificate?.Dispose();
        OwnedCertificate = certificateData is null ? null : X509CertificateLoader.LoadCertificate(certificateData);
        ClientCertificate = OwnedCertificate;
    }
}