namespace SteamAchievementGenerator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.panelHeader = new System.Windows.Forms.Panel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.lblHeadline = new System.Windows.Forms.Label();
            this.grpSource = new System.Windows.Forms.GroupBox();
            this.layoutSource = new System.Windows.Forms.TableLayoutPanel();
            this.txtHtmlPath = new System.Windows.Forms.TextBox();
            this.btnSelectHtml = new System.Windows.Forms.Button();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.grpGame = new System.Windows.Forms.GroupBox();
            this.layoutGame = new System.Windows.Forms.TableLayoutPanel();
            this.picGameHeader = new System.Windows.Forms.PictureBox();
            this.lblGameNameCaption = new System.Windows.Forms.Label();
            this.lblGameName = new System.Windows.Forms.Label();
            this.lblAppIdCaption = new System.Windows.Forms.Label();
            this.lblAppId = new System.Windows.Forms.Label();
            this.lblDeveloperCaption = new System.Windows.Forms.Label();
            this.lblDeveloper = new System.Windows.Forms.Label();
            this.lblReleaseCaption = new System.Windows.Forms.Label();
            this.lblRelease = new System.Windows.Forms.Label();
            this.lblCountsCaption = new System.Windows.Forms.Label();
            this.lblCounts = new System.Windows.Forms.Label();
            this.tabsData = new System.Windows.Forms.TabControl();
            this.tabAchievements = new System.Windows.Forms.TabPage();
            this.gridAchievements = new System.Windows.Forms.DataGridView();
            this.tabStats = new System.Windows.Forms.TabPage();
            this.gridStats = new System.Windows.Forms.DataGridView();
            this.lblStatsHint = new System.Windows.Forms.Label();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.grpOutput = new System.Windows.Forms.GroupBox();
            this.layoutOutput = new System.Windows.Forms.TableLayoutPanel();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.btnSelectOutput = new System.Windows.Forms.Button();
            this.flowOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.chkAchievements = new System.Windows.Forms.CheckBox();
            this.chkStats = new System.Windows.Forms.CheckBox();
            this.chkDownload = new System.Windows.Forms.CheckBox();
            this.chkLocalized = new System.Windows.Forms.CheckBox();
            this.chkClean = new System.Windows.Forms.CheckBox();
            this.lblIconNaming = new System.Windows.Forms.Label();
            this.cmbIconNaming = new System.Windows.Forms.ComboBox();
            this.panelActions = new System.Windows.Forms.TableLayoutPanel();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnOpenOutput = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();

            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.grpSource.SuspendLayout();
            this.layoutSource.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpGame.SuspendLayout();
            this.layoutGame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picGameHeader)).BeginInit();
            this.tabsData.SuspendLayout();
            this.tabAchievements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAchievements)).BeginInit();
            this.tabStats.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStats)).BeginInit();
            this.tabLog.SuspendLayout();
            this.grpOutput.SuspendLayout();
            this.layoutOutput.SuspendLayout();
            this.flowOptions.SuspendLayout();
            this.panelActions.SuspendLayout();
            this.SuspendLayout();

            // panelHeader
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 60;
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Controls.Add(this.lblHeadline);
            this.panelHeader.Controls.Add(this.picLogo);

            // picLogo
            this.picLogo.Dock = System.Windows.Forms.DockStyle.Left;
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(190, 56);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabStop = false;
            this.picLogo.Image = global::SteamAchievementGenerator.Properties.Resources.AchievementsGenLogo;

            // lblHeadline
            this.lblHeadline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadline.Name = "lblHeadline";
            this.lblHeadline.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeadline.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHeadline.Text = "SteamDB page  ->  steam_settings for gbe_fork (Goldberg)";
            this.lblHeadline.Font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, 10F, System.Drawing.FontStyle.Bold);

            // grpSource
            this.grpSource.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSource.Height = 62;
            this.grpSource.Name = "grpSource";
            this.grpSource.TabStop = false;
            this.grpSource.Text = "1. Saved SteamDB stats page (WebScrapBook single file, or \"webpage, complete\")";
            this.grpSource.Controls.Add(this.layoutSource);

            // layoutSource
            this.layoutSource.AutoSize = true;
            this.layoutSource.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutSource.ColumnCount = 2;
            this.layoutSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutSource.Dock = System.Windows.Forms.DockStyle.Top;
            this.layoutSource.Name = "layoutSource";
            this.layoutSource.RowCount = 1;
            this.layoutSource.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutSource.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.layoutSource.Controls.Add(this.txtHtmlPath, 0, 0);
            this.layoutSource.Controls.Add(this.btnSelectHtml, 1, 0);

            // txtHtmlPath
            this.txtHtmlPath.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtHtmlPath.Name = "txtHtmlPath";
            this.txtHtmlPath.AllowDrop = true;

            // btnSelectHtml
            this.btnSelectHtml.AutoSize = false;
            this.btnSelectHtml.Name = "btnSelectHtml";
            this.btnSelectHtml.Size = new System.Drawing.Size(120, 25);
            this.btnSelectHtml.Text = "Select HTML...";
            this.btnSelectHtml.UseVisualStyleBackColor = true;
            this.btnSelectHtml.Click += new System.EventHandler(this.btnSelectHtml_Click);

            // splitMain
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Name = "splitMain";
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Panel1MinSize = 240;
            this.splitMain.Panel2MinSize = 320;
            this.splitMain.Panel1.Controls.Add(this.grpGame);
            this.splitMain.Panel2.Controls.Add(this.tabsData);
            this.splitMain.Size = new System.Drawing.Size(1080, 400);
            this.splitMain.SplitterDistance = 330;

            // grpGame
            this.grpGame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGame.Name = "grpGame";
            this.grpGame.TabStop = false;
            this.grpGame.Text = "Game";
            this.grpGame.Controls.Add(this.layoutGame);

            // layoutGame
            this.layoutGame.ColumnCount = 2;
            this.layoutGame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutGame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutGame.Name = "layoutGame";
            this.layoutGame.RowCount = 6;
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutGame.Padding = new System.Windows.Forms.Padding(6);
            this.layoutGame.Controls.Add(this.picGameHeader, 0, 0);
            this.layoutGame.SetColumnSpan(this.picGameHeader, 2);
            this.layoutGame.Controls.Add(this.lblGameNameCaption, 0, 1);
            this.layoutGame.Controls.Add(this.lblGameName, 1, 1);
            this.layoutGame.Controls.Add(this.lblAppIdCaption, 0, 2);
            this.layoutGame.Controls.Add(this.lblAppId, 1, 2);
            this.layoutGame.Controls.Add(this.lblDeveloperCaption, 0, 3);
            this.layoutGame.Controls.Add(this.lblDeveloper, 1, 3);
            this.layoutGame.Controls.Add(this.lblReleaseCaption, 0, 4);
            this.layoutGame.Controls.Add(this.lblRelease, 1, 4);
            this.layoutGame.Controls.Add(this.lblCountsCaption, 0, 5);
            this.layoutGame.Controls.Add(this.lblCounts, 1, 5);

            // picGameHeader
            this.picGameHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picGameHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picGameHeader.Name = "picGameHeader";
            this.picGameHeader.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picGameHeader.TabStop = false;

            // captions and values
            this.lblGameNameCaption.AutoSize = true;
            this.lblGameNameCaption.Margin = new System.Windows.Forms.Padding(3, 8, 12, 3);
            this.lblGameNameCaption.Text = "Name";
            this.lblGameName.AutoSize = true;
            this.lblGameName.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblGameName.Text = "-";
            this.lblGameName.MaximumSize = new System.Drawing.Size(240, 0);

            this.lblAppIdCaption.AutoSize = true;
            this.lblAppIdCaption.Margin = new System.Windows.Forms.Padding(3, 6, 12, 3);
            this.lblAppIdCaption.Text = "App ID";
            this.lblAppId.AutoSize = true;
            this.lblAppId.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblAppId.Text = "-";

            this.lblDeveloperCaption.AutoSize = true;
            this.lblDeveloperCaption.Margin = new System.Windows.Forms.Padding(3, 6, 12, 3);
            this.lblDeveloperCaption.Text = "Developer";
            this.lblDeveloper.AutoSize = true;
            this.lblDeveloper.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblDeveloper.Text = "-";
            this.lblDeveloper.MaximumSize = new System.Drawing.Size(240, 0);

            this.lblReleaseCaption.AutoSize = true;
            this.lblReleaseCaption.Margin = new System.Windows.Forms.Padding(3, 6, 12, 3);
            this.lblReleaseCaption.Text = "Released";
            this.lblRelease.AutoSize = true;
            this.lblRelease.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblRelease.Text = "-";

            this.lblCountsCaption.AutoSize = true;
            this.lblCountsCaption.Margin = new System.Windows.Forms.Padding(3, 6, 12, 3);
            this.lblCountsCaption.Text = "Found";
            this.lblCounts.AutoSize = true;
            this.lblCounts.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblCounts.Text = "-";

            // tabsData
            this.tabsData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsData.Name = "tabsData";
            this.tabsData.SelectedIndex = 0;
            this.tabsData.Controls.Add(this.tabAchievements);
            this.tabsData.Controls.Add(this.tabStats);
            this.tabsData.Controls.Add(this.tabLog);

            // tabAchievements
            this.tabAchievements.Name = "tabAchievements";
            this.tabAchievements.Padding = new System.Windows.Forms.Padding(3);
            this.tabAchievements.Text = "Achievements";
            this.tabAchievements.UseVisualStyleBackColor = true;
            this.tabAchievements.Controls.Add(this.gridAchievements);

            // gridAchievements
            this.gridAchievements.AllowUserToAddRows = false;
            this.gridAchievements.AllowUserToDeleteRows = false;
            this.gridAchievements.AllowUserToResizeRows = false;
            this.gridAchievements.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAchievements.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.gridAchievements.Name = "gridAchievements";
            this.gridAchievements.ReadOnly = true;
            this.gridAchievements.RowHeadersVisible = false;
            this.gridAchievements.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // tabStats
            this.tabStats.Name = "tabStats";
            this.tabStats.Padding = new System.Windows.Forms.Padding(3);
            this.tabStats.Text = "Stats";
            this.tabStats.UseVisualStyleBackColor = true;
            this.tabStats.Controls.Add(this.gridStats);
            this.tabStats.Controls.Add(this.lblStatsHint);

            // lblStatsHint
            this.lblStatsHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatsHint.Name = "lblStatsHint";
            this.lblStatsHint.Height = 34;
            this.lblStatsHint.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblStatsHint.Text = "SteamDB does not publish the stat type. It is guessed from the default value - "
                + "correct \"int\" / \"float\" / \"avgrate\" here before generating if the game needs it.";

            // gridStats
            this.gridStats.AllowUserToAddRows = false;
            this.gridStats.AllowUserToResizeRows = false;
            this.gridStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridStats.Name = "gridStats";
            this.gridStats.RowHeadersVisible = false;
            this.gridStats.AutoGenerateColumns = false;

            // tabLog
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            this.tabLog.Controls.Add(this.txtLog);

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.WordWrap = false;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);

            // grpOutput
            this.grpOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpOutput.Height = 104;
            this.grpOutput.Name = "grpOutput";
            this.grpOutput.TabStop = false;
            this.grpOutput.Text = "2. Output";
            this.grpOutput.Controls.Add(this.layoutOutput);

            // layoutOutput
            this.layoutOutput.AutoSize = true;
            this.layoutOutput.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutOutput.ColumnCount = 2;
            this.layoutOutput.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutOutput.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutOutput.Dock = System.Windows.Forms.DockStyle.Top;
            this.layoutOutput.Name = "layoutOutput";
            this.layoutOutput.RowCount = 2;
            this.layoutOutput.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutOutput.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutOutput.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.layoutOutput.Controls.Add(this.txtOutputPath, 0, 0);
            this.layoutOutput.Controls.Add(this.btnSelectOutput, 1, 0);
            this.layoutOutput.Controls.Add(this.flowOptions, 0, 1);
            this.layoutOutput.SetColumnSpan(this.flowOptions, 2);

            // txtOutputPath
            this.txtOutputPath.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtOutputPath.Name = "txtOutputPath";

            // btnSelectOutput
            this.btnSelectOutput.AutoSize = false;
            this.btnSelectOutput.Name = "btnSelectOutput";
            this.btnSelectOutput.Size = new System.Drawing.Size(120, 25);
            this.btnSelectOutput.Text = "Select folder...";
            this.btnSelectOutput.UseVisualStyleBackColor = true;
            this.btnSelectOutput.Click += new System.EventHandler(this.btnSelectOutput_Click);

            // flowOptions
            this.flowOptions.AutoSize = true;
            this.flowOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowOptions.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowOptions.Name = "flowOptions";
            this.flowOptions.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.flowOptions.WrapContents = true;
            this.flowOptions.Controls.Add(this.chkAchievements);
            this.flowOptions.Controls.Add(this.chkStats);
            this.flowOptions.Controls.Add(this.chkDownload);
            this.flowOptions.Controls.Add(this.chkLocalized);
            this.flowOptions.Controls.Add(this.chkClean);
            this.flowOptions.Controls.Add(this.lblIconNaming);
            this.flowOptions.Controls.Add(this.cmbIconNaming);

            this.chkAchievements.AutoSize = true;
            this.chkAchievements.Checked = true;
            this.chkAchievements.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAchievements.Name = "chkAchievements";
            this.chkAchievements.Text = "achievements.json";
            this.chkAchievements.UseVisualStyleBackColor = true;

            this.chkStats.AutoSize = true;
            this.chkStats.Checked = true;
            this.chkStats.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStats.Name = "chkStats";
            this.chkStats.Text = "stats.json";
            this.chkStats.UseVisualStyleBackColor = true;

            this.chkDownload.AutoSize = true;
            this.chkDownload.Checked = true;
            this.chkDownload.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDownload.Name = "chkDownload";
            this.chkDownload.Text = "download icons";
            this.chkDownload.UseVisualStyleBackColor = true;

            this.chkLocalized.AutoSize = true;
            this.chkLocalized.Checked = true;
            this.chkLocalized.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLocalized.Name = "chkLocalized";
            this.chkLocalized.Text = "localized text";
            this.chkLocalized.UseVisualStyleBackColor = true;

            this.chkClean.AutoSize = true;
            this.chkClean.Name = "chkClean";
            this.chkClean.Text = "clear images first";
            this.chkClean.UseVisualStyleBackColor = true;

            this.lblIconNaming.AutoSize = true;
            this.lblIconNaming.Margin = new System.Windows.Forms.Padding(12, 6, 3, 3);
            this.lblIconNaming.Name = "lblIconNaming";
            this.lblIconNaming.Text = "Icon names:";

            this.cmbIconNaming.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbIconNaming.Name = "cmbIconNaming";
            this.cmbIconNaming.Width = 150;

            // panelActions
            this.panelActions.ColumnCount = 4;
            this.panelActions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.panelActions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.panelActions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelActions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.panelActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelActions.Height = 46;
            this.panelActions.Name = "panelActions";
            this.panelActions.RowCount = 1;
            this.panelActions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.panelActions.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.panelActions.Controls.Add(this.btnGenerate, 0, 0);
            this.panelActions.Controls.Add(this.btnOpenOutput, 1, 0);
            this.panelActions.Controls.Add(this.lblStatus, 2, 0);
            this.panelActions.Controls.Add(this.progressBar, 3, 0);

            // btnGenerate
            this.btnGenerate.AutoSize = false;
            this.btnGenerate.Enabled = false;
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(200, 32);
            this.btnGenerate.Text = "Generate steam_settings";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

            // btnOpenOutput
            this.btnOpenOutput.AutoSize = false;
            this.btnOpenOutput.Enabled = false;
            this.btnOpenOutput.Name = "btnOpenOutput";
            this.btnOpenOutput.Size = new System.Drawing.Size(120, 32);
            this.btnOpenOutput.Text = "Open folder";
            this.btnOpenOutput.UseVisualStyleBackColor = true;
            this.btnOpenOutput.Click += new System.EventHandler(this.btnOpenOutput_Click);

            // lblStatus
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStatus.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblStatus.Text = "Select a saved SteamDB stats page to begin.";

            // progressBar
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Name = "progressBar";
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this.progressBar.Visible = false;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 720);
            this.MinimumSize = new System.Drawing.Size(1000, 620);
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.grpSource);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.grpOutput);
            this.Controls.Add(this.panelActions);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Steam Achievement Generator X";
            this.AllowDrop = true;

            this.panelActions.ResumeLayout(false);
            this.panelActions.PerformLayout();
            this.flowOptions.ResumeLayout(false);
            this.flowOptions.PerformLayout();
            this.layoutOutput.ResumeLayout(false);
            this.layoutOutput.PerformLayout();
            this.grpOutput.ResumeLayout(false);
            this.grpOutput.PerformLayout();
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStats)).EndInit();
            this.tabStats.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAchievements)).EndInit();
            this.tabAchievements.ResumeLayout(false);
            this.tabsData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picGameHeader)).EndInit();
            this.layoutGame.ResumeLayout(false);
            this.layoutGame.PerformLayout();
            this.grpGame.ResumeLayout(false);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.layoutSource.ResumeLayout(false);
            this.layoutSource.PerformLayout();
            this.grpSource.ResumeLayout(false);
            this.grpSource.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.panelHeader.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblHeadline;
        private System.Windows.Forms.GroupBox grpSource;
        private System.Windows.Forms.TableLayoutPanel layoutSource;
        private System.Windows.Forms.TextBox txtHtmlPath;
        private System.Windows.Forms.Button btnSelectHtml;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpGame;
        private System.Windows.Forms.TableLayoutPanel layoutGame;
        private System.Windows.Forms.PictureBox picGameHeader;
        private System.Windows.Forms.Label lblGameNameCaption;
        private System.Windows.Forms.Label lblGameName;
        private System.Windows.Forms.Label lblAppIdCaption;
        private System.Windows.Forms.Label lblAppId;
        private System.Windows.Forms.Label lblDeveloperCaption;
        private System.Windows.Forms.Label lblDeveloper;
        private System.Windows.Forms.Label lblReleaseCaption;
        private System.Windows.Forms.Label lblRelease;
        private System.Windows.Forms.Label lblCountsCaption;
        private System.Windows.Forms.Label lblCounts;
        private System.Windows.Forms.TabControl tabsData;
        private System.Windows.Forms.TabPage tabAchievements;
        private System.Windows.Forms.DataGridView gridAchievements;
        private System.Windows.Forms.TabPage tabStats;
        private System.Windows.Forms.DataGridView gridStats;
        private System.Windows.Forms.Label lblStatsHint;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.GroupBox grpOutput;
        private System.Windows.Forms.TableLayoutPanel layoutOutput;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnSelectOutput;
        private System.Windows.Forms.FlowLayoutPanel flowOptions;
        private System.Windows.Forms.CheckBox chkAchievements;
        private System.Windows.Forms.CheckBox chkStats;
        private System.Windows.Forms.CheckBox chkDownload;
        private System.Windows.Forms.CheckBox chkLocalized;
        private System.Windows.Forms.CheckBox chkClean;
        private System.Windows.Forms.Label lblIconNaming;
        private System.Windows.Forms.ComboBox cmbIconNaming;
        private System.Windows.Forms.TableLayoutPanel panelActions;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnOpenOutput;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
    }
}
