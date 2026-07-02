using Android;
using Android.Content.PM;
using Android.OS;

using AndroidX.Core.App;
using AndroidX.Core.Content;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class FilePickerTool
{
    public static FilePickerFileType FileTypeZip = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "application/zip" } },
    });

    static bool HasStoragePermission()
    {
        var activity = Launcher.LauncherActivity.Instance;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
        {
            if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.ReadExternalStorage) != Permission.Granted
                || ContextCompat.CheckSelfPermission(activity, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ToastNotifyTool.Notify("Please Click Allow File Access Permission");
                ActivityCompat.RequestPermissions(activity,
                    [Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage],
                    1000);
                return false;
            }
        }
        return true;
    }

    public static async Task<FileResult> PickZipFile(string title = null)
    {
        if (title == null)
            title = "Please select zip file";

        if (!HasStoragePermission())
            return null;

        var options = new PickOptions
        {
            PickerTitle = title,
            FileTypes = FileTypeZip,
        };
        return await FilePicker.PickAsync(options);
    }

    public static async Task<List<FileResult>> PickMultipleZipFiles(string title = null)
    {
        if (title == null)
            title = "Please select zip file(s)";

        if (!HasStoragePermission())
            return null;

        var options = new PickOptions
        {
            PickerTitle = title,
            FileTypes = FileTypeZip,
        };
        var results = await FilePicker.PickMultipleAsync(options);
        return results?.ToList();
    }
}