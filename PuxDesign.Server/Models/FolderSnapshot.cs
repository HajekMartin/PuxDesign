namespace PuxDesign.Server.Models;

public sealed class FolderSnapshot
{
    public string RootPath { get; set; } = string.Empty;

    public DateTimeOffset LastAnalyzedAt { get; set; }

    public Dictionary<string, SnapshotFileEntry> Files { get; set; } = [];

    public List<string> Directories { get; set; } = [];
}

public sealed class SnapshotFileEntry
{
    public string Path { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public int Version { get; set; }
}
