using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Creates owned certificate data and independent TLS request contexts for authentication regressions.
/// </summary>
internal static class CertificateConnectionTestData
{
    /// <summary>
    ///     Creates a certificate with a fixture permission in its public subject.
    /// </summary>
    /// <param name="permission">The fixture permission.</param>
    /// <returns>A certificate owned by the caller.</returns>
    internal static X509Certificate2 CreateCertificate(
        string permission
    )
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new("CN=" + permission, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new(2041, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    ///     Creates a context whose TLS certificate is owned by the caller.
    /// </summary>
    /// <param name="services">The current request scope.</param>
    /// <param name="certificate">The current certificate, or null for a request without TLS credentials.</param>
    /// <returns>An independent request context.</returns>
    internal static DefaultHttpContext CreateContext(
        IServiceProvider services,
        X509Certificate2? certificate
    )
    {
        DefaultHttpContext context = new()
        {
            RequestServices = services,
        };
        context.Request.Scheme = "https";
        if (certificate is not null)
        {
            ITlsConnectionFeature feature = new CertificateConnectionTestFeature(certificate);
            context.Features.Set(feature);
        }

        return context;
    }
}