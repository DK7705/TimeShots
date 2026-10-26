using TimeShots.Core.Integrity;
using TimeShots.Core.Models;
using TimeShots.Core.Security;

namespace TimeShots.Cli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "verify" => HandleVerify(args.Skip(1).ToArray()),
                "keygen" => HandleKeygen(args.Skip(1).ToArray()),
                "version" or "-v" or "--version" => PrintVersion(),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            PrintError($"Execution error: {ex.Message}");
            return 1;
        }
    }

    private static int PrintVersion()
    {
        Console.WriteLine("TimeShots CLI v1.0.0");
        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"TimeShots — Tamper-Evident Screenshot Verification Tool

Usage:
  timeshots verify <image.png | metadata.json> [--key <key_file_or_hex>]
  timeshots verify --chain <captures_directory> [--key <key_file_or_hex>]
  timeshots keygen [--out <path>]
  timeshots --help

Commands:
  verify              Verify the cryptographic hash, linkage, and signature of a capture or chain
  keygen              Generate a secure 256-bit HMAC key
  --help, -h          Show command line help
");
    }

    private static int HandleVerify(string[] args)
    {
        if (args.Length == 0)
        {
            PrintError("Missing target path to verify. Usage: timeshots verify <file_or_dir>");
            return 1;
        }

        bool isChain = false;
        string? targetPath = null;
        string? keyInput = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--chain" or "-c")
            {
                isChain = true;
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    targetPath = args[++i];
                }
            }
            else if (args[i] is "--key" or "-k")
            {
                if (i + 1 < args.Length)
                {
                    keyInput = args[++i];
                }
            }
            else if (targetPath == null && !args[i].StartsWith('-'))
            {
                targetPath = args[i];
            }
        }

        byte[]? secretKey = ResolveKey(keyInput);

        if (isChain || (targetPath != null && Directory.Exists(targetPath)))
        {
            return VerifyDirectoryChain(targetPath ?? ".", secretKey);
        }

        if (targetPath == null || !File.Exists(targetPath))
        {
            PrintError($"File not found: {targetPath}");
            return 1;
        }

        return VerifySingleCapture(targetPath, secretKey);
    }

    private static int VerifySingleCapture(string filePath, byte[]? secretKey)
    {
        string imagePath;
        string metadataPath;

        if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            metadataPath = filePath;
            imagePath = Path.ChangeExtension(filePath, ".png");
        }
        else
        {
            imagePath = filePath;
            metadataPath = Path.ChangeExtension(filePath, ".json");
        }

        if (!File.Exists(imagePath))
        {
            PrintError($"Associated image file missing: {imagePath}");
            return 1;
        }

        if (!File.Exists(metadataPath))
        {
            PrintError($"Associated metadata sidecar missing: {metadataPath}");
            return 1;
        }

        Console.WriteLine($"\n🔍 Verifying Capture: {Path.GetFileName(imagePath)}");
        Console.WriteLine($"   Sidecar: {Path.GetFileName(metadataPath)}");

        string jsonContent = File.ReadAllText(metadataPath);
        var metadata = CaptureMetadata.FromJson(jsonContent);
        if (metadata == null)
        {
            PrintError("Failed to parse JSON metadata file.");
            return 1;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        var result = Verifier.VerifyCapture(imageBytes, metadata, secretKey);

        PrintResults(result);
        return result.IsValid ? 0 : 1;
    }

    private static int VerifyDirectoryChain(string dirPath, byte[]? secretKey)
    {
        Console.WriteLine($"\n🔍 Verifying Capture Chain in: {dirPath}");

        var jsonFiles = Directory.GetFiles(dirPath, "*.json", SearchOption.AllDirectories);
        if (jsonFiles.Length == 0)
        {
            PrintError($"No .json metadata sidecars found in directory: {dirPath}");
            return 1;
        }

        var list = new List<CaptureMetadata>();
        foreach (var file in jsonFiles)
        {
            try
            {
                var meta = CaptureMetadata.FromJson(File.ReadAllText(file));
                if (meta != null && !string.IsNullOrEmpty(meta.Integrity.ChainHash))
                {
                    list.Add(meta);
                }
            }
            catch
            {
                // Ignore non-metadata JSON files
            }
        }

        if (list.Count == 0)
        {
            PrintError("No valid TimeShots metadata records found in directory.");
            return 1;
        }

        Console.WriteLine($"   Found {list.Count} captures to verify.");

        // Verify each physical image file against its sidecar hash
        int imageCheckFailures = 0;
        foreach (var file in jsonFiles)
        {
            string imgPath = Path.ChangeExtension(file, ".png");
            if (!File.Exists(imgPath))
            {
                imageCheckFailures++;
                continue;
            }

            try
            {
                var meta = CaptureMetadata.FromJson(File.ReadAllText(file));
                if (meta != null && !string.IsNullOrEmpty(meta.Integrity.ImageHashSha256))
                {
                    string computed = HashChainEngine.ComputeImageHash(File.ReadAllBytes(imgPath));
                    if (!string.Equals(computed, meta.Integrity.ImageHashSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        imageCheckFailures++;
                    }
                }
            }
            catch
            {
                imageCheckFailures++;
            }
        }

        var result = Verifier.VerifyChain(list, secretKey);

        result.Checks.Insert(0, new VerificationCheck
        {
            Name = "Image Pixel Integrity",
            Passed = imageCheckFailures == 0,
            Message = imageCheckFailures == 0
                ? $"All {list.Count} image files exist and match their sidecar SHA-256 hashes."
                : $"{imageCheckFailures} image files are missing or modified!"
        });
        result.IsValid = result.IsValid && (imageCheckFailures == 0);

        PrintResults(result);
        return result.IsValid ? 0 : 1;
    }

    private static void PrintResults(VerificationResult result)
    {
        Console.WriteLine("\n---------------------------------------------------------");
        foreach (var check in result.Checks)
        {
            if (check.Passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{check.Name}: {check.Message}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{check.Name}: {check.Message}");
            }
        }
        Console.WriteLine("---------------------------------------------------------");

        if (result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ VERIFICATION SUCCESSFUL — Integrity verified.\n");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ VERIFICATION FAILED — Tampering or linkage break detected!\n");
        }
        Console.ResetColor();
    }

    private static int HandleKeygen(string[] args)
    {
        string? outPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--out" or "-o" && i + 1 < args.Length)
            {
                outPath = args[++i];
            }
        }

        var key = KeyManager.GenerateKey();
        var hex = KeyManager.KeyToHex(key);

        if (!string.IsNullOrEmpty(outPath))
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(outPath, hex);
            Console.WriteLine($"Key saved to: {outPath}");
        }
        else
        {
            Console.WriteLine($"Generated HMAC Key (256-bit):");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(hex);
            Console.ResetColor();
        }
        return 0;
    }

    private static byte[]? ResolveKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (File.Exists(input))
        {
            return KeyManager.GetOrCreateKey(input);
        }

        if (input.Trim().Length == KeyManager.KeySizeInBytes * 2)
        {
            return KeyManager.HexToKey(input);
        }

        return null;
    }

    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    private static int HandleUnknownCommand(string cmd)
    {
        PrintError($"Unknown command '{cmd}'. Run 'timeshots --help' for usage.");
        return 1;
    }
}
