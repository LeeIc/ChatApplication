using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Chatting_Client
{
  [Service(Name = "com.mycompany.myapp.MyForegroundService", ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeRemoteMessaging)]
  public class MyForegroundService : Service
  {
    private const int ServiceRunningNotificationId = 10000;
    private const string ChannelId = "my_foreground_service_channel";

    public override void OnCreate()
    {
      base.OnCreate();
      CreateNotificationChannel();
    }

    public override IBinder? OnBind(Intent? intent)
    {
      return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
      var notification = new NotificationCompat.Builder(this, ChannelId)
          .SetCategory(Notification.CategoryMessage)
          .SetContentTitle("Chatting Client")
          .SetContentText("Running in the background")
          .SetSmallIcon(Resource.Drawable.btn_radio_on_mtrl) // Replace with your app's icon
          .SetOngoing(true)
          .Build();
      if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
      {
        StartForeground(ServiceRunningNotificationId, notification, Android.Content.PM.ForegroundService.TypeRemoteMessaging);
      }
      else 
      {
        StartForeground(ServiceRunningNotificationId, notification);
      }
      // Perform your background tasks here

      return StartCommandResult.Sticky;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Handled by version check")]
    private void CreateNotificationChannel()
    {
      if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
      {
        var channel = new NotificationChannel(ChannelId, "Foreground Service Channel", NotificationImportance.Default)
        {
          Description = "Channel for foreground service"
        };
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
      }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "StopForeground is always available on Android")]
    public override void OnDestroy()
    {
      if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
      {
        StopForeground(true);
      }
      base.OnDestroy();
    }
  }
}
