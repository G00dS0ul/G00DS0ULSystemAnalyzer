using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IDriveDetectionService
	{
		List<DriveMetric> GetReadyDrives();
	}
}
