using System;
using System.IO;
using System.Text.Json;
using WindowsAudioWrapper;
using WindowsAudioWrapper.Models;

namespace WindowsAudioWrapper.SampleApp;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        string command = args[0].ToLowerInvariant();

        try
        {
            using var controller = new WindowsAudioController();

            switch (command)
            {
                case "save":
                    HandleSave(controller, args);
                    break;

                case "load":
                    HandleLoad(controller, args);
                    break;

                case "equal":
                    HandleEqual(controller, args);
                    break;

                case "print":
                    HandlePrint(controller, args);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Unknown command '{args[0]}'.");
                    Console.ResetColor();
                    PrintUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An unhandled error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void HandleSave(IWindowsAudioController controller, string[] args)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing file path parameter. Usage: save <filepath>");
            Console.ResetColor();
            return;
        }

        string filePath = args[1];
        Console.WriteLine("Capturing current system audio profile...");
        
        AudioProfile currentProfile = controller.GetCurrentProfile();

        string json = JsonSerializer.Serialize(currentProfile, JsonOptions);
        File.WriteAllText(filePath, json);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Success: Audio profile successfully saved to '{filePath}'.");
        Console.ResetColor();
    }

    private static void HandleLoad(IWindowsAudioController controller, string[] args)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing file path parameter. Usage: load <filepath>");
            Console.ResetColor();
            return;
        }

        string filePath = args[1];
        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: File '{filePath}' could not be found.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"Loading audio profile from '{filePath}'...");
        string json = File.ReadAllText(filePath);
        AudioProfile? profile = JsonSerializer.Deserialize<AudioProfile>(json);

        if (profile == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Failed to deserialize audio profile config.");
            Console.ResetColor();
            return;
        }

        // CRITICAL: Hydrate hidden execution switches so the engine knows what settings to apply
        profile.EnableAllFeatures();

        Console.WriteLine("Applying profile settings to Windows audio endpoints...");
        AudioProfileApplyResult result = controller.ApplyProfile(profile);

        if (result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success: Audio profile applied exactly to current hardware.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Failed to apply audio profile. Engine execution messages:");
            foreach (var msg in result.Messages)
            {
                Console.WriteLine($" - [{msg.Severity}] {msg.Code}: {msg.Message}");
            }
            Console.ResetColor();
        }
    }

    private static void HandleEqual(IWindowsAudioController controller, string[] args)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing file path parameters. Usage: equal <file1> [file2]");
            Console.ResetColor();
            return;
        }

        AudioProfile profileA;
        AudioProfile profileB;

        if (args.Length == 2)
        {
            // Case 1: equal myprofile.cfg (Compare current system hardware vs file)
            string filePath = args[1];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{filePath}' could not be found.");
                return;
            }

            profileA = controller.GetCurrentProfile();
            
            string jsonContent = File.ReadAllText(filePath);
            var loadedProfile = JsonSerializer.Deserialize<AudioProfile>(jsonContent) ?? new AudioProfile();
            loadedProfile.EnsureDefaults(); // Normalize zero-null states for an accurate structural match
            profileB = loadedProfile;

            Console.WriteLine($"Comparing [Current System Settings] against stored file [{filePath}]...");
        }
        else
        {
            // Case 2: equal profile1.cfg profile2.cfg (Compare file vs file)
            string file1 = args[1];
            string file2 = args[2];

            if (!File.Exists(file1) || !File.Exists(file2))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: One or both configuration files could not be found.");
                Console.ResetColor();
                return;
            }

            var p1 = JsonSerializer.Deserialize<AudioProfile>(File.ReadAllText(file1)) ?? new AudioProfile();
            var p2 = JsonSerializer.Deserialize<AudioProfile>(File.ReadAllText(file2)) ?? new AudioProfile();
            
            p1.EnsureDefaults();
            p2.EnsureDefaults();

            profileA = p1;
            profileB = p2;

            Console.WriteLine($"Comparing stored file [{file1}] against stored file [{file2}]...");
        }

        // Now you can cleanly compare the two objects natively using the overloaded operator!
        if (profileA == profileB)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("EQUAL");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("NOT EQUAL");
            Console.ResetColor();
        }

    }

    private static void HandlePrint(IWindowsAudioController controller, string[] args)
    {
        AudioProfile profile;

        if (args.Length == 1)
        {
            // Case 1: print (Print live system settings)
            Console.WriteLine("=== Current Live Windows Audio Profile Configuration ===");
            profile = controller.GetCurrentProfile();
        }
        else
        {
            // Case 2: print myprofile.cfg (Print file contents)
            string filePath = args[1];
            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File '{filePath}' could not be found.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"=== Stored Audio Profile Configuration: {filePath} ===");
            profile = JsonSerializer.Deserialize<AudioProfile>(File.ReadAllText(filePath)) ?? new AudioProfile();
            profile.EnsureDefaults();
        }

        string displayJson = JsonSerializer.Serialize(profile, JsonOptions);
        Console.WriteLine(displayJson);
        Console.WriteLine("======================================================");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("\nWindowsAudioWrapper Sample Utility Command Reference:");
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
        Console.WriteLine("  save <file.cfg>               | Captures current live system audio variables to a file.");
        Console.WriteLine("  load <file.cfg>               | Loads an audio configuration file and maps it to hardware.");
        Console.WriteLine("  equal <file.cfg>              | Micro-evaluates current live settings against a file state.");
        Console.WriteLine("  equal <file1.cfg> <file2.cfg> | Evaluates whether two standalone target configurations match.");
        Console.WriteLine("  print <file.cfg>              | Displays the structured data blocks stored inside a file.");
        Console.WriteLine("  print                         | Displays the exact current live hardware engine configuration.");
        Console.WriteLine("-------------------------------------------------------------------------------------------------\n");
    }
}