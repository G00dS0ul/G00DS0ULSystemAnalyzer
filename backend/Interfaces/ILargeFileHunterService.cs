using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface ILargeFileHunterService
	{
		Task<List<LargeFile>> GetTopLargeFilesAsync(string rootPath, int topN, CancellationToken cancellationToken = default);
	}
}
