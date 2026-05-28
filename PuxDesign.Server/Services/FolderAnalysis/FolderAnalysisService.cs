using Microsoft.Extensions.Options;
using PuxDesign.Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PuxDesign.Server.Services.FolderAnalysis;

public sealed class FolderAnalysisService : IFolderAnalysisService
{
    private const long MaxFileSizeBytes = 52_428_800L;
    private const int MaxFilesPerDirectory = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storagePath;

    public FolderAnalysisService(IOptions<SnapshotOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.StoragePath;
        _storagePath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<IReadOnlyCollection<AnalyzedFolderDto>> GetAnalyzedFoldersAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_storagePath);

        var folders = new List<AnalyzedFolderDto>();
        foreach (var snapshotFile in Directory.EnumerateFiles(_storagePath, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = await ReadSnapshotFileAsync(snapshotFile, cancellationToken);
            if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.RootPath))
            {
                continue;
            }

            folders.Add(new AnalyzedFolderDto(
                snapshot.RootPath,
                snapshot.LastAnalyzedAt,
                snapshot.Files.Count));
        }

        return folders
            .OrderBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<AnalysisResultDto> AnalyzeAsync(string path, CancellationToken cancellationToken)
    {
        var rootPath = NormalizeDirectoryPath(path);
        var previousSnapshot = await ReadSnapshotAsync(rootPath, cancellationToken);
        var currentSnapshot = await CreateSnapshotAsync(rootPath, previousSnapshot, cancellationToken);

        var previousFiles = previousSnapshot?.Files ?? [];
        var isInitialSnapshot = previousSnapshot is null;

        var newFiles = isInitialSnapshot
            ? []
            : currentSnapshot.Files
                .Where(file => !previousFiles.ContainsKey(file.Key))
                .Select(file => ToDto(file.Value))
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var changedFiles = isInitialSnapshot
            ? []
            : currentSnapshot.Files
                .Where(file => previousFiles.TryGetValue(file.Key, out var previousFile)
                    && !string.Equals(previousFile.Hash, file.Value.Hash, StringComparison.Ordinal))
                .Select(file => ToDto(file.Value))
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var removedFiles = isInitialSnapshot
            ? []
            : previousFiles
                .Where(file => !currentSnapshot.Files.ContainsKey(file.Key))
                .Select(file => ToDto(file.Value))
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var previousDirectories = previousSnapshot?.Directories ?? [];
        var currentDirectories = currentSnapshot.Directories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedDirectories = isInitialSnapshot
            ? []
            : previousDirectories
                .Where(directory => !currentDirectories.Contains(directory))
                .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        await WriteSnapshotAsync(currentSnapshot, cancellationToken);

        return new AnalysisResultDto(
            rootPath,
            currentSnapshot.LastAnalyzedAt,
            isInitialSnapshot,
            newFiles,
            changedFiles,
            removedFiles,
            removedDirectories,
            currentSnapshot.Files.Values
                .Select(ToDto)
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static FileVersionDto ToDto(SnapshotFileEntry file)
    {
        return new FileVersionDto(file.Path, file.Version);
    }

    private async Task<FolderSnapshot> CreateSnapshotAsync(
        string rootPath,
        FolderSnapshot? previousSnapshot,
        CancellationToken cancellationToken)
    {
        var files = new Dictionary<string, SnapshotFileEntry>(StringComparer.OrdinalIgnoreCase);
        var directories = new List<string>();
        var previousFiles = previousSnapshot?.Files ?? [];

        foreach (var directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            directories.Add(GetRelativePath(rootPath, directory));
        }

        foreach (var directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories).Prepend(rootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directoryFiles = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).ToArray();
            if (directoryFiles.Length > MaxFilesPerDirectory)
            {
                throw new FolderAnalysisException(
                    $"AdresÃ¡Å™ '{directory}' obsahuje {directoryFiles.Length} souborÅ¯. Limit je {MaxFilesPerDirectory} souborÅ¯ v jednom adresÃ¡Å™i.");
            }

            foreach (var filePath in directoryFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    throw new FolderAnalysisException(
                        $"Soubor '{filePath}' mÃ¡ {fileInfo.Length} bajtÅ¯. Limit je {MaxFileSizeBytes} bajtÅ¯.");
                }

                var relativePath = GetRelativePath(rootPath, filePath);
                var contentHash = await ComputeFileHashAsync(filePath, cancellationToken);
                var version = 1;

                if (previousFiles.TryGetValue(relativePath, out var previousFile))
                {
                    version = string.Equals(previousFile.Hash, contentHash, StringComparison.Ordinal)
                        ? previousFile.Version
                        : previousFile.Version + 1;
                }

                files[relativePath] = new SnapshotFileEntry
                {
                    Path = relativePath,
                    Hash = contentHash,
                    Version = version
                };
            }
        }

        return new FolderSnapshot
        {
            RootPath = rootPath,
            LastAnalyzedAt = DateTimeOffset.UtcNow,
            Files = files,
            Directories = directories
                .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string GetRelativePath(string rootPath, string path)
    {
        return Path.GetRelativePath(rootPath, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private async Task<FolderSnapshot?> ReadSnapshotAsync(string rootPath, CancellationToken cancellationToken)
    {
        var snapshotFile = GetSnapshotFilePath(rootPath);
        return File.Exists(snapshotFile)
            ? await ReadSnapshotFileAsync(snapshotFile, cancellationToken)
            : null;
    }

    private static async Task<FolderSnapshot?> ReadSnapshotFileAsync(string snapshotFile, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(snapshotFile);
        var snapshot = await JsonSerializer.DeserializeAsync<FolderSnapshot>(stream, JsonOptions, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        snapshot.Files = new Dictionary<string, SnapshotFileEntry>(snapshot.Files, StringComparer.OrdinalIgnoreCase);
        return snapshot;
    }

    private async Task WriteSnapshotAsync(FolderSnapshot snapshot, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_storagePath);

        var snapshotFile = GetSnapshotFilePath(snapshot.RootPath);
        await using var stream = File.Create(snapshotFile);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
    }

    private string GetSnapshotFilePath(string rootPath)
    {
        var pathBytes = Encoding.UTF8.GetBytes(rootPath.ToUpperInvariant());
        var hash = Convert.ToHexString(SHA256.HashData(pathBytes)).ToLowerInvariant();
        return Path.Combine(_storagePath, $"{hash}.json");
    }

    private static string NormalizeDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FolderAnalysisException("Cesta k adresáøi je povinná.");
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
        if (!Directory.Exists(fullPath))
        {
            throw new FolderAnalysisException($"Adresáø '{fullPath}' neexistuje.");
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
