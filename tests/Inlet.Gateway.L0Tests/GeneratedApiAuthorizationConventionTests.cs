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
        bool includeActionAllowAnonymous,
        bool includeActionAuthorize = false
    )
    {
        object[] controllerAttributes = includeControllerAllowAnonymous ? [new AllowAnonymousAttribute()] : [];
        ControllerModel controller = new(typeof(GeneratedController).GetTypeInfo(), controllerAttributes);
        object[] actionAttributes = (includeActionAllowAnonymous, includeActionAuthorize) switch
        {
            (true, true) => [new AllowAnonymousAttribute(), new AuthorizeAttribute()],
            (true, false) => [new AllowAnonymousAttribute()],
            (false, true) => [new AuthorizeAttribute()],
            var _ => [],
        };
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
    ///     Apply should not add default authorization filter when explicit action authorize metadata already exists.
    /// </summary>
    [Fact]
    public void ApplyDoesNotLayerDefaultFilterWhenActionAuthorizeExists()
    {
        // Arrange
        GeneratedApiAuthorizationOptions options = new()
        {
            Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            AllowAnonymousOptOut = true,
            DefaultPolicy = "generated-default",
        };
        GeneratedApiAuthorizationConvention convention = new(options);
        ApplicationModel application = CreateApplicationModel(false, false, true);

        // Act
        convention.Apply(application);

        // Assert
        ControllerModel controller = application.Controllers.Single();
        ActionModel action = controller.Actions.Single();
        Assert.Contains(action.Attributes, attribute => attribute is IAuthorizeData);
        Assert.DoesNotContain(controller.Filters, filter => filter is AuthorizeFilter);
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
    ///     Apply preserves ASP.NET behavior where controller-level allow-anonymous bypasses action-level authorize metadata
    ///     when opt-out is enabled.
    /// </summary>
    [Fact]
    public void ApplyPreservesControllerAllowAnonymousOverActionAuthorizeWhenOptOutEnabled()
    {
        // Arrange
        GeneratedApiAuthorizationOptions options = new()
        {
            Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            AllowAnonymousOptOut = true,
        };
        GeneratedApiAuthorizationConvention convention = new(options);
        ApplicationModel application = CreateApplicationModel(true, false, true);

        // Act
        convention.Apply(application);

        // Assert
        ControllerModel controller = application.Controllers.Single();
        ActionModel action = controller.Actions.Single();
        Assert.Contains(controller.Attributes, attribute => attribute is IAllowAnonymous);
        Assert.Contains(action.Attributes, attribute => attribute is IAuthorizeData);
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