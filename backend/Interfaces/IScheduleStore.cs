using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IScheduleStore
{
	List<ScheduledScan> LoadAll();
	void SaveAll(List<ScheduledScan> schedules);
}
