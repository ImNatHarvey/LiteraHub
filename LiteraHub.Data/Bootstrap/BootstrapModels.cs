namespace LiteraHub.Data.Bootstrap;

public sealed class BootstrapSummary
{
    public int TableCount { get; set; }
    public int RoleCount { get; set; }
    public int StatusCount { get; set; }
    public int CategoryCount { get; set; }
    public int UserCount { get; set; }
}

public sealed record BootstrapResult(
    string ServerUsed,
    string ConnectionString,
    IReadOnlyList<int> AppliedVersions,
    BootstrapSummary Summary);

public sealed class DatabaseBootstrapException : Exception
{
    public DatabaseBootstrapException(string message) : base(message) { }

    public DatabaseBootstrapException(string message, Exception inner) : base(message, inner) { }
}