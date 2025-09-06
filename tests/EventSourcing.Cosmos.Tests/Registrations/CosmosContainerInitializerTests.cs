using System.Net;
using System.Reflection;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Registrations;

/// <summary>
///     Tests for Cosmos container/database initialization wiring.
/// </summary>
public class CosmosContainerInitializerTests
{
    /// <summary>
    ///     Verifies that the initializer creates the database and container when not found.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task StartAsyncCreatesDatabaseAndContainerWhenContainerNotFound()
    {
        // Arrange
        BrookStorageOptions opts = new()
        {
            DatabaseId = "db",
        };
        Mock<CosmosClient> cosmos = new();
        Mock<Database> db = new();
        Mock<Container> existingContainer = new();

        // Database creation
        Mock<DatabaseResponse> dbResp = new();
        dbResp.SetupGet(r => r.Database).Returns(db.Object);
        cosmos.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                It.Is<string>(s => s == opts.DatabaseId),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbResp.Object);

        // DI factory path
        cosmos.Setup(c => c.GetDatabase(It.Is<string>(s => s == opts.DatabaseId))).Returns(db.Object);
        db.Setup(d => d.GetContainer(It.IsAny<string>())).Returns(existingContainer.Object);

        // Force initializer to take NotFound branch when checking existing container
        existingContainer.Setup(c => c.ReadContainerAsync(null, default))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));

        // Expect container creation
        db.Setup(d => d.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(),
                It.Is<string>(pk => pk == "/brookPartitionKey"),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ContainerResponse>());
        ServiceCollection services = new();
        services.AddSingleton<IOptions<BrookStorageOptions>>(Options.Create(opts));
        services.AddSingleton(cosmos.Object);
        services.AddCosmosBrookStorageProvider();
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService hosted = provider.GetRequiredService<IHostedService>();

        // Act
        await hosted.StartAsync(CancellationToken.None);

        // Assert
        db.Verify(
            d => d.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(),
                "/brookPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that an existing container with wrong partition key causes startup to throw.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task StartAsyncThrowsWhenExistingContainerPartitionKeyWrong()
    {
        // Arrange
        BrookStorageOptions opts = new()
        {
            DatabaseId = "db2",
            LockContainerName = "locks",
        };
        Mock<CosmosClient> cosmos = new();
        Mock<Database> db = new();
        Mock<Container> existingContainer = new();

        // Database creation
        Mock<DatabaseResponse> dbResp = new();
        dbResp.SetupGet(r => r.Database).Returns(db.Object);
        cosmos.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                It.Is<string>(s => s == opts.DatabaseId),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbResp.Object);

        // DI factory path
        cosmos.Setup(c => c.GetDatabase(It.Is<string>(s => s == opts.DatabaseId))).Returns(db.Object);
        db.Setup(d => d.GetContainer(It.IsAny<string>())).Returns(existingContainer.Object);

        // Existing container with wrong PK path
        Mock<ContainerResponse> containerResp = new();
        containerResp.SetupGet(r => r.Resource).Returns(new ContainerProperties("brooks", "/wrong"));
        existingContainer.Setup(c => c.ReadContainerAsync(null, default)).ReturnsAsync(containerResp.Object);
        ServiceCollection services = new();
        services.AddSingleton<IOptions<BrookStorageOptions>>(Options.Create(opts));
        services.AddSingleton(cosmos.Object);
        services.AddCosmosBrookStorageProvider();
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService hosted = provider.GetRequiredService<IHostedService>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => hosted.StartAsync(CancellationToken.None));
    }

    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter = null
    )
    {
        Type type = typeof(CosmosException);
        ConstructorInfo[] ctors =
            type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? ctor =
            ctors.FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode)));
        if (ctor is null)
        {
            throw new InvalidOperationException("No suitable CosmosException constructor found for tests.");
        }

        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo p = parameters[i];
            if (p.ParameterType == typeof(string))
            {
                args[i] = string.Empty;
            }
            else if (p.ParameterType == typeof(HttpStatusCode))
            {
                args[i] = statusCode;
            }
            else if (p.ParameterType == typeof(int))
            {
                args[i] = 0;
            }
            else if (p.ParameterType == typeof(long))
            {
                args[i] = 0L;
            }
            else if (p.ParameterType == typeof(TimeSpan))
            {
                args[i] = retryAfter ?? TimeSpan.Zero;
            }
            else if (p.ParameterType.IsValueType)
            {
                args[i] = Activator.CreateInstance(p.ParameterType);
            }
            else
            {
                args[i] = null;
            }
        }

        CosmosException? instance = (CosmosException?)ctor.Invoke(args);
        if (instance is null)
        {
            throw new InvalidOperationException("Failed to construct CosmosException");
        }

        return instance;
    }
}