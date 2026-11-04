using ZXing.Net.Maui;

namespace PIMP_MY_RIDE.Services;

public sealed class MemberQrScanner
{
    public async Task<string?> ScanAsync()
    {
#if ANDROID || IOS || MACCATALYST
        if (!BarcodeScanning.IsSupported)
            throw new NotSupportedException("This device does not have a supported camera.");

        var permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
            throw new UnauthorizedAccessException("Camera access is required to scan the gym QR code.");

        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (root is null)
            throw new InvalidOperationException("The app page is not ready to open the scanner.");

        var scannerPage = new MemberQrScannerPage();
        await root.Navigation.PushModalAsync(scannerPage);
        return await scannerPage.ScanResult;
#else
        throw new NotSupportedException("Live camera scanning is available in the Android and iPhone app. Open PMR on your phone to scan the gym QR code.");
#endif
    }
}
