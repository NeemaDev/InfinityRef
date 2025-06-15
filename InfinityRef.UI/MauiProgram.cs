using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Services;
using InfinityRef.UI.ViewModels;
using Microsoft.Extensions.Logging;
using IFilePicker = InfinityRef.Core.Interfaces.IFilePicker;

namespace InfinityRef
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register ViewModels.
            builder.Services.AddSingleton<MainViewModel>();

            // Register Services.
            builder.Services.AddSingleton<IFilePicker, FilePickerService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<ISaveLoadService, SaveAndLoadService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
