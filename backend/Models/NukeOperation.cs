using System;
using System.Collections.Generic;

namespace GSSystemAnalyzer.Models;

/// <summary>
/// Represents a completed nuke operation stored in the session-scoped undo stack.
/// </summary>
public record NukeOperation(
	string OperationId,
	DateTime ExecutedAt,
	List<string> OriginalPaths,
	List<string> DeletedPaths,
	bool UsedRecycleBin,
	int DeletedFiles
);
