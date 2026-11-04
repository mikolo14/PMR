namespace PIMP_MY_RIDE
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if ANDROID
            blazorWebView.HandlerChanged += (_, _) => DisableAndroidWebViewAutofill();
            DisableAndroidWebViewAutofill();
#endif
        }

#if ANDROID
        private void DisableAndroidWebViewAutofill()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26) &&
                blazorWebView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                nativeWebView.ImportantForAutofill = Android.Views.ImportantForAutofill.NoExcludeDescendants;
            }
        }
#endif
    }
}
