#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is required for the reliable preview Cosmos emulator path
using Aspire.Hosting;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
#pragma warning disable ASPIRECERTIFICATES001
        emulator.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001
    });

await builder.Build().RunAsync();
