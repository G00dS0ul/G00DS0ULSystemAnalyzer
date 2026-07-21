using System;
using Xunit;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Tests.Services
{
    public class WindowsStartupManagerTests
    {
        private readonly WindowsStartupManager _manager;

        public WindowsStartupManagerTests()
        {
            _manager = new WindowsStartupManager();
        }

        [Theory]
        [InlineData(@"C:\Windows\System32\cmd.exe", @"C:\Windows\System32\cmd.exe", "")]
        [InlineData(@"""C:\Program Files\Discord\Update.exe""", @"C:\Program Files\Discord\Update.exe", "")]
        [InlineData(@"""C:\Program Files\Discord\Update.exe"" --processStart Discord.exe", @"C:\Program Files\Discord\Update.exe", "--processStart Discord.exe")]
        [InlineData(@"C:\MyApp\app.exe -silent -minimized", @"C:\MyApp\app.exe", "-silent -minimized")]
        public void ParseCommand_HandlesVariousPathFormats_SafelyExtractsArgs(
            string rawCommand, string expectedPath, string expectedArgs)
        {
            var (path, args) = _manager.ParseCommand(rawCommand);

            Assert.Equal(expectedPath, path);
            Assert.Equal(expectedArgs, args);
        }
        
    }
}