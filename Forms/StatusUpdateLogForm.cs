using HotcakesWinFormsApp.UI;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Csak olvasható párbeszédablak, ami a teljes request / response trace-t
/// jeleníti meg, amit a
/// <see cref="HotcakesWinFormsApp.Services.OrderStatusUpdateService"/> termel.
///
/// Megjegyzés: a jelenlegi UI a felhasználó kérésére már nem jeleníti meg ezt
/// az ablakot — sem sikeres, sem sikertelen állapot-frissítés után.
/// Az osztály a fájlban marad, hogy szükség esetén egy fejlesztő egyetlen
/// soros hívással tudjon manuálisan diagnosztizálni
/// (pl. egy helyi próbából a kódból).
///
/// Vizuálisan az alkalmazás többi részében használt Pawpromise palettához igazodik.
/// </summary>
public class StatusUpdateLogForm : Form
{
    public StatusUpdateLogForm(string title, string log)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 540);
        MinimumSize = new Size(620, 420);
        Font = Theme.BodyFont;
        BackColor = Theme.PageBg;

        var header = new AppHeaderBar
        {
            Title = "PAWPROMISE NAPLÓ",
            Subtitle = title
        };
        Controls.Add(header);

        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PageBg,
            Padding = new Padding(20, 16, 20, 16)
        };

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(2)
        };

        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Cascadia Mono", 9f),
            ForeColor = Theme.TextPrimary,
            BackColor = Theme.CardBg,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Text = log
        };
        card.Controls.Add(box);
        bodyHost.Controls.Add(card);
        Controls.Add(bodyHost);

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Theme.PageBg,
            Padding = new Padding(20, 10, 20, 14)
        };
        var btnCopy = new ModernButton
        {
            Text = "Másolás vágólapra",
            Style = ModernButton.ButtonStyle.Ghost,
            Width = 180,
            Height = 36,
            Dock = DockStyle.Right
        };
        btnCopy.Click += (_, _) =>
        {
            try { Clipboard.SetText(log); }
            catch { /* a vágólap pillanatnyilag nem elérhető — figyelmen kívül hagyjuk */ }
        };

        var spacer = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = Color.Transparent };

        var btnClose = new ModernButton
        {
            Text = "Bezárás",
            Style = ModernButton.ButtonStyle.Primary,
            Width = 130,
            Height = 36,
            Dock = DockStyle.Right,
            DialogResult = DialogResult.OK
        };

        // A Dock.Right-nál fontos a sorrend: az utoljára hozzáadott vezérlő
        // dolgozódik fel ELSŐKÉNT, és így a LEGJOBBOLDALIBB lesz. Mi balról
        // jobbra a [Másolás] [Bezárás] sorrendet szeretnénk, ezért a Másolást
        // adjuk hozzá először, a Bezárást utoljára.
        bottom.Controls.Add(btnCopy);
        bottom.Controls.Add(spacer);
        bottom.Controls.Add(btnClose);

        Controls.Add(bottom);

        AcceptButton = btnClose;
    }
}
