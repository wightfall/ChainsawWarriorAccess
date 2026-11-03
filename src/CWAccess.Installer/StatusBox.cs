using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CWAccess.Installer
{
    /// <summary>
    /// A read-only text box of several lines (the setup's events, the What's new of an update). A screen reader reads it
    /// line by line with the arrow keys, and the cursor always stays on the first line, also when lines are added.
    /// </summary>
    public sealed class StatusBox : TextBox
    {
        private readonly List<string> lines = new List<string>();

        public StatusBox()
        {
            Multiline = true;
            ReadOnly = true;
            WordWrap = true;
            ScrollBars = ScrollBars.Vertical;
            TabStop = true;
            // Read-only boxes are grey by default; this one is meant to be read.
            BackColor = SystemColors.Window;
        }

        /// <summary>The lines shown, oldest first.</summary>
        public IList<string> Entries => lines.AsReadOnly();

        /// <summary>Adds a line at the end; the cursor stays on the first line.</summary>
        public void AppendLine(string line)
        {
            lines.Add(line ?? string.Empty);
            Show(lines);
        }

        /// <summary>Replaces all lines; the cursor goes to the first line.</summary>
        public void SetLines(IEnumerable<string> newLines)
        {
            lines.Clear();
            foreach (string l in newLines) lines.Add(l ?? string.Empty);
            Show(lines);
        }

        /// <summary>The text for the lines: one per row, Windows line breaks.</summary>
        public static string Join(IEnumerable<string> rows)
        {
            return string.Join("\r\n", new List<string>(rows).ToArray());
        }

        /// <summary>Puts the cursor on the first line and scrolls there.</summary>
        public void ToTop()
        {
            SelectionStart = 0;
            SelectionLength = 0;
            if (IsHandleCreated) ScrollToCaret();
        }

        private void Show(IEnumerable<string> rows)
        {
            Text = Join(rows);
            ToTop();
        }
    }
}
