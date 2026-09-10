using Windows.Storage;
using Windows.Storage.Pickers;

namespace Falah.Client.CashierWinUI.Platform;

/// <summary>
/// Picking a product photo off disk.
///
/// The picker is initialized with the app's own window handle. That is not optional here: this app
/// is unpackaged, and an unpackaged WinUI process gives FileOpenPicker no window to parent itself
/// to - without the handle the call throws instead of showing a dialog.
/// </summary>
public static class ImageFilePicker
{
    public static async Task<(string ContentType, byte[] Bytes)?> PickAsync()
    {
        var window = App.MainAppWindow;
        if (window is null) return null;

        var picker = new FileOpenPicker { ViewMode = PickerViewMode.Thumbnail, SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".webp" }) picker.FileTypeFilter.Add(extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        using var stream = await file.OpenStreamForReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();
        // Derived from the extension the user actually picked, not from the system's content-type
        // guess, which is empty for .webp on plenty of machines. The server re-checks the bytes
        // themselves anyway.
        var contentType = Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
        return (contentType, bytes);
    }
}
