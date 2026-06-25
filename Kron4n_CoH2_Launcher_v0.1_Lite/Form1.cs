using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kron4n_CoH2_Launcher
{
    public partial class Form1 : Form
    {
        private const string AppId = "231430";
        private const string AppTitle = "Kron4n CoH2 Launcher v0.1";

        private static readonly Color WindowBg = Color.FromArgb(22, 22, 27);
        private static readonly Color PanelBg = Color.FromArgb(16, 16, 21);
        private static readonly Color TopBarBg = Color.FromArgb(10, 10, 14);
        private static readonly Color SectionHeaderBg = Color.FromArgb(18, 18, 24);
        private static readonly Color SoftBorder = Color.FromArgb(95, 95, 110);
        private static readonly Color GoodGreen = Color.FromArgb(110, 220, 140);
        private static readonly Color WarnYellow = Color.FromArgb(235, 200, 90);

        private ComboBox cmbProfile = null!;
        private CheckBox chkSkipMovies = null!;
        private CheckBox chkBorderless = null!;
        private CheckBox chkLockMouse = null!;
        private CheckBox chkNoVsyncTripleBuffer = null!;
        private CheckBox chkForceActive = null!;
        private CheckBox chkDevMode = null!;
        private CheckBox chkAllowIncompatibleReplays = null!;

        private ComboBox cmbRefresh = null!;
        private TextBox txtCustomRefresh = null!;
        private TextBox txtExtraArgs = null!;
        private TextBox txtPreview = null!;

        private Label lblSteamStatus = null!;
        private Label lblCoH2Status = null!;
        private Label lblDocsStatus = null!;
        private Label lblStatus = null!;
        private Button btnMaximize = null!;

        private bool applyingProfile;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private const int ResizeBorder = 8;

        public Form1()
        {
            InitializeComponent();

            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;

            Text = AppTitle;
            Icon = LoadWindowIcon();

            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1060, 900);
            Size = new Size(1120, 940);
            BackColor = WindowBg;
            ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9F);

            FormBorderStyle = FormBorderStyle.None;
            MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;

            Controls.Clear();

            BuildUi();
            RefreshDetectionStatus();
            SelectProfile("Default Safe Modern Fix");
            UpdatePreview();
            ApplyRoundedCorners();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(1),
                Margin = new Padding(0),
                BackColor = SoftBorder
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(root);

            BuildTitleBar(root);
            BuildMainContent(root);
        }

        private void BuildTitleBar(TableLayoutPanel root)
        {
            var titleBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = TopBarBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.DoubleClick += (_, _) => ToggleMaximize();

            var divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(45, 45, 55)
            };

            var titleLabel = new Label
            {
                Text = AppTitle,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(14, 0, 0, 0),
                BackColor = TopBarBg
            };

            titleLabel.MouseDown += TitleBar_MouseDown;
            titleLabel.DoubleClick += (_, _) => ToggleMaximize();

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 180,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = TopBarBg
            };

            var btnMinimize = MakeTitleButton("—");
            btnMaximize = MakeTitleButton("□");
            var btnClose = MakeTitleButton("X");

            btnMinimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
            btnMaximize.Click += (_, _) => ToggleMaximize();
            btnClose.Click += (_, _) => Close();

            btnClose.MouseEnter += (_, _) =>
            {
                btnClose.BackColor = Color.FromArgb(232, 17, 35);
                btnClose.ForeColor = Color.White;
            };

            btnClose.MouseLeave += (_, _) =>
            {
                btnClose.BackColor = TopBarBg;
                btnClose.ForeColor = Color.WhiteSmoke;
            };

            buttonPanel.Controls.Add(btnMinimize);
            buttonPanel.Controls.Add(btnMaximize);
            buttonPanel.Controls.Add(btnClose);

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(buttonPanel);
            titleBar.Controls.Add(divider);

            root.Controls.Add(titleBar, 0, 0);
        }

        private void BuildMainContent(TableLayoutPanel root)
        {
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowBg,
                Padding = new Padding(28, 26, 28, 26),
                Margin = new Padding(0)
            };

            root.Controls.Add(contentHost, 0, 1);

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                ColumnCount = 1,
                RowCount = 6,
                BackColor = WindowBg
            };

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 316));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            contentHost.Controls.Add(main);

            BuildHeader(main);
            BuildDetectionBox(main);
            BuildOptionsBox(main);
            BuildButtonPanel(main);
            BuildPreviewBox(main);
            BuildFooter(main);
        }

        private void BuildHeader(TableLayoutPanel main)
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0)
            };

            Image? embeddedHeader = LoadEmbeddedImage("Kron4n_Header.png");

            if (embeddedHeader != null)
            {
                var headerImage = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = embeddedHeader,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = PanelBg
                };

                header.Controls.Add(headerImage);
            }
            else
            {
                var fallbackPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = PanelBg,
                    Padding = new Padding(30, 18, 30, 16)
                };

                var logo = new Label
                {
                    Text = "KRON4N",
                    Dock = DockStyle.Top,
                    Height = 50,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = PanelBg
                };

                var subtitle = new Label
                {
                    Text = "Company of Heroes 2 Launcher v0.1",
                    Dock = DockStyle.Top,
                    Height = 32,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                    ForeColor = Color.Gainsboro,
                    BackColor = PanelBg
                };

                fallbackPanel.Controls.Add(subtitle);
                fallbackPanel.Controls.Add(logo);
                header.Controls.Add(fallbackPanel);
            }

            main.Controls.Add(header, 0, 0);
        }

        private void BuildDetectionBox(TableLayoutPanel main)
        {
            Panel body;
            var section = MakeSection("Detection status", out body, bodyPadding: new Padding(12, 6, 12, 6));
            section.Margin = new Padding(0, 0, 0, 10);

            var statusGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = WindowBg
            };

            statusGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));

            lblSteamStatus = MakeStatusLabel("Steam: checking...");
            lblCoH2Status = MakeStatusLabel("CoH2: checking...");
            lblDocsStatus = MakeStatusLabel("Documents: checking...");

            var btnRefresh = MakeSmallButton("Refresh status");
            btnRefresh.Dock = DockStyle.Fill;
            btnRefresh.Margin = new Padding(8, 0, 4, 0);
            btnRefresh.Click += (_, _) => RefreshDetectionStatus();

            statusGrid.Controls.Add(lblSteamStatus, 0, 0);
            statusGrid.Controls.Add(lblCoH2Status, 1, 0);
            statusGrid.Controls.Add(lblDocsStatus, 2, 0);
            statusGrid.Controls.Add(btnRefresh, 3, 0);

            body.Controls.Add(statusGrid);
            main.Controls.Add(section, 0, 1);
        }

        private void BuildOptionsBox(TableLayoutPanel main)
        {
            Panel body;
            var section = MakeSection("Launch options", out body, bodyPadding: new Padding(12, 8, 12, 8));
            section.Margin = new Padding(0, 0, 0, 10);

            var optionsOuter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = WindowBg
            };

            optionsOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            optionsOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

            var leftOptions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = WindowBg,
                Padding = new Padding(8, 8, 4, 0)
            };

            chkSkipMovies = MakeCheckBox("Skip intro movies (-nomovies)");
            chkBorderless = MakeCheckBox("Borderless fullscreen (-window -fullwindow)");
            chkLockMouse = MakeCheckBox("Lock mouse to game window (-lockmouse)");
            chkNoVsyncTripleBuffer = MakeCheckBox("Disable VSync + Triple Buffer (-novsync -notriplebuffer)");
            chkForceActive = MakeCheckBox("Force active / focus fix (-forceactive)");
            chkDevMode = MakeCheckBox("Developer mode (-dev)");
            chkAllowIncompatibleReplays = MakeCheckBox("Allow incompatible replays (-allowincompatiblereplays)");

            leftOptions.Controls.Add(chkSkipMovies);
            leftOptions.Controls.Add(chkBorderless);
            leftOptions.Controls.Add(chkLockMouse);
            leftOptions.Controls.Add(chkNoVsyncTripleBuffer);
            leftOptions.Controls.Add(chkForceActive);
            leftOptions.Controls.Add(chkDevMode);
            leftOptions.Controls.Add(chkAllowIncompatibleReplays);

            var rightOptions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = WindowBg,
                Padding = new Padding(16, 10, 8, 0)
            };

            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            rightOptions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var profileLabel = new Label
            {
                Text = "Profile:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.WhiteSmoke,
                BackColor = WindowBg
            };

            cmbProfile = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };

            cmbProfile.Items.AddRange(new object[]
            {
                "Default Safe Modern Fix",
                "60Hz Safe Mode",
                "Borderless Only",
                "Dev Mode",
                "Replay Mode",
                "Custom"
            });

            cmbProfile.SelectedIndexChanged += (_, _) =>
            {
                if (applyingProfile)
                    return;

                string selected = cmbProfile.SelectedItem?.ToString() ?? "Custom";
                ApplyProfile(selected);
            };

            var refreshPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = WindowBg
            };

            var refreshLabel = new Label
            {
                Text = "Refresh rate:",
                AutoSize = false,
                Width = 100,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.WhiteSmoke,
                BackColor = WindowBg
            };

            cmbRefresh = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 145
            };

            cmbRefresh.Items.AddRange(new object[]
            {
                "Default",
                "60 Hz",
                "120 Hz",
                "144 Hz",
                "165 Hz",
                "240 Hz",
                "Custom"
            });

            cmbRefresh.SelectedIndex = 0;

            txtCustomRefresh = new TextBox
            {
                Width = 82,
                Enabled = false,
                PlaceholderText = "Hz",
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            refreshPanel.Controls.Add(refreshLabel);
            refreshPanel.Controls.Add(cmbRefresh);
            refreshPanel.Controls.Add(txtCustomRefresh);

            var extraLabel = new Label
            {
                Text = "Extra custom arguments:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.WhiteSmoke,
                BackColor = WindowBg
            };

            txtExtraArgs = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            var noteLabel = new Label
            {
                Text = "Profiles are presets. Manual changes switch to Custom.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Silver,
                BackColor = WindowBg
            };

            rightOptions.Controls.Add(profileLabel, 0, 0);
            rightOptions.Controls.Add(cmbProfile, 0, 1);
            rightOptions.Controls.Add(new Label { BackColor = WindowBg }, 0, 2);
            rightOptions.Controls.Add(refreshPanel, 0, 3);
            rightOptions.Controls.Add(new Label { BackColor = WindowBg }, 0, 4);
            rightOptions.Controls.Add(extraLabel, 0, 5);
            rightOptions.Controls.Add(txtExtraArgs, 0, 6);
            rightOptions.Controls.Add(noteLabel, 0, 7);

            optionsOuter.Controls.Add(leftOptions, 0, 0);
            optionsOuter.Controls.Add(rightOptions, 1, 0);

            body.Controls.Add(optionsOuter);
            main.Controls.Add(section, 0, 2);

            HookOptionEvents();
        }

        private void BuildButtonPanel(TableLayoutPanel main)
        {
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = WindowBg,
                Margin = new Padding(0, 0, 0, 10)
            };

            var btnLaunch = MakeButton("Launch CoH2");
            var btnSafe = MakeButton("Launch Safe Modern Fix");
            var btnCopy = MakeButton("Copy Steam Options");
            var btnDocs = MakeButton("Open Documents");
            var btnInstall = MakeButton("Open Install Folder");
            var btnReplays = MakeButton("Open Replays");
            var btnMods = MakeButton("Open Mods");
            var btnWorkshop = MakeButton("Open Workshop");
            var btnBackup = MakeButton("Backup Settings");

            btnLaunch.Click += (_, _) => LaunchCoH2(BuildArguments());

            btnSafe.Click += (_, _) =>
            {
                string safeArgs = "-nomovies -window -fullwindow -lockmouse -refresh 144 -novsync -notriplebuffer -forceactive";
                LaunchCoH2(safeArgs);
            };

            btnCopy.Click += (_, _) =>
            {
                Clipboard.SetText(BuildArguments());
                SetStatus("Steam launch options copied.");
            };

            btnDocs.Click += (_, _) => OpenDocumentsFolder();
            btnInstall.Click += (_, _) => OpenInstallFolder();
            btnReplays.Click += (_, _) => OpenReplaysFolder();
            btnMods.Click += (_, _) => OpenModsFolder();
            btnWorkshop.Click += (_, _) => OpenWorkshopFolder();
            btnBackup.Click += (_, _) => BackupCoH2Settings();

            buttons.Controls.Add(btnLaunch);
            buttons.Controls.Add(btnSafe);
            buttons.Controls.Add(btnCopy);
            buttons.Controls.Add(btnDocs);
            buttons.Controls.Add(btnInstall);
            buttons.Controls.Add(btnReplays);
            buttons.Controls.Add(btnMods);
            buttons.Controls.Add(btnWorkshop);
            buttons.Controls.Add(btnBackup);

            main.Controls.Add(buttons, 0, 3);
        }

        private void BuildPreviewBox(TableLayoutPanel main)
        {
            Panel body;
            var section = MakeSection("Current launch arguments", out body, bodyPadding: new Padding(12, 8, 12, 12));
            section.Margin = new Padding(0, 0, 0, 8);

            txtPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 36),
                ForeColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            body.Controls.Add(txtPreview);
            main.Controls.Add(section, 0, 4);
        }

        private void BuildFooter(TableLayoutPanel main)
        {
            lblStatus = new Label
            {
                Text = "Ready.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gainsboro,
                BackColor = WindowBg
            };

            main.Controls.Add(lblStatus, 0, 5);
        }

        private static Panel MakeSection(string title, out Panel body, Padding bodyPadding)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SoftBorder,
                Padding = new Padding(1)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = WindowBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                ForeColor = Color.WhiteSmoke,
                BackColor = SectionHeaderBg,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowBg,
                Padding = bodyPadding
            };

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(body, 0, 1);
            outer.Controls.Add(layout);

            return outer;
        }

        private void HookOptionEvents()
        {
            chkSkipMovies.CheckedChanged += (_, _) => ManualOptionChanged();
            chkBorderless.CheckedChanged += (_, _) => ManualOptionChanged();
            chkLockMouse.CheckedChanged += (_, _) => ManualOptionChanged();
            chkNoVsyncTripleBuffer.CheckedChanged += (_, _) => ManualOptionChanged();
            chkForceActive.CheckedChanged += (_, _) => ManualOptionChanged();
            chkDevMode.CheckedChanged += (_, _) => ManualOptionChanged();
            chkAllowIncompatibleReplays.CheckedChanged += (_, _) => ManualOptionChanged();

            cmbRefresh.SelectedIndexChanged += (_, _) => ManualOptionChanged();
            txtCustomRefresh.TextChanged += (_, _) => ManualOptionChanged();
            txtExtraArgs.TextChanged += (_, _) => ManualOptionChanged();
        }

        private void ManualOptionChanged()
        {
            if (!applyingProfile && cmbProfile != null)
            {
                applyingProfile = true;
                cmbProfile.SelectedItem = "Custom";
                applyingProfile = false;
            }

            UpdatePreview();
        }

        private void SelectProfile(string profileName)
        {
            applyingProfile = true;
            cmbProfile.SelectedItem = profileName;
            applyingProfile = false;

            ApplyProfile(profileName);
        }

        private void ApplyProfile(string profileName)
        {
            if (profileName == "Custom")
            {
                UpdatePreview();
                return;
            }

            applyingProfile = true;

            chkSkipMovies.Checked = false;
            chkBorderless.Checked = false;
            chkLockMouse.Checked = false;
            chkNoVsyncTripleBuffer.Checked = false;
            chkForceActive.Checked = false;
            chkDevMode.Checked = false;
            chkAllowIncompatibleReplays.Checked = false;
            cmbRefresh.SelectedItem = "Default";
            txtCustomRefresh.Text = "";
            txtExtraArgs.Text = "";

            switch (profileName)
            {
                case "Default Safe Modern Fix":
                    chkSkipMovies.Checked = true;
                    chkBorderless.Checked = true;
                    chkLockMouse.Checked = true;
                    chkNoVsyncTripleBuffer.Checked = true;
                    chkForceActive.Checked = true;
                    cmbRefresh.SelectedItem = "144 Hz";
                    break;

                case "60Hz Safe Mode":
                    chkSkipMovies.Checked = true;
                    chkBorderless.Checked = true;
                    chkLockMouse.Checked = true;
                    chkNoVsyncTripleBuffer.Checked = true;
                    chkForceActive.Checked = true;
                    cmbRefresh.SelectedItem = "60 Hz";
                    break;

                case "Borderless Only":
                    chkBorderless.Checked = true;
                    chkLockMouse.Checked = true;
                    break;

                case "Dev Mode":
                    chkSkipMovies.Checked = true;
                    chkDevMode.Checked = true;
                    break;

                case "Replay Mode":
                    chkSkipMovies.Checked = true;
                    chkAllowIncompatibleReplays.Checked = true;
                    break;
            }

            applyingProfile = false;
            UpdatePreview();
            SetStatus($"Profile selected: {profileName}");
        }

        private static Label MakeStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gainsboro,
                BackColor = WindowBg,
                AutoEllipsis = true
            };
        }

        private static CheckBox MakeCheckBox(string text)
        {
            return new CheckBox
            {
                Text = text,
                AutoSize = false,
                Width = 510,
                Height = 31,
                ForeColor = Color.WhiteSmoke,
                BackColor = WindowBg,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 4),
                UseVisualStyleBackColor = false
            };
        }

        private static Button MakeButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = false,
                Width = 170,
                Height = 36,
                Margin = new Padding(0, 0, 12, 8),
                BackColor = Color.FromArgb(28, 28, 35),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };

            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 220);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 42, 52);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(58, 58, 70);

            return btn;
        }

        private static Button MakeSmallButton(string text)
        {
            var btn = MakeButton(text);
            btn.Width = 150;
            btn.Height = 30;
            btn.Margin = new Padding(0);
            return btn;
        }

        private static Button MakeTitleButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 60,
                Height = 46,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = TopBarBg,
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TabStop = false
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 38, 48);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(56, 56, 68);

            return btn;
        }

        private string BuildArguments()
        {
            var args = new List<string>();

            if (chkSkipMovies.Checked)
                args.Add("-nomovies");

            if (chkBorderless.Checked)
            {
                args.Add("-window");
                args.Add("-fullwindow");
            }

            if (chkLockMouse.Checked)
                args.Add("-lockmouse");

            string refreshChoice = cmbRefresh.SelectedItem?.ToString() ?? "Default";

            if (refreshChoice != "Default")
            {
                string refreshValue = refreshChoice.Replace(" Hz", "").Trim();

                if (refreshChoice == "Custom")
                    refreshValue = txtCustomRefresh.Text.Trim();

                if (int.TryParse(refreshValue, out int hz) && hz > 0)
                {
                    args.Add("-refresh");
                    args.Add(hz.ToString());
                }
            }

            if (chkNoVsyncTripleBuffer.Checked)
            {
                args.Add("-novsync");
                args.Add("-notriplebuffer");
            }

            if (chkForceActive.Checked)
                args.Add("-forceactive");

            if (chkDevMode.Checked)
                args.Add("-dev");

            if (chkAllowIncompatibleReplays.Checked)
                args.Add("-allowincompatiblereplays");

            string extraArgs = txtExtraArgs.Text.Trim();

            if (!string.IsNullOrWhiteSpace(extraArgs))
                args.Add(extraArgs);

            return string.Join(" ", args);
        }

        private void UpdatePreview()
        {
            if (txtCustomRefresh != null && cmbRefresh != null)
                txtCustomRefresh.Enabled = cmbRefresh.SelectedItem?.ToString() == "Custom";

            if (txtPreview != null)
                txtPreview.Text = BuildArguments();
        }

        private void RefreshDetectionStatus()
        {
            string? steamExe = FindSteamExe();
            string? coh2Folder = FindCoH2InstallFolder();
            string docsFolder = GetCoH2DocumentsFolder();

            SetDetectionLabel(lblSteamStatus, "Steam", !string.IsNullOrWhiteSpace(steamExe) && File.Exists(steamExe), steamExe);
            SetDetectionLabel(lblCoH2Status, "CoH2", !string.IsNullOrWhiteSpace(coh2Folder) && Directory.Exists(coh2Folder), coh2Folder);
            SetDetectionLabel(lblDocsStatus, "Documents", Directory.Exists(docsFolder), docsFolder);

            SetStatus("Detection refreshed.");
        }

        private void SetDetectionLabel(Label label, string name, bool found, string? path)
        {
            if (found)
            {
                label.Text = $"{name}: Found";
                label.ForeColor = GoodGreen;
                label.Tag = path ?? "";
            }
            else
            {
                label.Text = $"{name}: Not found";
                label.ForeColor = WarnYellow;
                label.Tag = path ?? "";
            }
        }

        private void LaunchCoH2(string arguments)
        {
            try
            {
                string? steamExe = FindSteamExe();

                if (!string.IsNullOrWhiteSpace(steamExe) && File.Exists(steamExe))
                {
                    string launchArgs = $"-applaunch {AppId} {arguments}".Trim();

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = steamExe,
                        Arguments = launchArgs,
                        UseShellExecute = true
                    });

                    SetStatus($"Launching CoH2 with: {arguments}");
                    return;
                }

                string steamProtocol = "steam://run/" + AppId + "//" + arguments;

                Process.Start(new ProcessStartInfo
                {
                    FileName = steamProtocol,
                    UseShellExecute = true
                });

                SetStatus($"Launching CoH2 through Steam protocol with: {arguments}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not launch Company of Heroes 2.\n\n" + ex.Message,
                    AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                SetStatus("Launch failed.");
            }
        }

        private void OpenDocumentsFolder()
        {
            string folder = GetCoH2DocumentsFolder();
            OpenFolder(folder, createIfMissing: true, "CoH2 documents folder");
        }

        private void OpenReplaysFolder()
        {
            string folder = Path.Combine(GetCoH2DocumentsFolder(), "playback");
            OpenFolder(folder, createIfMissing: true, "CoH2 replays folder");
        }

        private void OpenModsFolder()
        {
            string folder = Path.Combine(GetCoH2DocumentsFolder(), "mods");
            OpenFolder(folder, createIfMissing: true, "CoH2 mods folder");
        }

        private void OpenInstallFolder()
        {
            string? folder = FindCoH2InstallFolder();

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(
                    "Could not find the Company of Heroes 2 install folder automatically.",
                    AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                SetStatus("Install folder not found.");
                return;
            }

            OpenFolder(folder, createIfMissing: false, "CoH2 install folder");
        }

        private void OpenWorkshopFolder()
        {
            string? exactFolder = FindCoH2WorkshopFolder();

            if (!string.IsNullOrWhiteSpace(exactFolder) && Directory.Exists(exactFolder))
            {
                OpenFolder(exactFolder, createIfMissing: false, "CoH2 Workshop folder");
                return;
            }

            string? preferredFolder = GetPreferredCoH2WorkshopFolder();
            string? contentFolder = FindWorkshopContentFolder();

            if (!string.IsNullOrWhiteSpace(preferredFolder))
            {
                DialogResult result = MessageBox.Show(
                    "The CoH2 Workshop folder was not found.\n\n" +
                    "This usually means Steam has not created it yet, or you have no CoH2 Workshop items downloaded.\n\n" +
                    "Expected folder:\n" + preferredFolder + "\n\n" +
                    "Yes = create/open CoH2 Workshop folder\n" +
                    "No = open the general Steam Workshop content folder instead\n" +
                    "Cancel = do nothing",
                    AppTitle,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Directory.CreateDirectory(preferredFolder);
                    OpenFolder(preferredFolder, createIfMissing: false, "CoH2 Workshop folder");
                    RefreshDetectionStatus();
                    return;
                }

                if (result == DialogResult.No)
                {
                    if (!string.IsNullOrWhiteSpace(contentFolder) && Directory.Exists(contentFolder))
                    {
                        OpenFolder(contentFolder, createIfMissing: false, "Steam Workshop content folder");
                        return;
                    }

                    string parent = Path.GetDirectoryName(preferredFolder) ?? preferredFolder;
                    Directory.CreateDirectory(parent);
                    OpenFolder(parent, createIfMissing: false, "Steam Workshop content folder");
                    return;
                }

                SetStatus("Workshop open cancelled.");
                return;
            }

            MessageBox.Show(
                "Could not find Steam or the Steam library folder automatically.",
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            SetStatus("Workshop folder not found.");
        }

        private void OpenFolder(string folder, bool createIfMissing, string displayName)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    if (!createIfMissing)
                    {
                        MessageBox.Show(
                            $"{displayName} was not found.",
                            AppTitle,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        SetStatus($"{displayName} not found.");
                        return;
                    }

                    DialogResult result = MessageBox.Show(
                        $"{displayName} was not found.\n\nCreate it now?",
                        AppTitle,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return;

                    Directory.CreateDirectory(folder);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + folder + "\"",
                    UseShellExecute = true
                });

                SetStatus($"Opened {displayName}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open {displayName}.\n\n{ex.Message}",
                    AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                SetStatus($"Could not open {displayName}.");
            }
        }

        private void BackupCoH2Settings()
        {
            try
            {
                string source = GetCoH2DocumentsFolder();

                if (!Directory.Exists(source))
                {
                    MessageBox.Show(
                        "The CoH2 documents folder was not found, so there is nothing to backup yet.",
                        AppTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    SetStatus("Backup failed: documents folder not found.");
                    return;
                }

                string parent = Directory.GetParent(source)?.FullName
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string destination = Path.Combine(parent, $"Company of Heroes 2 Backup - {stamp}");

                CopyDirectory(source, destination);

                MessageBox.Show(
                    "Backup created successfully.\n\n" +
                    "This creates a full backup folder, not a zip file.\n\n" +
                    "The backup is created next to your original Company of Heroes 2 folder.\n\n" +
                    "Original folder:\n" + source + "\n\n" +
                    "Backup folder:\n" + destination,
                    AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult openResult = MessageBox.Show(
                    "Do you want to open the backup folder now?\n\n" + destination,
                    AppTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (openResult == DialogResult.Yes)
                {
                    OpenFolder(destination, createIfMissing: false, "CoH2 backup folder");
                }
                else
                {
                    SetStatus("CoH2 settings backup created.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not backup CoH2 settings.\n\n" + ex.Message,
                    AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                SetStatus("Backup failed.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite: true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string targetDirectory = Path.Combine(destinationDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetDirectory);
            }
        }

        private static string GetCoH2DocumentsFolder()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return Path.Combine(
                docs,
                "My Games",
                "Company of Heroes 2");
        }

        private static string? FindSteamExe()
        {
            string? fromRegistry = ReadRegistryString(
                Registry.CurrentUser,
                @"Software\Valve\Steam",
                "SteamExe");

            if (!string.IsNullOrWhiteSpace(fromRegistry))
            {
                string fixedPath = fromRegistry.Replace('/', '\\');

                if (File.Exists(fixedPath))
                    return fixedPath;
            }

            string? steamPath = ReadRegistryString(
                Registry.CurrentUser,
                @"Software\Valve\Steam",
                "SteamPath");

            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                string possibleExe = Path.Combine(steamPath.Replace('/', '\\'), "Steam.exe");

                if (File.Exists(possibleExe))
                    return possibleExe;
            }

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam",
                "Steam.exe");

            if (File.Exists(defaultPath))
                return defaultPath;

            return null;
        }

        private static string? FindSteamPath()
        {
            string? steamPath = ReadRegistryString(
                Registry.CurrentUser,
                @"Software\Valve\Steam",
                "SteamPath");

            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                string fixedPath = steamPath.Replace('/', '\\');

                if (Directory.Exists(fixedPath))
                    return fixedPath;
            }

            string? steamExe = FindSteamExe();

            if (!string.IsNullOrWhiteSpace(steamExe) && File.Exists(steamExe))
                return Path.GetDirectoryName(steamExe);

            return null;
        }

        private static List<string> GetSteamLibraryPaths()
        {
            var paths = new List<string>();

            string? steamPath = FindSteamPath();

            if (string.IsNullOrWhiteSpace(steamPath))
                return paths;

            AddUniquePath(paths, steamPath);

            string libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryFile))
                return paths;

            try
            {
                foreach (string rawLine in File.ReadAllLines(libraryFile))
                {
                    string line = rawLine.Trim();

                    if (!line.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string? path = ExtractVdfValue(line);

                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    path = path.Replace(@"\\", @"\");

                    if (Directory.Exists(path))
                        AddUniquePath(paths, path);
                }
            }
            catch
            {
                return paths;
            }

            return paths;
        }

        private static void AddUniquePath(List<string> paths, string path)
        {
            foreach (string existing in paths)
            {
                if (string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            paths.Add(path);
        }

        private static string? ExtractVdfValue(string line)
        {
            string[] parts = line.Split('"');

            if (parts.Length >= 4)
                return parts[3];

            return null;
        }

        private static string? FindCoH2InstallFolder()
        {
            string? uninstallLocation = ReadRegistryString(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 231430",
                "InstallLocation");

            if (!string.IsNullOrWhiteSpace(uninstallLocation) && Directory.Exists(uninstallLocation))
                return uninstallLocation;

            foreach (string libraryPath in GetSteamLibraryPaths())
            {
                string possibleGamePath = Path.Combine(
                    libraryPath,
                    "steamapps",
                    "common",
                    "Company of Heroes 2");

                if (Directory.Exists(possibleGamePath))
                    return possibleGamePath;
            }

            return null;
        }

        private static string? FindCoH2WorkshopFolder()
        {
            foreach (string libraryPath in GetSteamLibraryPaths())
            {
                string possibleWorkshopPath = Path.Combine(
                    libraryPath,
                    "steamapps",
                    "workshop",
                    "content",
                    AppId);

                if (Directory.Exists(possibleWorkshopPath))
                    return possibleWorkshopPath;
            }

            return null;
        }

        private static string? FindWorkshopContentFolder()
        {
            foreach (string libraryPath in GetSteamLibraryPaths())
            {
                string possibleContentPath = Path.Combine(
                    libraryPath,
                    "steamapps",
                    "workshop",
                    "content");

                if (Directory.Exists(possibleContentPath))
                    return possibleContentPath;
            }

            return null;
        }

        private static string? GetPreferredCoH2WorkshopFolder()
        {
            string? installFolder = FindCoH2InstallFolder();
            List<string> libraryPaths = GetSteamLibraryPaths();

            if (!string.IsNullOrWhiteSpace(installFolder))
            {
                foreach (string libraryPath in libraryPaths)
                {
                    string commonPath = Path.Combine(libraryPath, "steamapps", "common");

                    if (installFolder.StartsWith(commonPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return Path.Combine(
                            libraryPath,
                            "steamapps",
                            "workshop",
                            "content",
                            AppId);
                    }
                }
            }

            if (libraryPaths.Count > 0)
            {
                return Path.Combine(
                    libraryPaths[0],
                    "steamapps",
                    "workshop",
                    "content",
                    AppId);
            }

            return null;
        }

        private static string? ReadRegistryString(RegistryKey root, string subKey, string valueName)
        {
            try
            {
                using RegistryKey? key = root.OpenSubKey(subKey);
                object? value = key?.GetValue(valueName);
                return value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static Image? LoadEmbeddedImage(string resourceName)
        {
            try
            {
                using Stream? stream = typeof(Form1).Assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                    return null;

                using Image loaded = Image.FromStream(stream);
                return new Bitmap(loaded);
            }
            catch
            {
                return null;
            }
        }

        private static Icon LoadWindowIcon()
        {
            try
            {
                using Stream? stream = typeof(Form1).Assembly.GetManifestResourceStream("K2.ico");

                if (stream != null)
                {
                    using Icon icon = new Icon(stream);
                    return (Icon)icon.Clone();
                }
            }
            catch
            {
                // fallback below
            }

            try
            {
                Icon? exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                if (exeIcon != null)
                    return exeIcon;
            }
            catch
            {
                // fallback below
            }

            return SystemIcons.Application;
        }

        private void SetStatus(string text)
        {
            if (lblStatus != null)
                lblStatus.Text = text;
        }

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                btnMaximize.Text = "□";
            }
            else
            {
                MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
                WindowState = FormWindowState.Maximized;
                btnMaximize.Text = "❐";
            }

            ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                Region = null;
                return;
            }

            IntPtr regionHandle = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 18, 18);
            Region = Region.FromHrgn(regionHandle);
            DeleteObject(regionHandle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (btnMaximize != null)
                btnMaximize.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";

            ApplyRoundedCorners();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);

                if ((int)m.Result == HTCLIENT)
                {
                    Point cursor = PointToClient(Cursor.Position);

                    bool left = cursor.X <= ResizeBorder;
                    bool right = cursor.X >= ClientSize.Width - ResizeBorder;
                    bool top = cursor.Y <= ResizeBorder;
                    bool bottom = cursor.Y >= ClientSize.Height - ResizeBorder;

                    if (left && top)
                    {
                        m.Result = (IntPtr)HTTOPLEFT;
                        return;
                    }

                    if (right && top)
                    {
                        m.Result = (IntPtr)HTTOPRIGHT;
                        return;
                    }

                    if (left && bottom)
                    {
                        m.Result = (IntPtr)HTBOTTOMLEFT;
                        return;
                    }

                    if (right && bottom)
                    {
                        m.Result = (IntPtr)HTBOTTOMRIGHT;
                        return;
                    }

                    if (left)
                    {
                        m.Result = (IntPtr)HTLEFT;
                        return;
                    }

                    if (right)
                    {
                        m.Result = (IntPtr)HTRIGHT;
                        return;
                    }

                    if (top)
                    {
                        m.Result = (IntPtr)HTTOP;
                        return;
                    }

                    if (bottom)
                    {
                        m.Result = (IntPtr)HTBOTTOM;
                        return;
                    }
                }

                return;
            }

            base.WndProc(ref m);
        }
    }
}