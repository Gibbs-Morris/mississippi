using System;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SagaStepAttribute : Attribute
{
    public SagaStepAttribute(int order)
    {
        Order = order;
    }

    public int Order { get; }
    public Type? Saga { get; set; }
}
