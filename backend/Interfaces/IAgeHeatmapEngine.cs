using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IAgeHeatmapEngine
{
	AgeHeatmapResult? Analyze(string root);
}
