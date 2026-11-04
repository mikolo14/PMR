using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace PIMP_MY_RIDE
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait, WindowSoftInputMode = SoftInput.AdjustPan, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // The Android autofill overlay was covering focused fields and dismissing the keyboard.
            if (OperatingSystem.IsAndroidVersionAtLeast(26) && Window?.DecorView is { } decorView)
                decorView.ImportantForAutofill = ImportantForAutofill.NoExcludeDescendants;
        }
    }
}
