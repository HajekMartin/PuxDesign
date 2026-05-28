using Microsoft.AspNetCore.Mvc;
using PuxDesign.Server.Models;
using PuxDesign.Server.Services;
using PuxDesign.Server.Services.FolderAnalysis;

namespace PuxDesign.Server.Controllers;

[ApiController]
[Route("api/folders")]
public sealed class FoldersController(IFolderAnalysisService folderAnalysisService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AnalyzedFolderDto>>> GetAnalyzedFolders(CancellationToken cancellationToken)
    {
        var folders = await folderAnalysisService.GetAnalyzedFoldersAsync(cancellationToken);
        return Ok(folders);
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalysisResultDto>> Analyze(
        [FromBody] AnalyzeFolderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await folderAnalysisService.AnalyzeAsync(request.Path, cancellationToken);
            return Ok(result);
        }
        catch (FolderAnalysisException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return BadRequest(new { message = $"K adresáři nebo souboru nelze přistoupit: {exception.Message}" });
        }
        catch (IOException exception)
        {
            return BadRequest(new { message = $"Adresář nelze analyzovat: {exception.Message}" });
        }
    }
}
