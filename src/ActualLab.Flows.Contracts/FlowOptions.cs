namespace ActualLab.Flows;

public record FlowOptions
{
    public static FlowOptions Default { get; set; } = new();

    public TimeSpan KeepAliveFor { get; init; } = TimeSpan.FromSeconds(10);
}
