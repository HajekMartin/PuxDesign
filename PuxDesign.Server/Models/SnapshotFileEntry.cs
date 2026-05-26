namespace PuxDesign.Server.Models;

public sealed class SnapshotFileEntry
{
    public string Path { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public int Version { get; set; }
}
