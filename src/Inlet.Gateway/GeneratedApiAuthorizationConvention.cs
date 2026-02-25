using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;


namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Applies global generated API authorization behavior to MVC application models.
/// </summary>
internal sealed class GeneratedApiAuthorizationConvention : IApplicationModelConvention
{
    private static readonly string[] SupportedGeneratorNames =
    [
        "AggregateControllerGenerator",
        "ProjectionEndpointsGenerator",
        "SagaControllerGenerator",
    ];

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratedApiAuthorizationConvention" /> class.
    /// </summary>
    /// <param name="options">The generated API authorization options.</param>
    public GeneratedApiAuthorizationConvention(
        GeneratedApiAuthorizationOptions options
    ) =>
        Options = options ?? throw new ArgumentNullException(nameof(options));

    private GeneratedApiAuthorizationOptions Options { get; }

    private static bool HasAllowAnonymous(
        ActionModel action
    ) =>
        action.Attributes.OfType<IAllowAnonymous>().Any() || action.Filters.OfType<IAllowAnonymousFilter>().Any();

    private static bool HasAllowAnonymous(
        ControllerModel controller
    ) =>
        controller.Attributes.OfType<IAllowAnonymous>().Any() ||
        controller.Filters.OfType<IAllowAnonymousFilter>().Any();

    private static bool IsGeneratedApiController(
        ControllerModel controller
    )
    {
        if (controller is null)
        {
            return false;
        }

        TypeInfo typeInfo = controller.ControllerType;
        return typeInfo.GetCustomAttributes<GeneratedCodeAttribute>(false)
            .Any(attr => SupportedGeneratorNames.Contains(attr.Tool, StringComparer.Ordinal));
    }

    private static void RemoveAllowAnonymous(
        ActionModel action
    )
    {
        if (action.Attributes is IList<object> actionAttributes)
        {
            object[] allowAnonymousAttributes = actionAttributes.OfType<IAllowAnonymous>().Cast<object>().ToArray();
            foreach (object allowAnonymousAttribute in allowAnonymousAttributes)
            {
                actionAttributes.Remove(allowAnonymousAttribute);
            }
        }

        IAllowAnonymousFilter[] allowAnonymousFilters = action.Filters.OfType<IAllowAnonymousFilter>().ToArray();
        foreach (IAllowAnonymousFilter allowAnonymousFilter in allowAnonymousFilters)
        {
            action.Filters.Remove(allowAnonymousFilter);
        }
    }

    private static void RemoveAllowAnonymous(
        ControllerModel controller
    )
    {
        if (controller.Attributes is IList<object> controllerAttributes)
        {
            object[] allowAnonymousAttributes = controllerAttributes.OfType<IAllowAnonymous>().Cast<object>().ToArray();
            foreach (object allowAnonymousAttribute in allowAnonymousAttributes)
            {
                controllerAttributes.Remove(allowAnonymousAttribute);
            }
        }

        IAllowAnonymousFilter[] allowAnonymousFilters = controller.Filters.OfType<IAllowAnonymousFilter>().ToArray();
        foreach (IAllowAnonymousFilter allowAnonymousFilter in allowAnonymousFilters)
        {
            controller.Filters.Remove(allowAnonymousFilter);
        }
    }

    /// <inheritdoc />
    public void Apply(
        ApplicationModel application
    )
    {
        ArgumentNullException.ThrowIfNull(application);
        if (Options.Mode != GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints)
        {
            return;
        }

        foreach (ControllerModel controller in application.Controllers.Where(IsGeneratedApiController))
        {
            bool hasControllerAllowAnonymous = HasAllowAnonymous(controller);
            if (hasControllerAllowAnonymous && !Options.AllowAnonymousOptOut)
            {
                RemoveAllowAnonymous(controller);
                hasControllerAllowAnonymous = false;
            }

            if (!hasControllerAllowAnonymous)
            {
                controller.Filters.Add(CreateDefaultAuthorizeFilter());
            }

            foreach (ActionModel action in controller.Actions)
            {
                bool hasActionAllowAnonymous = HasAllowAnonymous(action);
                if (hasActionAllowAnonymous && !Options.AllowAnonymousOptOut)
                {
                    RemoveAllowAnonymous(action);
                }
            }
        }
    }

    private AuthorizeFilter CreateDefaultAuthorizeFilter()
    {
        AuthorizeAttribute authorizeAttribute = new();
        if (!string.IsNullOrWhiteSpace(Options.DefaultPolicy))
        {
            authorizeAttribute.Policy = Options.DefaultPolicy;
        }

        if (!string.IsNullOrWhiteSpace(Options.DefaultRoles))
        {
            authorizeAttribute.Roles = Options.DefaultRoles;
        }

        if (!string.IsNullOrWhiteSpace(Options.DefaultAuthenticationSchemes))
        {
            authorizeAttribute.AuthenticationSchemes = Options.DefaultAuthenticationSchemes;
        }

        return new(new IAuthorizeData[] { authorizeAttribute });
    }
}