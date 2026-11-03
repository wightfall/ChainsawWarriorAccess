using System.Linq;
using System.Windows.Forms;
using CWAccess.Installer;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>The setup's events and the What's new of an update: read-only boxes of lines, cursor on the first line.</summary>
    public class StatusBoxTests
    {
        [Fact]
        public void AddedLinesComeLastAndTheCursorStaysOnTheFirstLine()
        {
            using (var box = new StatusBox())
            {
                box.AppendLine("Game folder: D:/Games");
                box.AppendLine("Installed mod version: 0.3.3.");
                box.AppendLine("Checking GitHub for updates...");
                Assert.Equal(new[] { "Game folder: D:/Games", "Installed mod version: 0.3.3.", "Checking GitHub for updates..." }, box.Entries);
                Assert.Equal(StatusBox.Join(box.Entries), box.Text);
                Assert.Equal(0, box.SelectionStart);
                Assert.Equal(0, box.SelectionLength);
            }
        }

        [Fact]
        public void ItIsAReadOnlyBoxOfSeveralLinesThatTabReaches()
        {
            using (var box = new StatusBox())
            {
                Assert.True(box.ReadOnly);
                Assert.True(box.Multiline);
                Assert.True(box.TabStop);
                Assert.Equal(ScrollBars.Vertical, box.ScrollBars);
            }
        }

        [Fact]
        public void SetLinesReplacesEverything()
        {
            using (var box = new StatusBox())
            {
                box.AppendLine("old");
                box.SetLines(new[] { "New in 0.3.4", "- one", null });
                Assert.Equal(new[] { "New in 0.3.4", "- one", string.Empty }, box.Entries);
                Assert.Equal("New in 0.3.4\r\n- one\r\n", box.Text);
                Assert.Equal(0, box.SelectionStart);
            }
        }

        [Fact]
        public void WhatsNewShowsTheNotesLineByLineInTheBox()
        {
            using (var d = new UpdateDialog("Chainsaw Warrior Access 9.9.9 is available.", "## New\n- **Thai** language\n- more"))
            {
                StatusBox notes = d.Controls.OfType<StatusBox>().Single();
                Assert.Equal(new[] { "New", "- Thai language", "- more" }, notes.Entries);
                Assert.Equal(0, notes.SelectionStart);
                Assert.True(notes.TabIndex < d.Controls.OfType<Button>().Min(b => b.TabIndex));
            }
        }
    }
}
