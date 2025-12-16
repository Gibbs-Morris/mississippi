using System;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.AspNetCore.Orleans.Authentication.Grains;

/// <summary>
///     Grain interface for authentication ticket storage.
/// </summary>
public interface IAuthTicketGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the authentication ticket data.
    /// </summary>
    /// <returns>
    ///     A task representing the asynchronous operation containing the ticket data,
    ///     or null if not found or expired.
    /// </returns>
    Task<AuthTicketData?> GetAsync();

    /// <summary>
    ///     Removes the authentication ticket.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync();

    /// <summary>
    ///     Renews the authentication ticket.
    /// </summary>
    /// <param name="expiresAt">The new expiration time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RenewAsync(
        DateTimeOffset expiresAt
    );

    /// <summary>
    ///     Stores the authentication ticket data.
    /// </summary>
    /// <param name="data">The ticket data to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreAsync(
        AuthTicketData data
    );
}