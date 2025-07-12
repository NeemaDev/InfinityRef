using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Navigation;
using InfinityRef.Core.Services;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.Services;
using InfinityRef.UI.ViewModels;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using IFilePicker = InfinityRef.Core.Interfaces.IFilePicker;

#if WINDOWS
using InfinityRef.UI.Platforms.Windows;
#endif

namespace InfinityRef
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // HttpClient for UriImageSources.
            builder.Services.AddHttpClient("ImageClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityRef/1.0");
            });

            // Register ViewModels.
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddTransient<CanvasViewModel>();

            // Register Services.
            builder.Services.AddSingleton<CanvasStackHandler>();
            builder.Services.AddSingleton<CanvasInteractionService>();
            builder.Services.AddSingleton<IFilePicker, FilePickerService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<ISaveLoadService, SaveAndLoadService>();
#if WINDOWS
            builder.Services.AddSingleton<IDragDropService, WindowsDragDropService>();
#else
            builder.Services.AddSingleton<IDragDropService, NoOperationDragDropService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
