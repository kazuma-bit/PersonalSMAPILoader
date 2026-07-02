using Newtonsoft.Json.Linq;

using SMAPIGameLoader.Tool;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SMAPIGameLoader.Launcher;

internal static class ModInstaller
{
    public static string ModDir = Path.Combine(FileTool.ExternalFilesDir, "Mods");
    public static Version GetMinGameVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumGameVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }
    public static Version GetMinSMAPIVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumApiVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }
    public static bool AssertModISupport(JObject manifest)
    {
        if (!SMAPIInstaller.IsInstalled)
        {
            ToastNotifyTool.Notify("Can't check mod, please install SMAPI first!!");
            return false;
        }

        var minGameVersion = GetMinGameVersion(manifest);
        if (minGameVersion != null && minGameVersion < new Version(1, 6, 0))
        {
            ToastNotifyTool.Notify("Not support for game version 1.6");
            return false;
        }

        var minSMAPIVersion = GetMinSMAPIVersion(manifest);
        if (minSMAPIVersion != null & minSMAPIVersion < new Version(4, 0, 0))
        {
            ToastNotifyTool.Notify("Not support for game version 1.6");
            return false;
        }

        bool isContentPack = manifest.ContainsKey("ContentPackFor");
        return true;
    }

    public static void InstallModPackZip(string zipFilePath, ZipArchive zip, bool showDialog = true)
    {
        ExtractModZipFile(zipFilePath, zip, ModTool.ModsDir);

        var entries = zip.Entries;
        var manifestEntires = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
        var logBuilder = new StringBuilder();
        var fileInfo = new FileInfo(zipFilePath);
        logBuilder.AppendLine("Mod zip: " + fileInfo.Name);
        logBuilder.AppendLine("");
        logBuilder.AppendLine("List mods: " + manifestEntires.Length);
        for (int i = 0; i < manifestEntires.Length; i++)
        {
            var manifestEntry = manifestEntires[i];
            var modDir = manifestEntry.FullName.Replace($"/{ModTool.ManifiestFileName}", "");
            var dirInfo = new DirectoryInfo(modDir);
            logBuilder.AppendLine($"[{i + 1}]: {dirInfo.Name}");
        }

        if (showDialog)
            DialogTool.Show("Installed Mod Pack", logBuilder.ToString());
    }

    public static async void OnClickInstallMod(Action OnInstalledCallback = null)
    {
        try
        {
            var pickFiles = await FilePickerTool.PickMultipleZipFiles();
            if (pickFiles == null || pickFiles.Count == 0)
                return;

            // single file - keep the original detailed per-mod dialog
            if (pickFiles.Count == 1)
            {
                var installedName = InstallModFromFile(pickFiles[0].FullPath, pickFiles[0].FileName);
                if (installedName == null)
                    return;

                OnInstalledCallback?.Invoke();
                FileTool.ClearCache();
                return;
            }

            // multiple files - install each, show one summary dialog at the end
            var installedNames = new List<string>();
            var failed = new List<string>();

            foreach (var pickFile in pickFiles)
            {
                try
                {
                    var name = InstallModFromFile(pickFile.FullPath, pickFile.FileName, showDialog: false);
                    if (name != null)
                        installedNames.Add(name);
                    else
                        failed.Add(pickFile.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    failed.Add(pickFile.FileName);
                }
            }

            var summary = new StringBuilder();
            summary.AppendLine($"Installed: {installedNames.Count}/{pickFiles.Count}");
            foreach (var n in installedNames)
                summary.AppendLine("✔ " + n);

            if (failed.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine($"Failed: {failed.Count}");
                foreach (var n in failed)
                    summary.AppendLine("✘ " + n);
            }

            DialogTool.Show("Installed Mods", summary.ToString());

            OnInstalledCallback?.Invoke();
            FileTool.ClearCache();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    // shared logic for installing one zip, used by both the single and batch paths
    static string InstallModFromFile(string fullPath, string fileName, bool showDialog = true)
    {
        using var zip = ZipFile.OpenRead(fullPath);
        var entries = zip.Entries;
        var manifestEntires = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
        if (manifestEntires.Length == 0)
        {
            ToastNotifyTool.Notify($"{fileName}: manifest.json not found");
            return null;
        }

        bool isModPack = manifestEntires.Length != 1;
        if (isModPack)
        {
            InstallModPackZip(fullPath, zip, showDialog);
            return fileName;
        }

        var manifestText = ReadManifest(manifestEntires[0]);
        var manifestJson = JObject.Parse(manifestText);
        string modName = manifestJson["Name"].ToString();

        ExtractModZipFile(fileName, zip, Path.Combine(ModDir));

        if (showDialog)
        {
            var modVersion = manifestJson["Version"].ToString();
            var author = manifestJson["Author"].ToString();
            var modLogBuilder = new StringBuilder();
            modLogBuilder.AppendLine($"Name: {modName}");
            modLogBuilder.AppendLine($"Version: {modVersion}");
            modLogBuilder.AppendLine($"Author: {author}");

            var minGameVersion = GetMinGameVersion(manifestJson);
            if (minGameVersion != null)
                modLogBuilder.AppendLine($"Minimum Game Version: " + minGameVersion);

            var minSMAPIVersion = GetMinSMAPIVersion(manifestJson);
            if (minSMAPIVersion != null)
                modLogBuilder.AppendLine($"Minimum SMAPI Version: " + minSMAPIVersion);

            DialogTool.Show("Installed Mod", modLogBuilder.ToString());
        }

        return modName;
    }

    public static void ExtractModZipFile(string zipFilePath, ZipArchive zip, string outputDir)
    {
        var fileNameNoExtens = new FileInfo(zipFilePath).Name.Replace(".zip", "");
        var checkFileExist = Path.Combine(outputDir, fileNameNoExtens);
        if (File.Exists(checkFileExist))
            File.Delete(checkFileExist);

        zip.ExtractToDirectory(outputDir, true);
    }
    public static string ReadManifest(ZipArchiveEntry entry)
    {
        string result;
        using (StreamReader reader = new StreamReader(entry.Open()))
        {
            result = reader.ReadToEnd();
        }
        return result;
    }
    internal static bool TryDeleteMod(string folderPath, bool cleaupParentFolder)
    {
        try
        {
            if (!Directory.Exists(folderPath)) return false;

            Directory.Delete(folderPath, true);

            if (cleaupParentFolder)
            {
                var parentDir = Directory.GetParent(folderPath).FullName;
                if (parentDir != ModDir)
                {
                    var dirs = Directory.GetDirectories(parentDir);
                    if (dirs.Length == 0)
                        Directory.Delete(parentDir, true);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
            return true;
        }
    }
}