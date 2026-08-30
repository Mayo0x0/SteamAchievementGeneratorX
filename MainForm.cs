using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteamAchievementGenerator.Generation;
using SteamAchievementGenerator.Model;
using SteamAchievementGenerator.Parsing;

namespace SteamAchievementGenerator
{
    public partial class MainForm : Form
    {
        private sealed class NamingChoice
        {
            public IconNaming Value { get; set; }
            public string Text { get; set; }
            public override string ToString() { return Text; }
        }

        private readonly HttpClient _http = HttpClientFactory.Create();
        private readonly BindingList<StatEntry> _stats = new BindingList<StatEntry>();

        private string _htmlPath;
        private ParseResult _parsed;
        private string _lastOutputDirectory;
        private CancellationTokenSource _cancellation;

        public MainForm() : this(null)
        {
        }

        public MainForm(string startupFile)
        {
            InitializeComponent();

            BuildAchievementColumns();
            BuildStatColumns();

            cmbIconNaming.Items.Add(new NamingChoice { Value = IconNaming.ApiName, Text = "API name" });
            cmbIconNaming.Items.Add(new NamingChoice { Value = IconNaming.SteamFileName, Text = "Steam file name" });
            cmbIconNaming.SelectedIndex = 0;

            gridStats.DataSource = _stats;

            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
            txtHtmlPath.DragEnter += MainForm_DragEnter;
            txtHtmlPath.DragDrop += MainForm_DragDrop;

            FormClosed += delegate { _http.Dispose(); };

            if (!string.IsNullOrEmpty(startupFile))
                Shown += delegate { LoadHtml(startupFile); };
        }

        // ------------------------------------------------------------------ columns

        private void BuildAchievementColumns()
        {
            gridAchievements.AutoGenerateColumns = false;
            gridAchievements.Columns.Clear();

            gridAchievements.Columns.Add(TextColumn("API Name", 220));
            gridAchievements.Columns.Add(TextColumn("Display Name", 200));
            gridAchievements.Columns.Add(TextColumn("Description", 320));
            gridAchievements.Columns.Add(TextColumn("Hidden", 60));
            gridAchievements.Columns.Add(TextColumn("Unlock %", 70));
            gridAchievements.Columns.Add(TextColumn("Icon", 90));
            gridAchievements.Columns.Add(TextColumn("Icon (locked)", 100));
        }

        private static DataGridViewTextBoxColumn TextColumn(string header, int width)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = header;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            return column;
        }

        private void BuildStatColumns()
        {
            gridStats.AutoGenerateColumns = false;
            gridStats.Columns.Clear();

            var apiName = new DataGridViewTextBoxColumn();
            apiName.HeaderText = "API Name";
            apiName.DataPropertyName = "ApiName";
            apiName.Width = 240;
            apiName.ReadOnly = true;
            gridStats.Columns.Add(apiName);

            var displayName = new DataGridViewTextBoxColumn();
            displayName.HeaderText = "Display Name";
            displayName.DataPropertyName = "DisplayName";
            displayName.Width = 200;
            displayName.ReadOnly = true;
            gridStats.Columns.Add(displayName);

            var type = new DataGridViewComboBoxColumn();
            type.HeaderText = "Type";
            type.DataPropertyName = "Type";
            type.Width = 100;
            type.DataSource = Enum.GetValues(typeof(StatType));
            gridStats.Columns.Add(type);

            var defaultValue = new DataGridViewTextBoxColumn();
            defaultValue.HeaderText = "Default";
            defaultValue.DataPropertyName = "DefaultValue";
            defaultValue.Width = 100;
            gridStats.Columns.Add(defaultValue);

            var globalValue = new DataGridViewTextBoxColumn();
            globalValue.HeaderText = "Global";
            globalValue.DataPropertyName = "GlobalValue";
            globalValue.Width = 100;
            gridStats.Columns.Add(globalValue);
        }

        // ------------------------------------------------------------------- loading

        private void btnSelectHtml_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Saved web pages (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
                dialog.RestoreDirectory = true;
                if (!string.IsNullOrEmpty(_htmlPath))
                    dialog.InitialDirectory = Path.GetDirectoryName(_htmlPath);

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadHtml(dialog.FileName);
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = GetDroppedHtml(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string path = GetDroppedHtml(e);
            if (path != null) LoadHtml(path);
        }

        private static string GetDroppedHtml(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;

            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null) return null;

            foreach (string path in paths)
            {
                string extension = Path.GetExtension(path);
                if (string.Equals(extension, ".html", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".htm", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        private void LoadHtml(string path)
        {
            _htmlPath = path;
            txtHtmlPath.Text = path;
            txtLog.Clear();
            SetStatus("Reading HTML...");

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                var document = HtmlLoader.Load(path);
                _parsed = SteamDbParser.Parse(document);

                ShowGameInfo();
                ShowAchievements();
                ShowStats();

                Log("Loaded " + path);
                Log("  App ID:       " + Or(_parsed.Game.AppId));
                Log("  Achievements: " + _parsed.Achievements.Count);
                Log("  Stats:        " + _parsed.Stats.Count);
                foreach (string warning in _parsed.Warnings) Log("  ! " + warning);

                if (string.IsNullOrEmpty(txtOutputPath.Text) || !Directory.Exists(txtOutputPath.Text))
                    txtOutputPath.Text = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), "steam_settings");

                bool anything = _parsed.Achievements.Count > 0 || _parsed.Stats.Count > 0;
                btnGenerate.Enabled = anything;

                SetStatus(anything
                    ? _parsed.Achievements.Count + " achievements, " + _parsed.Stats.Count + " stats - ready to generate."
                    : "Nothing found. Is this the /stats/ tab of a SteamDB app page?");
            }
            catch (Exception ex)
            {
                _parsed = null;
                btnGenerate.Enabled = false;
                Log("ERROR: " + ex);
                SetStatus("Could not read the HTML file.");
                MessageBox.Show(this, "Could not read the HTML file:" + Environment.NewLine + ex.Message,
                    "Steam Achievement Generator X", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previous;
            }
        }

        private void ShowGameInfo()
        {
            var game = _parsed.Game;

            lblGameName.Text = Or(game.Name);
            lblAppId.Text = Or(game.AppId);
            lblDeveloper.Text = Or(game.Developer);
            lblRelease.Text = Or(game.ReleaseDate);
            lblCounts.Text = _parsed.Achievements.Count + " achievements / " + _parsed.Stats.Count + " stats";

            if (picGameHeader.Image != null)
            {
                picGameHeader.Image.Dispose();
                picGameHeader.Image = null;
            }

            if (game.HeaderImage != null && game.HeaderImage.HasInlineData)
            {
                try
                {
                    using (var stream = new MemoryStream(game.HeaderImage.InlineData))
                        picGameHeader.Image = Image.FromStream(stream);
                }
                catch (ArgumentException)
                {
                    // Not a decodable image - leave the box empty.
                }
            }
        }

        private void ShowAchievements()
        {
            gridAchievements.Rows.Clear();
            if (_parsed.Achievements.Count == 0) return;

            var rows = new List<DataGridViewRow>(_parsed.Achievements.Count);

            foreach (var achievement in _parsed.Achievements)
            {
                var row = new DataGridViewRow();
                row.CreateCells(gridAchievements,
                    achievement.ApiName,
                    achievement.DisplayName,
                    achievement.Description,
                    achievement.Hidden ? "yes" : "",
                    achievement.UnlockPercentage.HasValue
                        ? achievement.UnlockPercentage.Value.ToString("0.0", CultureInfo.InvariantCulture)
                        : "",
                    DescribeIcon(achievement.Icon),
                    DescribeIcon(achievement.IconGray));
                rows.Add(row);
            }

            gridAchievements.Rows.AddRange(rows.ToArray());
        }

        private static string DescribeIcon(ImageRef image)
        {
            if (image == null || image.IsEmpty) return "missing";
            if (image.HasInlineData) return "embedded";
            if (!string.IsNullOrEmpty(image.RelativePath)) return "local file";
            if (!string.IsNullOrEmpty(image.RemoteUrl)) return "URL";
            return "download";
        }

        private void ShowStats()
        {
            _stats.Clear();
            foreach (var stat in _parsed.Stats) _stats.Add(stat);
        }

        // ---------------------------------------------------------------- generation

        private void btnSelectOutput_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose where the steam_settings folder should be written.";

                string current = txtOutputPath.Text;
                if (!string.IsNullOrEmpty(current))
                {
                    string start = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
                    if (!string.IsNullOrEmpty(start) && Directory.Exists(start))
                        dialog.SelectedPath = start;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Selecting the game folder should still produce a steam_settings subfolder.
                    string chosen = dialog.SelectedPath;
                    if (!string.Equals(Path.GetFileName(chosen.TrimEnd(Path.DirectorySeparatorChar)),
                                       "steam_settings", StringComparison.OrdinalIgnoreCase))
                        chosen = Path.Combine(chosen, "steam_settings");

                    txtOutputPath.Text = chosen;
                }
            }
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            if (_parsed == null) return;

            if (_cancellation != null)
            {
                _cancellation.Cancel();
                return;
            }

            string output = txtOutputPath.Text.Trim();
            if (output.Length == 0)
            {
                MessageBox.Show(this, "Please choose an output folder first.", "Steam Achievement Generator X",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Commit a cell that is still being edited in the stats grid.
            gridStats.EndEdit();

            var options = new GeneratorOptions();
            options.OutputDirectory = Path.GetFullPath(output);
            options.WriteAchievements = chkAchievements.Checked;
            options.WriteStats = chkStats.Checked;
            options.DownloadMissingIcons = chkDownload.Checked;
            options.LocalizedTextObjects = chkLocalized.Checked;
            options.CleanImagesFolder = chkClean.Checked;
            options.IconNaming = ((NamingChoice)cmbIconNaming.SelectedItem).Value;

            _parsed.Stats.Clear();
            foreach (var stat in _stats) _parsed.Stats.Add(stat);

            _cancellation = new CancellationTokenSource();
            SetBusy(true);

            StringBuilder summary = null;

            try
            {
                var progress = new Progress<GenerationProgress>(p =>
                {
                    if (p.Total > 0)
                    {
                        progressBar.Maximum = p.Total;
                        progressBar.Value = Math.Min(p.Completed, p.Total);
                    }
                    SetStatus(p.Message);
                });

                var writer = new SteamSettingsWriter(options);
                var report = await writer.GenerateAsync(_parsed, _htmlPath, _http, progress, _cancellation.Token);

                _lastOutputDirectory = report.OutputDirectory;
                btnOpenOutput.Enabled = true;

                Log("");
                Log("Generated in " + report.OutputDirectory);
                Log("  achievements: " + report.AchievementsWritten);
                Log("  stats:        " + report.StatsWritten);
                Log("  icons:        " + report.IconsWritten + " written, " + report.IconsMissing + " missing");
                foreach (string warning in report.Warnings) Log("  ! " + warning);

                SetStatus("Done - " + report.AchievementsWritten + " achievements, " + report.StatsWritten + " stats.");
                summary = new StringBuilder();
                summary.AppendLine("steam_settings written to:");
                summary.AppendLine(report.OutputDirectory);
                summary.AppendLine();
                summary.AppendLine("Achievements: " + report.AchievementsWritten);
                summary.AppendLine("Stats:        " + report.StatsWritten);
                summary.AppendLine("Icons:        " + report.IconsWritten);
                if (report.IconsMissing > 0)
                    summary.AppendLine("Missing icons: " + report.IconsMissing + " (see the Log tab)");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Cancelled.");
                Log("Cancelled by user.");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex);
                SetStatus("Generation failed.");
                MessageBox.Show(this, "Generation failed:" + Environment.NewLine + ex.Message,
                    "Steam Achievement Generator X", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                SetBusy(false);
            }

            // Shown after the UI is interactive again, so the button no longer reads "Cancel".
            if (summary != null)
            {
                MessageBox.Show(this, summary.ToString(), "Steam Achievement Generator X",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnOpenOutput_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastOutputDirectory) || !Directory.Exists(_lastOutputDirectory)) return;

            try
            {
                Process.Start("explorer.exe", "\"" + _lastOutputDirectory + "\"");
            }
            catch (Exception ex)
            {
                Log("Could not open the folder: " + ex.Message);
            }
        }

        // -------------------------------------------------------------------- helpers

        private void SetBusy(bool busy)
        {
            progressBar.Visible = busy;
            if (!busy) progressBar.Value = 0;

            btnSelectHtml.Enabled = !busy;
            btnSelectOutput.Enabled = !busy;
            flowOptions.Enabled = !busy;
            gridStats.Enabled = !busy;

            btnGenerate.Text = busy ? "Cancel" : "Generate steam_settings";
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        private void Log(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);
        }

        private static string Or(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }
    }
}
