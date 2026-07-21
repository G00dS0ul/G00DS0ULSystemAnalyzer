using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface ITempFolderCleanerService
{
	/// <summary>Enumerate all known temp locations, sum sizes, and return the breakdown.</summary>
	Task<TempPreviewResponse> PreviewAsync(CancellationToken cancellationToken = default);

	/// <summary>Delete the contents (not the directories themselves) of the selected temp paths,
	/// skipping locked files. Delegates actual deletion to the NukeProtocolService.</summary>
	Task<TempCleanResult> CleanAsync(List<string> paths, CancellationToken cancellationToken = default);
}
