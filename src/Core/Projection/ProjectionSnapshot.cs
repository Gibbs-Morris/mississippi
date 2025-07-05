namespace Mississippi.Core.Projection;

[GenerateSerializer]
[Alias("Mississippi.Core.ProjectionSnapshot")]
public sealed record ProjectionSnapshot<TModel>
{
    [Id(3)]
    public TModel Data { get; init; }

    [Id(0)]
    public long Version { get; init; }

    [Id(1)]
    public string ProjectionPath { get; init; }

    [Id(2)]
    public string AggegrateRootPath { get; init; }
}