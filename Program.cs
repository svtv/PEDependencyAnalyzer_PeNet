using System.Text;
using System.Text.RegularExpressions;
using PeNet;

namespace PEDependencyAnalyzer
{
    internal class Program
    {
        private class DllInfo
        {
            public required string Name { get; init; }
            public required string FullPath { get; init; }
        }

        private record PublishOptions
        {
            public bool Enabled { get; init; }
            public string DirectoryName { get; init; } = "publish";
            public bool IncludeRuntimeDll { get; init; } = true;
            public bool IncludeVirtualDll { get; init; } = true;
        }

        private static readonly HashSet<string> AnalyzedFiles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DllInfo> SystemDependencies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DllInfo> VirtualDependencies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DllInfo> RuntimeDependencies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DllInfo> OtherDependencies = new(StringComparer.OrdinalIgnoreCase);
        
        // Cache for found DLLs
        private static readonly Dictionary<string, string?> DllPathCache = new(StringComparer.OrdinalIgnoreCase);
        
        // Compiled regular expressions
        private static readonly Regex[] CompiledRuntimePatterns;

        // Runtime DLL patterns
        // For details, see https://learn.microsoft.com/en-us/cpp/windows/determining-which-dlls-to-redistribute?view=msvc-160
        private static readonly string[] RuntimePatterns =
        [
            @"^vcruntime\d+(_\d+)?\.dll$",	// Runtime Library for native code.
            @"^vccorlib\d+\.dll$",	        // Runtime Library for managed code.
            @"^msvcp\d+(_\d+)?\.dll$",	    // C++ Standard Library for native code.
            @"^msvcr\d+\.dll$",	            // C++ Standard Library for native code.
            @"^concrt\d+\.dll$",	        // Concurrency Runtime Library for native code.
            @"^mfc\d+\.dll$",	            // Microsoft Foundation Classes (MFC) Library.
            @"^mfc\d+.*\.dll$",	            // Microsoft Foundation Classes (MFC) Library Resources.
            @"^mfc\d+u\.dll$",	            // MFC Library with Unicode support.
            @"^mfcmifc80\.dll$",	        // MFC Managed Interfaces Library.
            @"^mfcm\d+\.dll$",	            // MFC Managed Library.
            @"^mfcm\d+u\.dll$",	            // MFC Managed Library with Unicode support.
            @"^vcamp\d+\.dll$",	            // AMP Library for native code.
            @"^vcomp\d+\.dll$",	            // OpenMP Library for native code.

                                            // And additionally
            @"^ucrtbase.*\.dll$",           // ucrtbase.dll, ucrtbased.dll
            @"^msvcrt.*\.dll$",             // msvcrt.dll, msvcrtd.dll
            @"^msvcp_win\.dll$"             // msvcp_win.dll
        ];

        private static readonly string[] SystemDirectories = 
        [
            @"C:\Windows\system32",
            @"C:\Windows\SysWOW64",
            @"C:\Windows"
        ];

        static Program()
        {
            // Compile regular expressions during initialization
            CompiledRuntimePatterns = RuntimePatterns
                .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                .ToArray();
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var publishOptions = ParsePublishOptions(args);
            var targetPath = args[0];

            if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
            {
                Console.WriteLine($"Path does not exist: {targetPath}");
                return;
            }

            if (File.Exists(targetPath))
            {
                AnalyzeFile(targetPath, 0);
            }
            else
            {
                var files = Directory.GetFiles(targetPath, "*.exe", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(targetPath, "*.dll", SearchOption.AllDirectories))
                    .ToArray();

                // Parallel file processing
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        AnalyzeFile(file, 0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error analyzing {Path.GetFileName(file)}: {ex.Message}");
                    }
                });
            }

            PrintDependenciesByType();

            if (publishOptions.Enabled && File.Exists(targetPath))
            {
                PublishDependencies(targetPath, publishOptions);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: PEDependencyAnalyzer <file_or_directory_path> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --publish[=directory_name]     Enable publish mode (default directory: 'publish')");
            Console.WriteLine("  --no-runtime                   Exclude runtime DLLs from publish");
            Console.WriteLine("  --no-virtual                   Exclude virtual DLLs from publish");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  PEDependencyAnalyzer app.exe");
            Console.WriteLine("  PEDependencyAnalyzer app.exe --publish");
            Console.WriteLine("  PEDependencyAnalyzer app.exe --publish=dist --no-runtime");
        }

        private static PublishOptions ParsePublishOptions(string[] args)
        {
            var options = new PublishOptions
            {
                Enabled = false,
                DirectoryName = "publish",
                IncludeRuntimeDll = true,
                IncludeVirtualDll = true
            };

            foreach (var arg in args.Skip(1)) // Skip the first argument (file path)
            {
                if (arg.StartsWith("--publish"))
                {
                    options = options with { Enabled = true };
                    var parts = arg.Split('=', 2);
                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        options = options with { DirectoryName = parts[1] };
                    }
                }
                else if (arg == "--no-runtime")
                {
                    options = options with { IncludeRuntimeDll = false };
                }
                else if (arg == "--no-virtual")
                {
                    options = options with { IncludeVirtualDll = false };
                }
            }

            return options;
        }

        private static void PublishDependencies(string sourceFile, PublishOptions options)
        {
            var publishDir = Path.Combine(Path.GetDirectoryName(sourceFile) ?? ".", options.DirectoryName);
            Directory.CreateDirectory(publishDir);

            // Copy source file
            var fileName = Path.GetFileName(sourceFile);
            Console.WriteLine($"\nPublishing to {publishDir}");
            Console.WriteLine($"Copying {fileName}");
            File.Copy(sourceFile, Path.Combine(publishDir, fileName), true);

            // Helper function to copy dependencies
            void CopyDependencies(string title, IEnumerable<DllInfo> dependencies)
            {
                foreach (var dll in dependencies)
                {
                    if (string.IsNullOrEmpty(dll.FullPath) || !File.Exists(dll.FullPath))
                        continue;

                    Console.WriteLine($"Copying {dll.Name}");
                    File.Copy(dll.FullPath, Path.Combine(publishDir, dll.Name), true);
                }
            }

            // Copy other dependencies
            CopyDependencies("Other Dependencies", OtherDependencies.Values);

            // Copy runtime dependencies if enabled
            if (options.IncludeRuntimeDll)
            {
                CopyDependencies("Runtime Dependencies", RuntimeDependencies.Values);
            }

            // Copy virtual dependencies if enabled
            if (options.IncludeVirtualDll)
            {
                CopyDependencies("Virtual Dependencies", VirtualDependencies.Values);
            }

            Console.WriteLine("Publishing completed");
        }

        private static void ClassifyDependency(string dll, string? fullPath)
        {
            var dllInfo = new DllInfo { Name = dll, FullPath = fullPath ?? "" };

            // First check if it's a virtual dependency (API Set)
            if (dll.StartsWith("api-ms-win-", StringComparison.OrdinalIgnoreCase) ||
                dll.StartsWith("ext-ms-win-", StringComparison.OrdinalIgnoreCase))
            {
                VirtualDependencies[dll] = dllInfo;
                return;
            }

            // Then check if it's a runtime dependency
            if (IsRuntimeDll(dll))
            {
                RuntimeDependencies[dll] = dllInfo;
                return;
            }

            // Check if the DLL is in a system directory
            if (!string.IsNullOrEmpty(fullPath) && 
                SystemDirectories.Any(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
            {
                SystemDependencies[dll] = dllInfo;
                return;
            }

            // If none of the above, check if it's in the predefined system DLLs list
            if (IsSystemDll(dll))
            {
                SystemDependencies[dll] = dllInfo;
                return;
            }

            // If none of the above conditions are met, it's another dependency
            OtherDependencies[dll] = dllInfo;
        }

        private static bool IsRuntimeDll(string dll)
        {
            dll = dll.ToLower();

            // Check using compiled regular expressions
            if (CompiledRuntimePatterns.Any(regex => regex.IsMatch(dll)))
                return true;

            // Check specific runtime DLLs
            string[] specificRuntimeDlls =
            [
                // .NET Runtime
                "clr.dll", "coreclr.dll", "clrjit.dll", "mscorlib.dll",
                "mscorwks.dll", "mscorsvr.dll", "mscoree.dll",
                
                // Common Language Runtime
                "fusion.dll", "diasymreader.dll",
                
                // Other Runtime Components
                "hostfxr.dll", "hostpolicy.dll"
            ];

            return specificRuntimeDlls.Contains(dll);
        }

        private static bool IsSystemDll(string dll)
        {
            string[] systemDlls =
            [
                "kernel32.dll", "user32.dll", "gdi32.dll", "ntdll.dll", "advapi32.dll",
                "comctl32.dll", "comdlg32.dll", "shell32.dll", "shlwapi.dll", "ole32.dll",
                "oleaut32.dll", "rpcrt4.dll", "sechost.dll", "winspool.drv", "ws2_32.dll",
                "bcrypt.dll", "combase.dll", "win32u.dll", "kernelbase.dll"
            ];
            return systemDlls.Contains(dll.ToLower());
        }

        private static void PrintDependenciesByType()
        {
            var sb = new StringBuilder();

            void AppendDependencies(string title, IEnumerable<DllInfo> dependencies)
            {
                sb.AppendLine($"\n{title}:")
                  .AppendLine(new string('-', title.Length + 1));
                
                foreach (var dll in dependencies.OrderBy(x => x.Name))
                {
                    sb.AppendLine($"{dll.Name,-40} {dll.FullPath}");
                }
            }

            AppendDependencies("System Dependencies", SystemDependencies.Values);
            AppendDependencies("Runtime Dependencies", RuntimeDependencies.Values);
            AppendDependencies("Virtual Dependencies (API Sets)", VirtualDependencies.Values);
            AppendDependencies("Other Dependencies", OtherDependencies.Values);

            sb.AppendLine("\nSummary:")
              .AppendLine("--------")
              .AppendLine($"System Dependencies: {SystemDependencies.Count}")
              .AppendLine($"Runtime Dependencies: {RuntimeDependencies.Count}")
              .AppendLine($"Virtual Dependencies: {VirtualDependencies.Count}")
              .AppendLine($"Other Dependencies: {OtherDependencies.Count}")
              .AppendLine($"Total Dependencies: {SystemDependencies.Count + RuntimeDependencies.Count + VirtualDependencies.Count + OtherDependencies.Count}");

            Console.Write(sb.ToString());
        }

        private static void AnalyzeFile(string filePath, int depth)
        {
            if (!File.Exists(filePath) || !AnalyzedFiles.Add(filePath.ToLower()))
                return;

            try
            {
                var peFile = new PeFile(filePath);
                var imports = peFile.ImportedFunctions;
                if (imports == null || !imports.Any()) return;

                var dlls = imports.Select(f => f.DLL).Distinct();
                foreach (var dll in dlls)
                {
                    var dllPath = FindDll(dll, filePath);
                    if (dllPath == null) continue;

                    ClassifyDependency(dll, dllPath);
                    AnalyzeFile(dllPath, depth + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{new string(' ', depth * 2)}Error analyzing {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private static string? FindDll(string dllName, string importingModulePath)
        {
            // Check cache
            if (DllPathCache.TryGetValue(dllName, out var cachedPath))
                return cachedPath;

            var moduleDirectory = Path.GetDirectoryName(importingModulePath);
            if (moduleDirectory != null)
            {
                var fullPath = Path.Combine(moduleDirectory, dllName);
                if (File.Exists(fullPath))
                {
                    DllPathCache[dllName] = fullPath;
                    return fullPath;
                }
            }

            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar))
            {
                DllPathCache[dllName] = null;
                return null;
            }

            foreach (var path in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var fullPath = Path.Combine(path, dllName);
                    if (File.Exists(fullPath))
                    {
                        DllPathCache[dllName] = fullPath;
                        return fullPath;
                    }
                }
                catch
                {
                    // Skip invalid paths in PATH
                }
            }

            DllPathCache[dllName] = null;
            return null;
        }
    }
}
