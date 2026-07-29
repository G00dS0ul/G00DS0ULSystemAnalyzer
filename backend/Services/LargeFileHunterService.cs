using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Interfaces;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class LargeFileHunterService : ILargeFileHunterService
{
	private readonly ISettingService _settings;
	private readonly ILogger<LargeFileHunterService> _logger;

	public LargeFileHunterService(ISettingService settings, ILogger<LargeFileHunterService> logger)
	{
		_settings = settings;
		_logger = logger;
	}

	public async Task<List<LargeFile>> GetTopLargeFilesAsync(string rootPath, int topN, CancellationToken cancellationToken = default)
	{
		var config = _settings.Current.Scan;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		// To offload the heavy hardrive I/O to a background thread
		return await Task.Run(() =>
		{
			// The Min-Heap. 'long' is the priority (the file size)
			var topFiles = new PriorityQueue<LargeFile, long>();

			var options = new EnumerationOptions
			{
				IgnoreInaccessible = true, // This silently skips locked system folders
				RecurseSubdirectories = true,
				ReturnSpecialDirectories = false,
				AttributesToSkip = 0
			};

			if (config.SkipHiddenFiles) options.AttributesToSkip |= FileAttributes.Hidden;
			if (config.SkipSystemFiles) options.AttributesToSkip |= FileAttributes.System;

			// needs normalization of path to support linux 
			var directory = new DirectoryInfo(rootPath);

			foreach (var fileInfo in directory.EnumerateFiles("*", options))
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{

					long size = fileInfo.Length;

					if (topFiles.Count < topN)
					{
						// We haven't reached the limit yet, so add it
						topFiles.Enqueue(new LargeFile { Path = fileInfo.FullName, SizeBytes = size }, size);
					}
					else if (size > topFiles.Peek().SizeBytes)
					{
						// Means the queue is full. Kick out the smallest file, and add the new bigger one
						topFiles.EnqueueDequeue(new LargeFile { Path = fileInfo.FullName, SizeBytes = size }, size);
					}
				}
				catch
				{
					// just in case
					continue;
				}
			}

			// To extract the files from the queue into a list
			var result = new List<LargeFile>(topFiles.Count);
			while (topFiles.Count > 0)
			{
				result.Add(topFiles.Dequeue());
			}

			// A min-heap pops the smallest files files first, so we reverse it so the largest is at index 0
			result.Reverse();

			// To format the raw bytes into human-readable strings
			foreach (var file in result)
			{
				file.SizeFormatted = FormatSize(file.SizeBytes);
			}

			stopwatch.Stop();
			_logger.LogDebug("Large file scan completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

			return result;

		}, cancellationToken);

	}

	private string FormatSize(long bytes)
	{
		string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
		int counter = 0;
		decimal number = bytes;

		while (Math.Round(number / 1024) >= 1)
		{
			number /= 1024;
			counter++;
		}

		return string.Format("{0:n1} {1}", number, suffixes[counter]);
	}
}
