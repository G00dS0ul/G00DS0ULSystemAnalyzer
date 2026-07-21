using System;
using System.Collections.Generic;
using System.IO;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Tests.Fakes;

public class FakeDriveDetectionService : IDriveDetectionService
{
	public List<DriveMetric> GetReadyDrives()
	{
		string actualSystemRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
		return new List<DriveMetric>
		{
			new DriveMetric { Name = actualSystemRoot, Label = "Main OS", IsReady = true }
		};
	}
}
