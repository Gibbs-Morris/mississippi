namespace Mississippi.AspNetCore.Orleans.Authentication.Grains;

using System;
using System.Threading.Tasks;
using global::Orleans;

/// <summary>
/// Grain interface for authentication ticket storage.
/// </summary>
public interface IAuthTicketGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the authentication ticket data.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation containing the ticket data,
    /// or null if not found or expired.
    /// </returns>
    Task<AuthTicketData?> GetAsync();

    /// <summary>
    /// Stores the authentication ticket data.
    /// </summary>
    /// <param name="data">The ticket data to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreAsync(AuthTicketData data);

    /// <summary>
    /// Renews the authentication ticket.
    /// </summary>
    /// <param name="expiresAt">The new expiration time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RenewAsync(DateTimeOffset expiresAt);

    /// <summary>
    /// Removes the authentication ticket.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync();
}
