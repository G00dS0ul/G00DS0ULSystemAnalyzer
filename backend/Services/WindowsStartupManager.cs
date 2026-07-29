using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Interfaces;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace GSSystemAnalyzer.Services
{
    public class WindowsStartupManager : IStartupManager
    {
        private const string RunKeypath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ShadowKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        public Task<IEnumerable<StartupProgramDto>> GetStartupEntriesAsync()
        {
            var entries = new List<StartupProgramDto>();

            using (var userKey = Registry.CurrentUser.OpenSubKey(RunKeypath))
            {
                if (userKey != null)
                {
                    foreach (var valueName in userKey.GetValueNames())
                    {
                        var command = userKey.GetValue(valueName)?.ToString() ?? string.Empty;
                        var (path, args) = ParseCommand(command);

                        entries.Add(new StartupProgramDto
                        {
                            Id = valueName,
                            Name = valueName,
                            ExecutablePath = path,
                            Arguments = args,
                            IsEnabled = CheckIfEnabled(valueName),
                            Scope = "user",
                            Platform = "windows"
                        });
                    }
                }
            }

            using (var systemKey = Registry.LocalMachine.OpenSubKey(RunKeypath))
            {
                if (systemKey != null)
                {
                    foreach (var valueName in systemKey.GetValueNames())
                    {
                        var command = systemKey.GetValue(valueName)?.ToString() ?? string.Empty;
                        var (path, args) = ParseCommand(command);

                        entries.Add(new StartupProgramDto
                        {
                            Id = valueName,
                            Name = valueName,
                            ExecutablePath = path,
                            Arguments = args,
                            IsEnabled = CheckIfEnabled(valueName),
                            Scope = "system",
                            Platform = "windows"
                        });

                    }
                }
            }

            return Task.FromResult<IEnumerable<StartupProgramDto>>(entries);
        }

        public Task ToggleStartupEntryAsync(string id, bool enable)
        {
            using var shadowKey = Registry.CurrentUser.OpenSubKey(ShadowKeyPath, writable: true);
            if (shadowKey != null)
            {
                byte[]? currentValue = shadowKey.GetValue(id) as byte[];
                byte[] newValue = (currentValue != null && currentValue.Length >= 12) ? currentValue : new byte[12];

                newValue[0] = enable ? (byte)0x02 : (byte)0x03;

                shadowKey.SetValue(id, newValue, RegistryValueKind.Binary);
            }
            return Task.CompletedTask;
        }

        public Task DeleteStartupEntryAsync(string id)
        {
            using (var userKey = Registry.CurrentUser.OpenSubKey(RunKeypath, writable: true))
            {
                if (userKey?.GetValue(id) != null)
                {
                    userKey.DeleteValue(id, throwOnMissingValue: false);
                }
            }

            using (var systemKeyRead = Registry.LocalMachine.OpenSubKey(RunKeypath, writable: false))
            {
                if (systemKeyRead?.GetValue(id) != null)
                {
                    try
                    {
                        using var systemKeyWrite = Registry.LocalMachine.OpenSubKey(RunKeypath, writable: true);
                        systemKeyWrite?.DeleteValue(id, throwOnMissingValue: false);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new UnauthorizedAccessException($"Administrator privileges are required to delete the system-level application: {id}");
                    }
                }
            }

            return Task.CompletedTask;
        }

        public (string path, string args) ParseCommand(string command)
        {
            var match = Regex.Match(command, @"^(?:""([^""]+)""|([^""]+\.(?:exe|bat|cmd)))\s*(.*)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string path = !string.IsNullOrEmpty(match.Groups[1].Value) ? match.Groups[1].Value : match.Groups[2].Value;
                return (path, match.Groups[3].Value);
            }
            return (command, string.Empty);
        }

        private bool CheckIfEnabled(string id)
        {
            using var shadowKey = Registry.CurrentUser.OpenSubKey(ShadowKeyPath);
            var bytes = shadowKey?.GetValue(id) as byte[];

            return bytes == null || bytes.Length == 0 || bytes[0] == 0x02;
        }

    }
}