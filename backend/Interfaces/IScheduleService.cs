using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IScheduleService
{
	List<ScheduledScan> GetAll();
	ScheduledScan? GetById(Guid id);
	ScheduledScan Create(CreateScheduleRequest request);
	ScheduledScan? Update(Guid id, UpdateScheduleRequest request);
	bool Delete(Guid id);
	List<ScheduledScan> GetDueSchedules(DateTimeOffset now);
	void MarkCompleted(Guid id, DateTimeOffset completedAt);
}
