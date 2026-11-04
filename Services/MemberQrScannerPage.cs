#if ANDROID || IOS || MACCATALYST
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace PIMP_MY_RIDE.Services;

internal sealed class MemberQrScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CameraBarcodeReaderView _camera;
    private int _finished;

    public Task<string?> ScanResult => _result.Task;

    public MemberQrScannerPage()
    {
        Title = "Gym check-in";
        BackgroundColor = Colors.Black;

        _camera = new CameraBarcodeReaderView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            CameraLocation = CameraLocation.Rear,
            IsDetecting = true,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false,
                TryHarder = true
            }
        };
        _camera.BarcodesDetected += OnBarcodesDetected;

        var closeButton = new Button
        {
            Text = "Close scanner",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#25332E"),
            CornerRadius = 14,
            Padding = new Thickness(20, 12),
            HorizontalOptions = LayoutOptions.Center
        };
        closeButton.Clicked += async (_, _) => await FinishAsync(null);

        var guide = new Border
        {
            Stroke = Color.FromArgb("#40534A"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#E818211D"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = new Thickness(20),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Scan gym QR code", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Point your camera at the check-in code provided at the gym.", FontSize = 14, TextColor = Color.FromArgb("#D5DDD8"), HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };

        var overlay = new Grid
        {
            Padding = new Thickness(24, 34),
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
        };
        overlay.Add(guide, 0, 0);
        var frame = new Border
        {
            WidthRequest = 250,
            HeightRequest = 250,
            Stroke = Color.FromArgb("#8DE0B1"),
            StrokeThickness = 3,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        overlay.Add(frame, 0, 1);
        overlay.Add(closeButton, 0, 2);

        var layout = new Grid();
        layout.Children.Add(_camera);
        layout.Children.Add(overlay);
        Content = layout;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = FinishAsync(null);
        return true;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value)) return;
        MainThread.BeginInvokeOnMainThread(async () => await FinishAsync(value));
    }

    private async Task FinishAsync(string? value)
    {
        if (Interlocked.Exchange(ref _finished, 1) != 0) return;
        _camera.IsDetecting = false;
        _result.TrySetResult(value);
        if (Navigation.ModalStack.LastOrDefault() == this)
            await Navigation.PopModalAsync();
    }
}
#endif
