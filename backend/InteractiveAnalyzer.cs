namespace GSSystemAnalyzer
{
	public static class InteractiveAnalyzer
	{
		/*
        public static async Task Main()
        {
            var scanner = new DiskScannerEngine();
            Console.CursorVisible = false;


            var currentPath = Directory.GetCurrentDirectory();
            var lastPath = string.Empty;
            var selectedIndex = 0;
            List<FileSystemInfo> items = new List<FileSystemInfo>();

            while (true)
            {
                if (currentPath != lastPath)
                {
                    items = scanner.LoadDirectoryItems(currentPath);

                    lastPath = currentPath;

                    // Keep calculating and don't freeze the UI
                    _ = scanner.CalculateMissingSizesAsync(items);

                    Console.Clear();
                }

                if (selectedIndex >= items.Count)
                {
                    selectedIndex = Math.Max(0, items.Count - 1);
                }

                DrawMenu(scanner, currentPath, items, selectedIndex);

                await Task.Delay(50);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.DownArrow:
                            selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                            break;
                        case ConsoleKey.UpArrow:
                            selectedIndex = Math.Max(0, selectedIndex - 1);
                            break;
                        case ConsoleKey.Backspace:
                        case ConsoleKey.Escape:
                            var parent = Directory.GetParent(currentPath);
                            if (parent != null)
                            {
                                currentPath = parent.FullName;
                                selectedIndex = 0;
                            }
                            break;
                        case ConsoleKey.Enter:
                            if (items[selectedIndex] is DirectoryInfo dir)
                            {
                                currentPath = dir.FullName;
                                selectedIndex = 0;
                            }
                            break;
                        case ConsoleKey.Delete:
                            scanner.ExecuteDelete(items[selectedIndex]);
                            lastPath = string.Empty; // Force reload after delete
                            break;
                    }
                }
            }
        }

        private static void DrawMenu(DiskScannerEngine scanner, string currentPath, List<FileSystemInfo> items, int selectedIndex)
        {
            Console.SetCursorPosition(0, 0);
            Console.CursorVisible = false;

            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine(@"
   _____  ___   ___  ____   _____  ___  _   _ _      _______        __
  / ____|/ _ \ / _ \|  _ \ / ____|/ _ \| | | | |    / ____\ \      / /
 | |  __| | | | | | | | | | (___ | | | | | | | |   | (___  \ \ /\ / / 
 | | |_ | | | | | | | | | |\___ \| | | | | | | |    \___ \  \ V  V /  
 | |__| | |_| | |_| | |_| |____) | |_| | |_| | |________) |  \_/\_/   
  \_____|\___/ \___/|____/|_____/ \___/ \___/|_____|_____/            
        ");

            Console.ResetColor();

            Console.WriteLine("=========================================================================".PadRight(Console.WindowWidth - 1));
            Console.WriteLine($"ROOT: {currentPath}".PadRight(Console.WindowWidth - 1));
            Console.WriteLine("[UP/DOWN] Navigate | [ENTER] Open | [BACKSPACE] Go Back | [DELETE] Nuke".PadRight(Console.WindowWidth - 1));
            Console.WriteLine("=========================================================================".PadRight(Console.WindowWidth - 1));

            var maxItems = Math.Max(5, Console.WindowHeight - 16);
            var startIndex = Math.Max(0, selectedIndex - maxItems / 2);
            var endIndex = Math.Min(items.Count, startIndex + maxItems);

            if (endIndex - startIndex < maxItems)
            {
                startIndex = Math.Max(0, endIndex - maxItems);
            }
            for (var i = startIndex; i < endIndex; i++)
            {
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ResetColor();
                }

                var item = items[i];

                string outputLine;

                if (item is DirectoryInfo dir)
                {
                    if (scanner.DirectorySizeCache.TryGetValue(dir.FullName, out long size))
                    {
                        outputLine = $"[DIR] {item.Name,-40} | {(size /1048576.0):F2} MB";
                    }
                    else
                    {
                        outputLine = $"[DIR] {item.Name,-40} | Calculating...";
                    }

                }
                else if (item is FileInfo file)
                {
                    outputLine = $"[FILE] {item.Name,-40} | {(file.Length / 1048576.0):F2} MB";
                }

                else
                {
                    outputLine = $"{item.Name,-40}";

                }

                Console.WriteLine(outputLine.PadRight(Console.WindowWidth - 1));
            }

            Console.ResetColor();

            for (int i = Console.CursorTop; i < Console.WindowHeight - 1; i++)
            {
                Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            }
        }
        */
	}

}
