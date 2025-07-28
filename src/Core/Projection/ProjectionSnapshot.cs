namespace Mississippi.Core.Projection;

[GenerateSerializer]
[Alias("Mississippi.Core.ProjectionSnapshot")]
public sealed record ProjectionSnapshot<TModel>
{
    [Id(3)]
    public required TModel Data { get; init; }

    [Id(0)]
    public long Version { get; init; }

    [Id(1)]
    public required string ProjectionPath { get; init; }

    [Id(2)]
    public required string AggegrateRootPath { get; init; }
}