namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Az összes MessageBox hívást központosítja, magyar UI szövegekkel.
/// Ezeket használd a MessageBox.Show közvetlen hívása helyett.
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
    /// API hibaablakot jelenít meg. A bejövő üzenet feltehetően már egy
    /// felhasználóbarát magyar szöveg az ApiExceptionMapper-ből — nincs
    /// hozzáadva általános wrapper.
    /// </summary>
    public static void ShowApiError(string message, string title = "API Hiba")
        => ShowError(message, title);

    /// <summary>Sikeres mentés ablak egy elmentett PDF után.</summary>
    public static void ShowPdfSaved(string path)
        => ShowInfo($"PDF sikeresen elmentve:\n{path}", "PDF Mentve");
}
