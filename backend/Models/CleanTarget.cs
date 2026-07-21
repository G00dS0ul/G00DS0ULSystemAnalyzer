namespace GSSystemAnalyzer.Models;

/// <summary>Distinguishes temp directories from regenerable caches.</summary>
public enum CleanCategory
{
	Temp,
	Cache
}

/// <summary>
/// A single discoverable clean target with a human-readable label and category.
/// Used internally by ResolveCleanTargets(); the DTO exposed to the API is TempLocationPreview.
/// </summary>
public sealed record CleanTarget(string Path, string Label, CleanCategory Category);
