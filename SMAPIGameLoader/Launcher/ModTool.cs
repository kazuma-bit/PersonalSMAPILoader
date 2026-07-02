using System;
using System.Collections.Generic;
using System.IO;

namespace SMAPIGameLoader.Launcher;

internal static class ModTool
{
    const string ModsDirName = "Mods";
    public static string ModsDir { get; } = Path.Combine(FileTool.ExternalFilesDir, ModsDirName);

    public static string ManifiestFileName = "manifest.json";
    public static void FindManifestFile(string rootDirPath, List<string> manifestFiles)
    {
        try
        {
            if (!Directory.Exists(rootDirPath)) return;

            var manifestFilePath = Path.Combine(rootDirPath, ManifiestFileName);
            if (rootDirPath != ModsDir)
            {
                if (Path.Exists(manifestFilePath))
                {
                    manifestFiles.Add(manifestFilePath);
                    return;
                }
            }

            var folders = Directory.GetDirectories(rootDirPath);
            foreach (var folderPath in folders)
            {
                FindManifestFile(folderPath, manifestFiles);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public static string SetModEnabled(string modFolderPath, bool enabled)
    {
        var parentDir = Path.GetDirectoryName(modFolderPath);
        var folderName = Path.GetFileName(modFolderPath);
        var isCurrentlyEnabled = !folderName.StartsWith(".");

        if (isCurrentlyEnabled == enabled)
            return modFolderPath;

        var newFolderName = enabled ? folderName.Substring(1) : "." + folderName;
        var newFolderPath = Path.Combine(parentDir, newFolderName);

        Directory.Move(modFolderPath, newFolderPath);
        return newFolderPath;
    }
}
