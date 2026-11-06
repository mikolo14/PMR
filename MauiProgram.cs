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
            builder.Services.AddSingleton<PIMP_MY_RIDE.Services.SupabaseGymService>();

            // Use the platform-native HTTP handler so Android TLS/network works correctly
            builder.Services.AddSingleton<HttpClient>(_ =>
            {
#if ANDROID
                var handler = new Xamarin.Android.Net.AndroidMessageHandler
                {
                    // Allow TLS 1.2+ on older Android versions
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
#else
                return new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
#endif
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
