using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="GeneratedApiAuthorizationConvention" />.
/// </summary>
public sealed class GeneratedApiAuthorizationConventionTests
{
    private static ApplicationModel CreateApplicationModel(
        bool includeControllerAllowAnonymous,
        bool includeActionAllowAnonymous
    )
    {
        object[] controllerAttributes = includeControllerAllowAnonymous ? [new AllowAnonymousAttribute()] : [];
        ControllerModel controller = new(typeof(GeneratedController).GetTypeInfo(), controllerAttributes);
        object[] actionAttributes = includeActionAllowAnonymous ? [new AllowAnonymousAttribute()] : [];
        ActionModel action = new(
            typeof(GeneratedController).GetMethod(nameof(GeneratedController.Get))!,
            actionAttributes);
        controller.Actions.Add(action);
        ApplicationModel application = new();
        application.Controllers.Add(controller);
        return application;
    }

    [GeneratedCode("AggregateControllerGenerator", "1.0")]
    private sealed class GeneratedController
    {
        public void Get()
        {
        }
    }

    /// <summary>
    ///     Apply preserves AllowAnonymous metadata when opt-out is enabled.
    /// </summary>
    [Fact]
    public void ApplyPreservesAllowAnonymousWhenOptOutEnabled()
    {
        // Arrange
        GeneratedApiAuthorizationOptions options = new()
        {
            Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            AllowAnonymousOptOut = true,
        };
        GeneratedApiAuthorizationConvention convention = new(options);
        ApplicationModel application = CreateApplicationModel(true, true);

        // Act
        convention.Apply(application);

        // Assert
        ControllerModel controller = application.Controllers.Single();
        ActionModel action = controller.Actions.Single();
        Assert.Contains(controller.Attributes, attribute => attribute is IAllowAnonymous);
        Assert.Contains(action.Attributes, attribute => attribute is IAllowAnonymous);
        Assert.DoesNotContain(controller.Filters, filter => filter is AuthorizeFilter);
    }

    /// <summary>
    ///     Apply removes AllowAnonymous metadata from generated controllers and actions when opt-out is disabled.
    /// </summary>
    [Fact]
    public void ApplyRemovesAllowAnonymousWhenOptOutDisabled()
    {
        // Arrange
        GeneratedApiAuthorizationOptions options = new()
        {
            Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            AllowAnonymousOptOut = false,
        };
        GeneratedApiAuthorizationConvention convention = new(options);
        ApplicationModel application = CreateApplicationModel(true, true);

        // Act
        convention.Apply(application);

        // Assert
        ControllerModel controller = application.Controllers.Single();
        ActionModel action = controller.Actions.Single();
        Assert.DoesNotContain(controller.Attributes, attribute => attribute is IAllowAnonymous);
        Assert.DoesNotContain(action.Attributes, attribute => attribute is IAllowAnonymous);
        Assert.DoesNotContain(controller.Filters, filter => filter is IAllowAnonymousFilter);
        Assert.DoesNotContain(action.Filters, filter => filter is IAllowAnonymousFilter);
        Assert.Contains(controller.Filters, filter => filter is AuthorizeFilter);
    }
}