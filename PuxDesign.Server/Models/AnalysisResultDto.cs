namespace PuxDesign.Server.Models;

public sealed record AnalysisResultDto(
    string Path,
    DateTimeOffset AnalyzedAt,
    bool IsInitialSnapshot,
    IReadOnlyCollection<FileVersionDto> NewFiles,
    IReadOnlyCollection<FileVersionDto> ChangedFiles,
    IReadOnlyCollection<FileVersionDto> RemovedFiles,
    IReadOnlyCollection<string> RemovedDirectories,
    IReadOnlyCollection<FileVersionDto> CurrentFiles);

public sealed record FileVersionDto(string Path, int Version);
