using System;

namespace GSSystemAnalyzer.Models
{
	public class ScanRequest
	{
		public string Root { get; set; } = string.Empty;
		public Guid? ScanId { get; set; }
	}
}
