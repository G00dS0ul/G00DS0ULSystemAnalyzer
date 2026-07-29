namespace GSSystemAnalyzer.Interfaces
{
	public interface IFileSystemProvider
	{
		bool DirectoryExists(string path);
		bool FileExists(string path);
		string[] GetDirectories(string path, string searchPattern = "*");
		string[] GetFiles(string path, string searchPattern = "*");
		string ReadAllText(string path);
	}
}
