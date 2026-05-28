using PuxDesign.Server.Models;

namespace PuxDesign.Server.Services.FolderAnalysis;

public interface IFolderAnalysisService
{
    Task<IReadOnlyCollection<AnalyzedFolderDto>> GetAnalyzedFoldersAsync(CancellationToken cancellationToken);

    Task<AnalysisResultDto> AnalyzeAsync(string path, CancellationToken cancellationToken);
}
