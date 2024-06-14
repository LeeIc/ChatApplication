using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;

namespace Chatting_Client
{
  [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
  public class MainActivity : MauiAppCompatActivity
  {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Handled by version check")]
    protected override void OnCreate(Bundle? savedInstanceState)
    {
      base.OnCreate(savedInstanceState);

      // Start the foreground service
      Intent intent = new Intent(this, typeof(MyForegroundService));
      if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
      {
        if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
        {
          RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 0);
        }
        StartForegroundService(intent);
      }
      else
      {
        StartService(intent);
      }
    }
  }
}
