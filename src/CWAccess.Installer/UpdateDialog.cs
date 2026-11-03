using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CWAccess.Installer
{
    /// <summary>
    /// "Update available": the question, then what is new as a list (one line per item, focused first so a screen
    /// reader reads it line by line with the arrow keys), then Yes and No. Enter answers Yes, Escape No.
    /// </summary>
    public sealed class UpdateDialog : Form
    {
        private readonly Label question = new Label();
        private readonly Label notesLabel = new Label();
        private readonly StatusBox notes = new StatusBox();
        private readonly Button yes = new Button();
        private readonly Button no = new Button();

        public UpdateDialog(string headline, string releaseNotes)
        {
            Font = new Font("Segoe UI", 10f);
            Text = SetupText.T("upd_title", "Update available");
            ClientSize = new Size(640, 460);
            MinimumSize = new Size(480, 360);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;

            question.Text = headline + " " + SetupText.T("upd_question", "Download and install it now?");
            question.SetBounds(12, 12, 616, 46);
            question.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            notesLabel.Text = SetupText.T("upd_notes_label", "&What's new:");
            notesLabel.AutoSize = true;
            notesLabel.Location = new Point(12, 64);

            notes.SetBounds(12, 88, 616, 300);
            notes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            notes.AccessibleName = SetupText.T("upd_notes", "What's new");
            List<string> lines = ReleaseNotes.Lines(releaseNotes);
            if (lines.Count == 0) lines.Add(SetupText.T("upd_no_notes", "This release has no notes."));
            notes.SetLines(lines);

            yes.Text = SetupText.T("yes", "&Yes");
            yes.DialogResult = DialogResult.Yes;
            yes.SetBounds(12, 404, 150, 40);
            yes.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            no.Text = SetupText.T("no", "&No");
            no.DialogResult = DialogResult.No;
            no.SetBounds(172, 404, 150, 40);
            no.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            question.TabIndex = 0;
            notesLabel.TabIndex = 1;
            notes.TabIndex = 2;
            yes.TabIndex = 3;
            no.TabIndex = 4;
            Controls.AddRange(new Control[] { question, notesLabel, notes, yes, no });
            AcceptButton = yes;
            CancelButton = no;
            AccessibleDescription = question.Text;
            Shown += (s, e) =>
            {
                notes.Focus();
                notes.ToTop();
            };
        }

        /// <summary>Shows the question; true when the player chose Yes.</summary>
        public static bool Ask(IWin32Window owner, string headline, string releaseNotes)
        {
            using (var d = new UpdateDialog(headline, releaseNotes)) return d.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
