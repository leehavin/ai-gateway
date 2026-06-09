namespace DataChat.Core.Configuration;

public enum DomainsSource
{
    File,
    Database
}

public static class DomainsSourceParser
{
    public static DomainsSource Parse(string? value) =>
        string.Equals(value, "Database", StringComparison.OrdinalIgnoreCase)
            ? DomainsSource.Database
            : DomainsSource.File;
}
