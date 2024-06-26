﻿using Chatting_Client.Views;
using Plugin.LocalNotification;
namespace Chatting_Client
{
  public static class MauiProgram
  {
    public static MauiApp CreateMauiApp()
    {
      var builder = MauiApp.CreateBuilder();
      builder
        .UseMauiApp<App>()
        .RegisterViewModels()
        .UseLocalNotification()
        .ConfigureFonts(fonts =>
        {
          fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
          fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

#if DEBUG
     // builder.Logging.AddDebug();
#endif

      return builder.Build();
    }

    private static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
      mauiAppBuilder.Services.AddSingleton<MainViewModel>();

      return mauiAppBuilder;
    }
  }
}
