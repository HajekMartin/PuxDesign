namespace PuxDesign.Server.Models;

public sealed record AnalyzedFolderDto(
    string Path,
    DateTimeOffset LastAnalyzedAt,
    int FileCount);
