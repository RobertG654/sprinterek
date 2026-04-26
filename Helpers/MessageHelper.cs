namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Centralizes all MessageBox calls with Hungarian UI text.
/// Use these instead of calling MessageBox.Show directly.
/// </summary>
public static class MessageHelper
{
    public static void ShowError(string message, string title = "Hiba")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static void ShowInfo(string message, string title = "Információ")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void ShowWarning(string message, string title = "Figyelmeztetés")
        => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static DialogResult ShowQuestion(string message, string title = "Megerősítés")
        => MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

    /// <summary>
    /// Displays an API error dialog. The message is expected to already be a
    /// user-friendly Hungarian string from ApiExceptionMapper — no generic wrapper is added.
    /// </summary>
    public static void ShowApiError(string message, string title = "API Hiba")
        => ShowError(message, title);

    /// <summary>Displays a success dialog after a PDF is saved.</summary>
    public static void ShowPdfSaved(string path)
        => ShowInfo($"PDF sikeresen elmentve:\n{path}", "PDF Mentve");
}
