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

        // Column indices of gridAchievements.
        private const int ColApiName = 0;
        private const int ColDisplayName = 1;
        private const int ColDescription = 2;
        private const int ColLocalizedName = 3;
        private const int ColLocalizedDescription = 4;
        private const int ColHidden = 5;
        private const int ColUnlock = 6;
        private const int ColIcon = 7;
        private const int ColIconGray = 8;

        private const string NoLanguage = "(none)";

        private static readonly Color MissingTranslationColor = Color.FromArgb(255, 244, 214);

        private readonly HttpClient _http = HttpClientFactory.Create();
        private readonly BindingList<StatEntry> _stats = new BindingList<StatEntry>();

        private string _htmlPath;
        private string _translationPath;
        private ParseResult _parsed;
        private string _language;
        private string _lastOutputDirectory;
        private CancellationTokenSource _cancellation;
        private bool _suspendLanguageEvent;

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

            _suspendLanguageEvent = true;
            cmbLanguage.Items.Add(NoLanguage);
            foreach (string language in SteamLanguages.All)
            {
                if (string.Equals(language, "english", StringComparison.Ordinal)) continue;
                cmbLanguage.Items.Add(language);
            }
            cmbLanguage.SelectedIndex = 0;
            _suspendLanguageEvent = false;

            gridStats.DataSource = _stats;

            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
            txtHtmlPath.DragEnter += MainForm_DragEnter;
            txtHtmlPath.DragDrop += MainForm_DragDrop;
            txtTranslationPath.DragEnter += MainForm_DragEnter;
            txtTranslationPath.DragDrop += MainForm_DragDrop;

            FormClosed += delegate { _http.Dispose(); };

            if (!string.IsNullOrEmpty(startupFile))
                Shown += delegate { LoadAnyHtml(startupFile); };
        }

        // ------------------------------------------------------------------ columns

        private void BuildAchievementColumns()
        {
            gridAchievements.AutoGenerateColumns = false;
            gridAchievements.Columns.Clear();

            gridAchievements.Columns.Add(TextColumn("API Name", 210, true));
            gridAchievements.Columns.Add(TextColumn("Display Name", 170, true));
            gridAchievements.Columns.Add(TextColumn("Description", 260, true));
            gridAchievements.Columns.Add(TextColumn("Name (translated)", 170, false));
            gridAchievements.Columns.Add(TextColumn("Description (translated)", 260, false));
            gridAchievements.Columns.Add(TextColumn("Hidden", 60, true));
            gridAchievements.Columns.Add(TextColumn("Unlock %", 70, true));
            gridAchievements.Columns.Add(TextColumn("Icon", 90, true));
            gridAchievements.Columns.Add(TextColumn("Icon (locked)", 100, true));

            UpdateLocalizedColumnHeaders();
        }

        private static DataGridViewTextBoxColumn TextColumn(string header, int width, bool readOnly)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = header;
            column.Width = width;
            column.ReadOnly = readOnly;
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

        private void UpdateLocalizedColumnHeaders()
        {
            string suffix = string.IsNullOrEmpty(_language) ? "translated" : _language;

            gridAchievements.Columns[ColLocalizedName].HeaderText = "Name (" + suffix + ")";
            gridAchievements.Columns[ColLocalizedDescription].HeaderText = "Description (" + suffix + ")";

            bool editable = !string.IsNullOrEmpty(_language);
            gridAchievements.Columns[ColLocalizedName].ReadOnly = !editable;
            gridAchievements.Columns[ColLocalizedDescription].ReadOnly = !editable;
        }

        // ------------------------------------------------------------------- loading

        private void btnSelectHtml_Click(object sender, EventArgs e)
        {
            string path = AskForHtml("Saved SteamDB stats page", _htmlPath);
            if (path != null) LoadSteamDb(path);
        }

        private void btnSelectTranslation_Click(object sender, EventArgs e)
        {
            if (_parsed == null)
            {
                MessageBox.Show(this, "Load the SteamDB stats page first - the translations are merged onto it.",
                    "Steam Achievement Generator X", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string path = AskForHtml("Saved steamcommunity.com achievements page", _translationPath ?? _htmlPath);
            if (path != null) LoadTranslation(path);
        }

        private string AskForHtml(string title, string near)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = "Saved web pages (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
                dialog.RestoreDirectory = true;

                if (!string.IsNullOrEmpty(near))
                {
                    string directory = Path.GetDirectoryName(near);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        dialog.InitialDirectory = directory;
                }

                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = GetDroppedHtml(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string path = GetDroppedHtml(e);
            if (path != null) LoadAnyHtml(path);
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

        /// <summary>Decides from the file itself whether it is a SteamDB page or a community page.</summary>
        private void LoadAnyHtml(string path)
        {
            if (_parsed != null && LooksLikeCommunityPage(path)) LoadTranslation(path);
            else LoadSteamDb(path);
        }

        private static bool LooksLikeCommunityPage(string path)
        {
            try
            {
                using (var reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    var buffer = new char[262144];
                    int read = reader.Read(buffer, 0, buffer.Length);
                    string head = new string(buffer, 0, read);

                    return head.IndexOf("achieveRow", StringComparison.OrdinalIgnoreCase) >= 0
                        || head.IndexOf("steamcommunity.com/stats", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        private void LoadSteamDb(string path)
        {
            _htmlPath = path;
            txtHtmlPath.Text = path;
            txtLog.Clear();
            SetStatus("Reading HTML...");

            // A new base page invalidates whatever was merged onto the old one.
            _translationPath = null;
            txtTranslationPath.Text = "";
            SelectLanguage(null);
            lblTranslated.Text = "-";

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                _parsed = SteamDbParser.Parse(HtmlLoader.Load(path));

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

        private void LoadTranslation(string path)
        {
            if (_parsed == null) return;

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                var translations = SteamCommunityParser.Parse(HtmlLoader.Load(path));

                Log("");
                Log("Loaded translations from " + path);
                foreach (string warning in translations.Warnings) Log("  ! " + warning);

                string language = translations.Language;
                if (string.IsNullOrEmpty(language))
                {
                    language = AskForLanguage(translations.DetectedTag);
                    if (string.IsNullOrEmpty(language))
                    {
                        Log("  ! Cancelled - no language chosen.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(translations.AppId) &&
                    !string.IsNullOrEmpty(_parsed.Game.AppId) &&
                    translations.AppId != _parsed.Game.AppId)
                {
                    var answer = MessageBox.Show(this,
                        "That page belongs to App ID " + translations.AppId +
                        ", but the SteamDB page is App ID " + _parsed.Game.AppId + "." +
                        Environment.NewLine + Environment.NewLine + "Use it anyway?",
                        "Steam Achievement Generator X", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (answer != DialogResult.Yes) return;
                }

                _translationPath = path;
                txtTranslationPath.Text = path;

                // Drop a previous run for the same language so removed rows do not linger.
                foreach (var achievement in _parsed.Achievements)
                    achievement.SetLocalized(language, null, null);

                var report = TranslationMerger.Apply(_parsed.Achievements, translations, language);

                SelectLanguage(language);
                ShowAchievements();

                Log("  Language:     " + language +
                    (string.IsNullOrEmpty(translations.DetectedTag) ? "" : " (from lang=\"" + translations.DetectedTag + "\")"));
                Log("  Rows:         " + translations.Achievements.Count);
                Log("  Translated:   " + report.Matched + " of " + _parsed.Achievements.Count);

                foreach (string note in report.Notes) Log("  ! " + note);

                if (report.Untranslated.Count > 0)
                {
                    Log("  Missing " + language + " text for " + report.Untranslated.Count + " achievements:");
                    foreach (var achievement in report.Untranslated)
                        Log("    - " + achievement.ApiName + "  (" + achievement.DisplayName + ")");
                }

                if (report.Unassigned.Count > 0)
                {
                    Log("  " + report.Unassigned.Count + " rows of the localized page matched no achievement:");
                    foreach (var row in report.Unassigned)
                        Log("    - " + row.DisplayName);
                }

                ReportTranslationResult(report);
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex);
                SetStatus("Could not read the localized page.");
                MessageBox.Show(this, "Could not read the localized page:" + Environment.NewLine + ex.Message,
                    "Steam Achievement Generator X", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previous;
            }
        }

        private void ReportTranslationResult(TranslationReport report)
        {
            int total = _parsed.Achievements.Count;
            lblTranslated.Text = report.Matched + " / " + total + " " + report.Language;

            if (report.Untranslated.Count == 0 && report.Unassigned.Count == 0)
            {
                SetStatus("All " + total + " achievements have " + report.Language + " text.");
                return;
            }

            SetStatus(report.Matched + " of " + total + " achievements translated - " +
                      report.Untranslated.Count + " still missing.");

            var message = new StringBuilder();
            message.AppendLine(report.Matched + " of " + total + " achievements got " + report.Language + " text.");

            if (report.Untranslated.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Missing (highlighted in the Achievements tab, editable there):");

                int shown = 0;
                foreach (var achievement in report.Untranslated)
                {
                    if (shown++ >= 15)
                    {
                        message.AppendLine("  ... and " + (report.Untranslated.Count - 15) + " more, see the Log tab");
                        break;
                    }
                    message.AppendLine("  - " + achievement.ApiName + "  (" + achievement.DisplayName + ")");
                }
            }

            if (report.Unassigned.Count > 0)
            {
                message.AppendLine();
                message.AppendLine(report.Unassigned.Count +
                                   " rows of the localized page could not be assigned - see the Log tab.");
            }

            MessageBox.Show(this, message.ToString(), "Steam Achievement Generator X",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string AskForLanguage(string detectedTag)
        {
            using (var dialog = new Form())
            using (var combo = new ComboBox())
            using (var label = new Label())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                dialog.Text = "Which language?";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(360, 120);

                label.SetBounds(12, 12, 336, 34);
                label.Text = string.IsNullOrEmpty(detectedTag)
                    ? "The page does not say which language it is in."
                    : "Could not map the page language \"" + detectedTag + "\" to a Steam language.";

                combo.SetBounds(12, 50, 336, 21);
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                foreach (string language in SteamLanguages.All) combo.Items.Add(language);
                combo.SelectedIndex = 0;

                ok.SetBounds(192, 84, 75, 24);
                ok.Text = "OK";
                ok.DialogResult = DialogResult.OK;

                cancel.SetBounds(273, 84, 75, 24);
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;

                dialog.Controls.AddRange(new Control[] { label, combo, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;

                return dialog.ShowDialog(this) == DialogResult.OK ? (string)combo.SelectedItem : null;
            }
        }

        private void SelectLanguage(string language)
        {
            _language = language;
            _suspendLanguageEvent = true;

            int index = cmbLanguage.Items.IndexOf(language ?? NoLanguage);
            if (index < 0 && !string.IsNullOrEmpty(language))
            {
                cmbLanguage.Items.Add(language);
                index = cmbLanguage.Items.Count - 1;
            }

            cmbLanguage.SelectedIndex = index < 0 ? 0 : index;
            _suspendLanguageEvent = false;

            UpdateLocalizedColumnHeaders();
        }

        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendLanguageEvent) return;

            string selected = cmbLanguage.SelectedItem as string;
            _language = string.Equals(selected, NoLanguage, StringComparison.Ordinal) ? null : selected;

            UpdateLocalizedColumnHeaders();
            ShowAchievements();
            UpdateTranslatedCount();
        }

        // ------------------------------------------------------------------- display

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
            if (_parsed == null || _parsed.Achievements.Count == 0) return;

            var rows = new List<DataGridViewRow>(_parsed.Achievements.Count);

            foreach (var achievement in _parsed.Achievements)
            {
                var row = new DataGridViewRow();
                row.CreateCells(gridAchievements,
                    achievement.ApiName,
                    achievement.DisplayName,
                    achievement.Description,
                    achievement.GetLocalizedDisplayName(_language),
                    achievement.GetLocalizedDescription(_language),
                    achievement.Hidden ? "yes" : "",
                    achievement.UnlockPercentage.HasValue
                        ? achievement.UnlockPercentage.Value.ToString("0.0", CultureInfo.InvariantCulture)
                        : "",
                    DescribeIcon(achievement.Icon),
                    DescribeIcon(achievement.IconGray));

                rows.Add(row);
            }

            gridAchievements.Rows.AddRange(rows.ToArray());

            for (int i = 0; i < _parsed.Achievements.Count; i++)
                ApplyTranslationHighlight(i);
        }

        private void ApplyTranslationHighlight(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= gridAchievements.Rows.Count) return;

            var row = gridAchievements.Rows[rowIndex];
            bool missing = !string.IsNullOrEmpty(_language)
                        && !_parsed.Achievements[rowIndex].HasLocalization(_language);

            var color = missing ? MissingTranslationColor : Color.Empty;
            row.Cells[ColLocalizedName].Style.BackColor = color;
            row.Cells[ColLocalizedDescription].Style.BackColor = color;
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

        private void gridAchievements_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_parsed == null || string.IsNullOrEmpty(_language)) return;
            if (e.RowIndex < 0 || e.RowIndex >= _parsed.Achievements.Count) return;
            if (e.ColumnIndex != ColLocalizedName && e.ColumnIndex != ColLocalizedDescription) return;

            var row = gridAchievements.Rows[e.RowIndex];
            _parsed.Achievements[e.RowIndex].SetLocalized(
                _language,
                Convert.ToString(row.Cells[ColLocalizedName].Value),
                Convert.ToString(row.Cells[ColLocalizedDescription].Value));

            ApplyTranslationHighlight(e.RowIndex);
            UpdateTranslatedCount();
        }

        private void UpdateTranslatedCount()
        {
            if (_parsed == null || string.IsNullOrEmpty(_language))
            {
                lblTranslated.Text = "-";
                return;
            }

            int translated = 0;
            foreach (var achievement in _parsed.Achievements)
                if (achievement.HasLocalization(_language)) translated++;

            int total = _parsed.Achievements.Count;
            lblTranslated.Text = translated + " / " + total + " " + _language;

            SetStatus(translated == total
                ? "All " + total + " achievements have " + _language + " text."
                : translated + " of " + total + " achievements translated - " + (total - translated) + " still missing.");
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

            // Commit cells that are still being edited.
            gridStats.EndEdit();
            gridAchievements.EndEdit();

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

                int untranslated = CountUntranslated();
                if (untranslated > 0)
                    summary.AppendLine("Without " + _language + " text: " + untranslated);
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

        private int CountUntranslated()
        {
            if (_parsed == null || string.IsNullOrEmpty(_language)) return 0;

            int missing = 0;
            foreach (var achievement in _parsed.Achievements)
                if (!achievement.HasLocalization(_language)) missing++;

            return missing;
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
            btnSelectTranslation.Enabled = !busy;
            cmbLanguage.Enabled = !busy;
            btnSelectOutput.Enabled = !busy;
            flowOptions.Enabled = !busy;
            gridStats.Enabled = !busy;
            gridAchievements.Enabled = !busy;

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
