namespace PuxDesign.Server.Models;

public sealed class FolderSnapshot
{
    public string RootPath { get; set; } = string.Empty;

    public DateTimeOffset LastAnalyzedAt { get; set; }

    public Dictionary<string, SnapshotFileEntry> Files { get; set; } = [];

    public List<string> Directories { get; set; } = [];
}
