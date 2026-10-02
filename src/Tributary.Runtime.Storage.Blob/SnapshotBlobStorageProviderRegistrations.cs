using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Composes Blob snapshot storage through the staged Mississippi runtime builder.
/// </summary>
public static class SnapshotBlobStorageProviderRegistrations
{
    private static ConditionalWeakTable<IRuntimeBuilder, object> Registrations { get; } = new();

    /// <summary>Queues Blob snapshot storage using a host-owned keyed Blob client.</summary>
    /// <param name="builder">The runtime builder.</param>
    /// <param name="configure">Optional Blob storage options configuration.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddBlobSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        Action<SnapshotBlobStorageOptions>? configure = null
    ) =>
        AddCore(builder, configure, null, null);

    /// <summary>Queues Blob snapshot storage with a lazily-created keyed client.</summary>
    /// <param name="builder">The runtime builder.</param>
    /// <param name="blobConnectionString">The Blob service connection string.</param>
    /// <param name="configure">Optional Blob storage options configuration.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddBlobSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        string blobConnectionString,
        Action<SnapshotBlobStorageOptions>? configure = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobConnectionString);
        return AddCore(builder, configure, null, blobConnectionString);
    }

    /// <summary>Queues Blob snapshot storage using configuration and a host-owned keyed client.</summary>
    /// <param name="builder">The runtime builder.</param>
    /// <param name="configuration">The Blob snapshot options section.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddBlobSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, null);
    }

    /// <summary>Queues Blob snapshot storage using configuration and a lazily-created keyed client.</summary>
    /// <param name="builder">The runtime builder.</param>
    /// <param name="blobConnectionString">The Blob service connection string.</param>
    /// <param name="configuration">The Blob snapshot options section.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddBlobSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        string blobConnectionString,
        IConfiguration configuration
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobConnectionString);
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, blobConnectionString);
    }

    private static IRuntimeBuilder AddCore(
        IRuntimeBuilder builder,
        Action<SnapshotBlobStorageOptions>? configure,
        IConfiguration? configuration,
        string? blobConnectionString
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        if (!Registrations.TryAdd(builder, new()))
        {
            throw new BuilderValidationException(
            [
                new(
                    "SNAPSHOT_BLOB_DUPLICATE",
                    "Blob Snapshot storage has already been configured for this runtime.",
                    "Combine Blob Snapshot settings in one AddBlobSnapshotStorageProvider(...) call."),
            ]);
        }

        try
        {
            builder.ConfigureSilo(silo =>
            {
                SnapshotBlobStorageOptions snapshot = new();
                configuration?.Bind(snapshot);
                configure?.Invoke(snapshot);
                Validate(snapshot, blobConnectionString);
                RegisterCore(silo.Services, snapshot, blobConnectionString);
            });
            return builder;
        }
        catch
        {
            Registrations.Remove(builder);
            throw;
        }
    }

    private static void CopyOptions(
        SnapshotBlobStorageOptions source,
        SnapshotBlobStorageOptions target
    )
    {
        target.BlobServiceClientServiceKey = source.BlobServiceClientServiceKey;
        target.ContainerName = source.ContainerName;
        target.EnableCompression = source.EnableCompression;
        target.MaximumSnapshotDocumentSizeBytes = source.MaximumSnapshotDocumentSizeBytes;
        target.MaximumSnapshotPayloadSizeBytes = source.MaximumSnapshotPayloadSizeBytes;
    }

    private static ServiceDescriptor GetEffectiveProviderDescriptor(
        IServiceCollection services
    )
    {
        for (int index = services.Count - 1; index >= 0; index--)
        {
            ServiceDescriptor descriptor = services[index];
            if ((descriptor.ServiceType == typeof(ISnapshotStorageProvider)) && !descriptor.IsKeyedService)
            {
                return descriptor;
            }
        }

        throw new InvalidOperationException("No unkeyed snapshot provider was registered.");
    }

    private static void RegisterCore(
        IServiceCollection services,
        SnapshotBlobStorageOptions snapshot,
        string? blobConnectionString
    )
    {
        if (blobConnectionString is not null)
        {
            services.AddKeyedSingleton<BlobServiceClient>(
                SnapshotBlobDefaults.BlobServiceClientServiceKey,
                (_, _) => new(blobConnectionString));
        }

        services.AddOptions<SnapshotBlobStorageOptions>()
            .Configure(options => CopyOptions(snapshot, options))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor
                .Singleton<IValidateOptions<SnapshotBlobStorageOptions>, SnapshotBlobStorageOptionsValidator>());
        services.AddSingleton<ISnapshotBlobCodec, SnapshotBlobCodec>();
        services.AddSingleton<ISnapshotBlobOperations, SnapshotBlobOperations>();
        services.AddSingleton<ISnapshotBlobRepository, SnapshotBlobRepository>();
        services.TryAddSingleton<ISnapshotStorageProvider, SnapshotBlobStorageProvider>();
        ServiceDescriptor providerDescriptor = GetEffectiveProviderDescriptor(services);
        services.Add(
            ServiceDescriptor.Describe(
                typeof(ISnapshotStorageReader),
                provider => provider.GetRequiredService<ISnapshotStorageProvider>(),
                providerDescriptor.Lifetime));
        services.Add(
            ServiceDescriptor.Describe(
                typeof(ISnapshotStorageWriter),
                provider => provider.GetRequiredService<ISnapshotStorageProvider>(),
                providerDescriptor.Lifetime));
        services.AddHostedService<SnapshotBlobContainerInitializer>();
        services.AddKeyedSingleton<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey,
            (provider, _) =>
            {
                SnapshotBlobStorageOptions options =
                    provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
                BlobServiceClient client =
                    provider.GetRequiredKeyedService<BlobServiceClient>(options.BlobServiceClientServiceKey);
                return client.GetBlobContainerClient(options.ContainerName);
            });
    }

    private static void Validate(
        SnapshotBlobStorageOptions snapshot,
        string? blobConnectionString
    )
    {
        ValidateOptionsResult result = new SnapshotBlobStorageOptionsValidator().Validate(null, snapshot);
        if (result.Failed)
        {
            throw new BuilderValidationException(
            [
                new(
                    "SNAPSHOT_BLOB_OPTIONS_INVALID",
                    string.Join(" ", result.Failures),
                    "Correct the Blob snapshot options before building the runtime."),
            ]);
        }

        if (blobConnectionString is not null &&
            !string.Equals(
                snapshot.BlobServiceClientServiceKey,
                SnapshotBlobDefaults.BlobServiceClientServiceKey,
                StringComparison.Ordinal))
        {
            throw new BuilderValidationException(
            [
                new(
                    "SNAPSHOT_BLOB_CLIENT_KEY_CONFLICT",
                    "A connection string cannot be combined with a custom Blob client service key.",
                    "Register a host-owned keyed BlobServiceClient when using a custom key."),
            ]);
        }
    }
}