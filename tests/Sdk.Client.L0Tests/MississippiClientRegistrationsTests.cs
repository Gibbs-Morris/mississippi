using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Sdk.Client.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiClientRegistrations" />.
/// </summary>
public sealed class MississippiClientRegistrationsTests
{
    private static WebAssemblyHostBuilder CreateTestWebAssemblyHostBuilder()
    {
        Assembly wasmAssembly = typeof(WebAssemblyHostBuilder).Assembly;
        Type internalJsMethodsType = wasmAssembly.GetType(
                                         "Microsoft.AspNetCore.Components.WebAssembly.Services.IInternalJSImportMethods",
                                         true) ??
                                     throw new InvalidOperationException("Internal JS methods interface not found.");
        MethodInfo createProxyMethod = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => (method.Name == nameof(DispatchProxy.Create)) &&
                              method.IsGenericMethodDefinition &&
                              (method.GetGenericArguments().Length == 2) &&
                              (method.GetParameters().Length == 0));
        MethodInfo genericCreate = createProxyMethod.MakeGenericMethod(
            internalJsMethodsType,
            typeof(TestJSImportMethodsProxy));
        object proxy = genericCreate.Invoke(null, null) ??
                       throw new InvalidOperationException("Proxy creation failed.");
        ConstructorInfo? ctor = typeof(WebAssemblyHostBuilder)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(c =>
                (c.GetParameters().Length == 1) && (c.GetParameters()[0].ParameterType == internalJsMethodsType));
        if (ctor is null)
        {
            throw new InvalidOperationException("WebAssemblyHostBuilder constructor not found.");
        }

        return (WebAssemblyHostBuilder)ctor.Invoke([proxy]);
    }

    private class TestJSImportMethodsProxy : DispatchProxy
    {
        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args
        )
        {
            if (targetMethod is null)
            {
                return null;
            }

            Type returnType = targetMethod.ReturnType;
            if (returnType == typeof(string))
            {
                return "http://localhost/";
            }

            if (returnType == typeof(bool))
            {
                return false;
            }

            if (returnType == typeof(Uri))
            {
                return new Uri("http://localhost/");
            }

            if (returnType == typeof(void))
            {
                return null;
            }

            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (returnType == typeof(ValueTask))
            {
                return ValueTask.CompletedTask;
            }

            if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                Type resultType = returnType.GetGenericArguments()[0];
                object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return Activator.CreateInstance(returnType, result);
            }

            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }
    }

    /// <summary>
    ///     AddMississippiClient should register options and apply configuration.
    /// </summary>
    [Fact]
    public void AddMississippiClientRegistersOptionsAndAppliesConfiguration()
    {
        WebAssemblyHostBuilder builder = CreateTestWebAssemblyHostBuilder();
        builder.AddMississippiClient(options =>
        {
            options.AutoReconnect = false;
            options.HubPathPrefix = "/hubs-custom";
            options.BaseAddress = new("https://localhost");
        });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<MississippiClientOptions> options = provider.GetRequiredService<IOptions<MississippiClientOptions>>();
        Assert.False(options.Value.AutoReconnect);
        Assert.Equal("/hubs-custom", options.Value.HubPathPrefix);
        Assert.Equal(new("https://localhost"), options.Value.BaseAddress);
    }
}