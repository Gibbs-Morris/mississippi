using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.SignalRConnection;

/// <summary>
///     Tests for <see cref="SignalRConnectionRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("SignalRConnection")]
[AllureSubSuite("SignalRConnectionRegistrations")]
public sealed class SignalRConnectionRegistrationsTests
{
    /// <summary>
    ///     Verifies that AddSignalRConnectionFeature registers action reducers.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void AddSignalRConnectionFeatureRegistersReducers()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddSignalRConnectionFeature();

        // Assert - verify reducers are registered for SignalRConnectionState
        ServiceDescriptor[] reducerDescriptors = services
            .Where(sd => sd.ServiceType == typeof(IActionReducer<SignalRConnectionState>))
            .ToArray();

        // Should have 6 reducers (one for each action type)
        Assert.Equal(6, reducerDescriptors.Length);
    }

    /// <summary>
    ///     Verifies that AddSignalRConnectionFeature returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void AddSignalRConnectionFeatureReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddSignalRConnectionFeature();

        // Assert
        Assert.Same(services, result);
    }
}
