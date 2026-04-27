using HotcakesWinFormsApp.UI;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Read-only dialog that displays the full request/response trace produced
/// by <see cref="HotcakesWinFormsApp.Services.OrderStatusUpdateService"/>.
///
/// Shown automatically when an automatic status update fails so the user
/// can copy the trace into a bug report. Also reachable on success via the
/// "Részletek" (Details) button on the success message — useful for
/// diagnosing edge cases while we're still confirming the endpoint.
///
/// Visuals match the Pawpromise palette used elsewhere in the app.
/// </summary>
public class StatusUpdateLogForm : Form
{
    public StatusUpdateLogForm(string title, string log)
    {
        Text = title;
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
            catch { /* clipboard may be momentarily unavailable — ignore */ }
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

        // Order matters for Dock.Right: last-added is processed first and ends
        // up RIGHTMOST. We want [Másolás] [Bezárás] left-to-right, so add Copy
        // first and Close last.
        bottom.Controls.Add(btnCopy);
        bottom.Controls.Add(spacer);
        bottom.Controls.Add(btnClose);

        Controls.Add(bottom);

        AcceptButton = btnClose;
    }
}
