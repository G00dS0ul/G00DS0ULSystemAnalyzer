namespace GSSystemAnalyzer.Models
{
    public class StartupProgramDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string ExecutablePath { get; set; }
        public string? Arguments { get; set; }
        public bool IsEnabled { get; set; }
        public required string Scope { get; set; }
        public required string Platform { get; set; }
    }
}