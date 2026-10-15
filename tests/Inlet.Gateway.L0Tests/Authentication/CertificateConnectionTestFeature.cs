using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Features;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Borrows a request-owned fixture certificate without taking disposal ownership.
/// </summary>
internal sealed class CertificateConnectionTestFeature : ITlsConnectionFeature
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CertificateConnectionTestFeature" /> class.
    /// </summary>
    /// <param name="certificate">The certificate owned by the request fixture.</param>
    internal CertificateConnectionTestFeature(
        X509Certificate2 certificate
    ) =>
        ClientCertificate = certificate;

    /// <inheritdoc />
    public X509Certificate2? ClientCertificate { get; set; }

    /// <inheritdoc />
    public Task<X509Certificate2?> GetClientCertificateAsync(
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(ClientCertificate);
}