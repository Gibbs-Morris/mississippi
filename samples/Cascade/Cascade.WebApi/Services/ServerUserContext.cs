using System;
using System.Linq;

using Cascade.Components.Services;

using Microsoft.AspNetCore.Http;


namespace Cascade.WebApi.Services;

/// <summary>
///     Server-side implementation of <see cref="IUserContext" /> for the WebApi host.
/// </summary>
/// <remarks>
///     In a real application, this would be backed by ASP.NET Core authentication.
///     For the demo, it uses a simple header-based approach.
/// </remarks>
internal sealed class ServerUserContext : IUserContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerUserContext" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public ServerUserContext(
        IHttpContextAccessor httpContextAccessor
    )
    {
        HttpContextAccessor = httpContextAccessor
                              ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public string? DisplayName =>
        HttpContextAccessor.HttpContext?.Request.Headers["X-User-DisplayName"].FirstOrDefault();

    /// <inheritdoc />
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    /// <inheritdoc />
    public string? UserId =>
        HttpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

    private IHttpContextAccessor HttpContextAccessor { get; }
}
