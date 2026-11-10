namespace DentOrganizer.Services;

/// <summary>Native confirm/alert dialogs (work on all platforms, unlike JS confirm()).</summary>
public class DialogService
{
    private static Page? CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No") =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            CurrentPage is { } page && await page.DisplayAlertAsync(title, message, accept, cancel));

    public Task AlertAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (CurrentPage is { } page) await page.DisplayAlertAsync(title, message, "OK");
        });
}
