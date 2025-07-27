namespace Mississippi.Core.Abstractions.Providers.Projection;

/// <summary>
/// Provides unified projection storage functionality combining read and write operations.
/// </summary>
public interface IProjectionStorageProvider : IProjectionStorageWriter, IProjectionStorageReader
{
}