using GSSystemAnalyzer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSSystemAnalyzer.Interfaces
{
    public interface IStartupManager
    {
        Task<IEnumerable<StartupProgramDto>> GetStartupEntriesAsync();
        Task ToggleStartupEntryAsync(string id, bool enable);
        Task DeleteStartupEntryAsync(string id);
    }
}
