using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace PIMP_MY_RIDE
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<PIMP_MY_RIDE.Services.MemberQrScanner>();
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<PIMP_MY_RIDE.Services.SupabaseGymService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
