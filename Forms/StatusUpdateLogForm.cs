namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Read-only dialog that displays the full request/response trace produced
/// by <see cref="HotcakesWinFormsApp.Services.OrderStatusUpdateService"/>.
///
/// Shown automatically when an automatic status update fails so the user
/// can copy the trace into a bug report. Also reachable on success via the
/// "Részletek" (Details) button on the success message — useful for
/// diagnosing edge cases while we're still confirming the endpoint.
/// </summary>
public class StatusUpdateLogForm : Form
{
    public StatusUpdateLogForm(string title, string log)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 480);
        MinimumSize = new Size(560, 360);
        Font = new Font("Segoe UI", 9f);

        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9f),
            Dock = DockStyle.Fill,
            Text = log
        };

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8) };
        var btnCopy = new Button
        {
            Text = "Másolás vágólapra",
            Width = 160,
            Height = 28,
            Dock = DockStyle.Right
        };
        btnCopy.Click += (_, _) =>
        {
            try { Clipboard.SetText(log); }
            catch { /* clipboard may be momentarily unavailable — ignore */ }
        };

        var btnClose = new Button
        {
            Text = "Bezárás",
            Width = 100,
            Height = 28,
            Dock = DockStyle.Right,
            DialogResult = DialogResult.OK
        };

        bottom.Controls.Add(btnClose);
        bottom.Controls.Add(btnCopy);

        Controls.Add(box);
        Controls.Add(bottom);

        AcceptButton = btnClose;
    }
}
