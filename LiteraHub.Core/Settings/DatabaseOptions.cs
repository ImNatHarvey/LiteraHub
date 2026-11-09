namespace LiteraHub.Core.Settings;

public sealed class DatabaseOptions
{
    public string Server { get; init; } = ".\\SQLEXPRESS";
    public string? FallbackServer { get; init; }
    public string Name { get; init; } = "LiteraHub";
}