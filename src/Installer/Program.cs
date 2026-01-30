using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using K4os.Compression.LZ4;
using Microsoft.Win32;

namespace InterrogationRussianInstaller
{
    public class InstallerForm : Form
    {
        private Label titleLabel;
        private Label versionLabel;
        private Label pathLabel;
        private TextBox pathTextBox;
        private Button browseButton;
        private GroupBox infoGroup;
        private Label infoLabel;
        private Button installButton;
        private Button uninstallButton;
        private ProgressBar progressBar;
        private RichTextBox logBox;
        private Label statusLabel;
        private BackgroundWorker worker;

        private const string ModVersion = "1.1";
        private const string GameExeName = "Interrogation.exe";
        private const string GameFolderName = "Interrogation";

        // ====================================================================
        // Archive entry indices for translation files
        // ====================================================================
        private static readonly Dictionary<string, int> TranslationEntries = new Dictionary<string, int>()
        {
            ["campaign.ru.lua"] = 3418,
            ["chapter1.ru.lua"] = 804,
            ["chapter2.ru.lua"] = 1733,
            ["chapter3.ru.lua"] = 1450,
            ["credits.ru.lua"] = 954,
            ["cutscene1.ru.lua"] = 1120,
            ["episode8.ru.lua"] = 2303,
            ["fuior_main.ru.lua"] = 3648,
            ["interview1.ru.lua"] = 2494,
            ["interview2.ru.lua"] = 1832,
            ["interview3.ru.lua"] = 3051,
            ["level_clog.ru.lua"] = 333,
            ["level_episode0.ru.lua"] = 3583,
            ["level_episode1.ru.lua"] = 1554,
            ["level_episode10.ru.lua"] = 1436,
            ["level_episode2.ru.lua"] = 2350,
            ["level_episode3.ru.lua"] = 2642,
            ["level_episode4.ru.lua"] = 666,
            ["level_episode5.ru.lua"] = 2169,
            ["level_episode6.ru.lua"] = 1942,
            ["level_episode7.ru.lua"] = 3220,
            ["level_episode9.ru.lua"] = 1004,
            ["level_sitdown_fred.ru.lua"] = 2581,
            ["level_sitdown_informer1.ru.lua"] = 819,
            ["level_sitdown_informer2.ru.lua"] = 547,
            ["level_sitdown_joseph.ru.lua"] = 769,
            ["level_sitdown_marin.ru.lua"] = 971,
            ["level_test_aaron.ru.lua"] = 2136,
            ["level_test_actor.ru.lua"] = 2912,
            ["level_test_adams.ru.lua"] = 2107,
            ["level_test_alex.ru.lua"] = 506,
            ["level_test_amatis.ru.lua"] = 110,
            ["level_test_anaba.ru.lua"] = 1884,
            ["level_test_anton.ru.lua"] = 3249,
            ["level_test_bakil.ru.lua"] = 2271,
            ["level_test_daniel.ru.lua"] = 3741,
            ["level_test_dennis.ru.lua"] = 3420,
            ["level_test_diana.ru.lua"] = 973,
            ["level_test_fred.ru.lua"] = 2744,
            ["level_test_helene.ru.lua"] = 3469,
            ["level_test_insanity.ru.lua"] = 717,
            ["level_test_interpreter.ru.lua"] = 3054,
            ["level_test_james.ru.lua"] = 290,
            ["level_test_jerry.ru.lua"] = 2124,
            ["level_test_lucas.ru.lua"] = 193,
            ["level_test_lynda.ru.lua"] = 1820,
            ["level_test_maya.ru.lua"] = 1975,
            ["level_test_michael.ru.lua"] = 2168,
            ["level_test_peterson.ru.lua"] = 1388,
            ["level_test_reed.ru.lua"] = 2010,
            ["level_test_samantha.ru.lua"] = 1747,
            ["level_test_silvia.ru.lua"] = 256,
            ["level_test_steve.ru.lua"] = 1024,
            ["level_test_tab.ru.lua"] = 1775,
            ["level_test_timer.ru.lua"] = 2887,
            ["level_test_transcript.ru.lua"] = 2803,
            ["level_test_valerie.ru.lua"] = 1093,
            ["main.ru.lua"] = 3020,
            ["wall.ru.lua"] = 743,
        };

        // Archive entry indices for Cyrillic fonts (main)
        private static readonly Dictionary<int, string> FontEntries = new Dictionary<int, string>()
        {
            [1335] = "dialogue_merged.fontc",
            [414] = "dialogue_bold_merged.fontc",
            [1583] = "dialogue_italic_merged.fontc",
            [936] = "dialogue_bolditalic_merged.fontc",
            [779] = "dialogue_small_merged.fontc",
            [650] = "dialogue2_merged.fontc",
            [393] = "dialogue2_bold_merged.fontc",
            [2454] = "dialogue2_italic_merged.fontc",
            [167] = "dialogue2_bolditalic_merged.fontc",
            [2118] = "document_merged.fontc",
            [3255] = "document_bold_merged.fontc",
            [444] = "document_thin_merged.fontc",
            [1290] = "document_serif_merged.fontc",
            [2392] = "document_serif_bold_merged.fontc",
            [1874] = "document_serif_italic_merged.fontc",
            [1463] = "document_serif_bolditalic_merged.fontc",
            [2420] = "typewriter_merged.fontc",
            [2798] = "handwriting_merged.fontc",
            [2596] = "title_merged.fontc",
            [2246] = "title2_merged.fontc",
            [3444] = "title_bold_merged.fontc",
            [3641] = "title_bold2_merged.fontc",
        };

        // System font (builtins) — UI elements including menu button
        private const int SystemFontEntryIndex = 1855;
        private const string SystemFontName = "system_font.fontc";

        public InstallerForm()
        {
            InitializeComponents();
            string detected = DetectGamePath();
            if (detected != null)
            {
                pathTextBox.Text = detected;
                CheckInstalledState(detected);
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Interrogation \u2014 \u0420\u0443\u0441\u0438\u0444\u0438\u043A\u0430\u0442\u043E\u0440";
            this.Size = new Size(620, 560);
            this.MinimumSize = new Size(620, 560);
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(24, 24, 32);
            this.ForeColor = Color.FromArgb(220, 220, 230);

            // Title
            titleLabel = new Label
            {
                Text = "Interrogation \u2014 \u0420\u0443\u0441\u0438\u0444\u0438\u043A\u0430\u0442\u043E\u0440",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            versionLabel = new Label
            {
                Text = "\u0412\u0435\u0440\u0441\u0438\u044F " + ModVersion + "  |  Artem Lytkin (4RHT3CT0R)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 140, 160),
                AutoSize = true,
                Location = new Point(22, 52)
            };

            // Path selection
            pathLabel = new Label
            {
                Text = "\u041F\u0430\u043F\u043A\u0430 \u0441 \u0438\u0433\u0440\u043E\u0439 Interrogation:",
                AutoSize = true,
                Location = new Point(20, 85)
            };

            pathTextBox = new TextBox
            {
                Location = new Point(20, 105),
                Size = new Size(470, 24),
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle
            };

            browseButton = new Button
            {
                Text = "\u041E\u0431\u0437\u043E\u0440...",
                Location = new Point(500, 104),
                Size = new Size(85, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.FromArgb(220, 220, 230)
            };
            browseButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            browseButton.Click += BrowseButton_Click;

            // Info box
            infoGroup = new GroupBox
            {
                Text = "\u0411\u0443\u0434\u0443\u0442 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u044B",
                Location = new Point(20, 140),
                Size = new Size(565, 100),
                ForeColor = Color.FromArgb(180, 180, 200)
            };

            infoLabel = new Label
            {
                Text = "\u2022 60 \u0444\u0430\u0439\u043B\u043E\u0432 \u043F\u0435\u0440\u0435\u0432\u043E\u0434\u0430 (.ru.lua) \u2014 \u0434\u0438\u0430\u043B\u043E\u0433\u0438, \u044D\u043F\u0438\u0437\u043E\u0434\u044B, \u0434\u043E\u043F\u0440\u043E\u0441\u044B, \u043C\u0435\u043D\u044E\n" +
                       "\u2022 23 \u043A\u0438\u0440\u0438\u043B\u043B\u0438\u0447\u0435\u0441\u043A\u0438\u0445 \u0448\u0440\u0438\u0444\u0442\u0430 (.fontc) \u0432\u043A\u043B\u044E\u0447\u0430\u044F \u0441\u0438\u0441\u0442\u0435\u043C\u043D\u044B\u0439\n" +
                       "\u2022 LZ4-\u043F\u0430\u0442\u0447\u0438\u043D\u0433 \u0430\u0440\u0445\u0438\u0432\u0430 Defold Engine (game.arcd)\n" +
                       "\u2022 \u0410\u0432\u0442\u043E\u043C\u0430\u0442\u0438\u0447\u0435\u0441\u043A\u0438\u0439 \u0431\u044D\u043A\u0430\u043F \u043E\u0440\u0438\u0433\u0438\u043D\u0430\u043B\u044C\u043D\u044B\u0445 \u0444\u0430\u0439\u043B\u043E\u0432",
                Location = new Point(15, 22),
                Size = new Size(535, 70),
                ForeColor = Color.FromArgb(200, 200, 215)
            };
            infoGroup.Controls.Add(infoLabel);

            // Buttons
            installButton = new Button
            {
                Text = "\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u0438\u0442\u044C",
                Location = new Point(20, 252),
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 100, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            installButton.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 80);
            installButton.Click += InstallButton_Click;

            uninstallButton = new Button
            {
                Text = "\u0423\u0434\u0430\u043B\u0438\u0442\u044C",
                Location = new Point(190, 252),
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f)
            };
            uninstallButton.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 60);
            uninstallButton.Click += UninstallButton_Click;

            // Progress
            progressBar = new ProgressBar
            {
                Location = new Point(20, 300),
                Size = new Size(565, 20),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, 325),
                Size = new Size(565, 20),
                ForeColor = Color.FromArgb(160, 160, 180)
            };

            // Log
            logBox = new RichTextBox
            {
                Location = new Point(20, 350),
                Size = new Size(565, 155),
                ReadOnly = true,
                BackColor = Color.FromArgb(16, 16, 24),
                ForeColor = Color.FromArgb(180, 180, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Worker
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            this.Controls.AddRange(new Control[]
            {
                titleLabel, versionLabel, pathLabel, pathTextBox, browseButton,
                infoGroup, installButton, uninstallButton, progressBar,
                statusLabel, logBox
            });
        }

        // ========== UI HANDLERS ==========

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u043F\u0430\u043F\u043A\u0443 \u0441 \u0438\u0433\u0440\u043E\u0439 Interrogation";
                dialog.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(pathTextBox.Text) && Directory.Exists(pathTextBox.Text))
                    dialog.SelectedPath = pathTextBox.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    pathTextBox.Text = dialog.SelectedPath;
                    CheckInstalledState(dialog.SelectedPath);
                }
            }
        }

        private void CheckInstalledState(string gamePath)
        {
            string backupDir = Path.Combine(gamePath, "backup");
            bool installed = File.Exists(Path.Combine(backupDir, "game.arcd.original"));
            if (installed)
            {
                statusLabel.Text = "\u0421\u0442\u0430\u0442\u0443\u0441: \u043F\u0435\u0440\u0435\u0432\u043E\u0434 \u0443\u0436\u0435 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D";
                statusLabel.ForeColor = Color.FromArgb(100, 200, 120);
            }
            else
            {
                statusLabel.Text = "\u0421\u0442\u0430\u0442\u0443\u0441: \u043F\u0435\u0440\u0435\u0432\u043E\u0434 \u043D\u0435 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D";
                statusLabel.ForeColor = Color.FromArgb(200, 200, 100);
            }
        }

        private void Log(string message, Color? color = null)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => Log(message, color)));
                return;
            }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionColor = color ?? Color.FromArgb(180, 180, 200);
            logBox.AppendText(message + "\n");
            logBox.ScrollToCaret();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            installButton.Enabled = enabled;
            uninstallButton.Enabled = enabled;
            browseButton.Enabled = enabled;
            pathTextBox.Enabled = enabled;
        }

        // ========== INSTALL ==========

        private void InstallButton_Click(object sender, EventArgs e)
        {
            string gamePath = pathTextBox.Text.Trim().Trim('"');

            if (string.IsNullOrEmpty(gamePath))
            {
                MessageBox.Show(
                    "\u0423\u043A\u0430\u0436\u0438\u0442\u0435 \u043F\u0443\u0442\u044C \u043A \u043F\u0430\u043F\u043A\u0435 \u0441 \u0438\u0433\u0440\u043E\u0439.",
                    "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string exePath = Path.Combine(gamePath, GameExeName);
            if (!File.Exists(exePath))
            {
                MessageBox.Show(
                    $"\u0424\u0430\u0439\u043B \"{GameExeName}\" \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D \u0432:\n{gamePath}\n\n\u041F\u0440\u043E\u0432\u0435\u0440\u044C\u0442\u0435 \u043F\u0443\u0442\u044C \u0438 \u043F\u043E\u043F\u0440\u043E\u0431\u0443\u0439\u0442\u0435 \u0441\u043D\u043E\u0432\u0430.",
                    "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string arcdPath = Path.Combine(gamePath, "game.arcd");
            string arciPath = Path.Combine(gamePath, "game.arci");
            if (!File.Exists(arcdPath) || !File.Exists(arciPath))
            {
                MessageBox.Show(
                    "\u0424\u0430\u0439\u043B\u044B \u0430\u0440\u0445\u0438\u0432\u0430 (game.arcd / game.arci) \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D\u044B.\n\u0423\u0431\u0435\u0434\u0438\u0442\u0435\u0441\u044C, \u0447\u0442\u043E \u044D\u0442\u043E Steam-\u0432\u0435\u0440\u0441\u0438\u044F \u0438\u0433\u0440\u044B.",
                    "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var procs = Process.GetProcessesByName("Interrogation");
                if (procs.Length > 0)
                {
                    MessageBox.Show(
                        "\u0418\u0433\u0440\u0430 Interrogation \u0437\u0430\u043F\u0443\u0449\u0435\u043D\u0430.\n\u0417\u0430\u043A\u0440\u043E\u0439\u0442\u0435 \u0438\u0433\u0440\u0443 \u043F\u0435\u0440\u0435\u0434 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u043E\u0439.",
                        "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch { }

            logBox.Clear();
            SetButtonsEnabled(false);
            progressBar.Visible = true;
            progressBar.Value = 0;
            worker.RunWorkerAsync(new WorkerArgs { GamePath = gamePath, Mode = "install" });
        }

        // ========== UNINSTALL ==========

        private void UninstallButton_Click(object sender, EventArgs e)
        {
            string gamePath = pathTextBox.Text.Trim().Trim('"');

            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                MessageBox.Show(
                    "\u0423\u043A\u0430\u0436\u0438\u0442\u0435 \u043F\u0443\u0442\u044C \u043A \u043F\u0430\u043F\u043A\u0435 \u0441 \u0438\u0433\u0440\u043E\u0439.",
                    "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string backupDir = Path.Combine(gamePath, "backup");
            if (!Directory.Exists(backupDir))
            {
                MessageBox.Show(
                    "\u041F\u0430\u043F\u043A\u0430 backup \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D\u0430.\n\n\u0412\u044B \u043C\u043E\u0436\u0435\u0442\u0435 \u0432\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u0438\u0442\u044C \u0444\u0430\u0439\u043B\u044B \u0447\u0435\u0440\u0435\u0437 Steam:\n\u041F\u041A\u041C \u043D\u0430 \u0438\u0433\u0440\u0435 \u2192 \u0421\u0432\u043E\u0439\u0441\u0442\u0432\u0430 \u2192 \u041B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0435 \u0444\u0430\u0439\u043B\u044B \u2192 \u041F\u0440\u043E\u0432\u0435\u0440\u0438\u0442\u044C \u0446\u0435\u043B\u043E\u0441\u0442\u043D\u043E\u0441\u0442\u044C",
                    "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "\u0423\u0434\u0430\u043B\u0438\u0442\u044C \u0440\u0443\u0441\u0438\u0444\u0438\u043A\u0430\u0442\u043E\u0440?\n\n\u041E\u0440\u0438\u0433\u0438\u043D\u0430\u043B\u044C\u043D\u044B\u0435 \u0444\u0430\u0439\u043B\u044B game.arcd, game.arci, game.dmanifest\n\u0431\u0443\u0434\u0443\u0442 \u0432\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u044B \u0438\u0437 \u0440\u0435\u0437\u0435\u0440\u0432\u043D\u043E\u0439 \u043A\u043E\u043F\u0438\u0438.",
                "\u041F\u043E\u0434\u0442\u0432\u0435\u0440\u0436\u0434\u0435\u043D\u0438\u0435",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            logBox.Clear();
            SetButtonsEnabled(false);
            progressBar.Visible = true;
            progressBar.Value = 0;
            worker.RunWorkerAsync(new WorkerArgs { GamePath = gamePath, Mode = "uninstall" });
        }

        // ========== BACKGROUND WORKER ==========

        private class WorkerArgs
        {
            public string GamePath;
            public string Mode;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = (WorkerArgs)e.Argument;
            if (args.Mode == "install")
                DoInstall(args.GamePath);
            else
                DoUninstall(args.GamePath);
        }

        private void DoInstall(string gamePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(),
                "InterrogationRussian_" + Guid.NewGuid().ToString("N").Substring(0, 8));

            try
            {
                Directory.CreateDirectory(tempDir);

                // Step 1: Extract embedded data
                worker.ReportProgress(5, "\u0420\u0430\u0441\u043F\u0430\u043A\u043E\u0432\u043A\u0430 \u0434\u0430\u043D\u043D\u044B\u0445...");
                Log("[1/4] \u0420\u0430\u0441\u043F\u0430\u043A\u043E\u0432\u043A\u0430 \u0432\u0441\u0442\u0440\u043E\u0435\u043D\u043D\u044B\u0445 \u0434\u0430\u043D\u043D\u044B\u0445...");

                string zipTemp = Path.Combine(tempDir, "data.zip");
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("data.zip"))
                {
                    if (stream == null)
                    {
                        Log("\u041E\u0428\u0418\u0411\u041A\u0410: \u0432\u0441\u0442\u0440\u043E\u0435\u043D\u043D\u044B\u0435 \u0434\u0430\u043D\u043D\u044B\u0435 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D\u044B.", Color.Red);
                        return;
                    }
                    using (var file = File.Create(zipTemp))
                        stream.CopyTo(file);
                }

                ZipFile.ExtractToDirectory(zipTemp, tempDir);
                File.Delete(zipTemp);
                Log("   OK", Color.FromArgb(100, 200, 120));

                // Step 2: Create backup
                worker.ReportProgress(10, "\u0421\u043E\u0437\u0434\u0430\u043D\u0438\u0435 \u0440\u0435\u0437\u0435\u0440\u0432\u043D\u043E\u0439 \u043A\u043E\u043F\u0438\u0438...");
                Log("[2/4] \u0421\u043E\u0437\u0434\u0430\u043D\u0438\u0435 \u0440\u0435\u0437\u0435\u0440\u0432\u043D\u043E\u0439 \u043A\u043E\u043F\u0438\u0438...");

                string backupDir = Path.Combine(gamePath, "backup");
                Directory.CreateDirectory(backupDir);

                string[] backupFiles = { "game.arcd", "game.arci", "game.dmanifest" };
                foreach (var bf in backupFiles)
                {
                    string src = Path.Combine(gamePath, bf);
                    string dst = Path.Combine(backupDir, bf + ".original");
                    if (File.Exists(dst))
                    {
                        Log($"   {bf} \u2014 \u0431\u044D\u043A\u0430\u043F \u0443\u0436\u0435 \u0435\u0441\u0442\u044C");
                    }
                    else if (File.Exists(src))
                    {
                        File.Copy(src, dst);
                        Log($"   {bf} \u2014 \u0431\u044D\u043A\u0430\u043F \u0441\u043E\u0437\u0434\u0430\u043D", Color.FromArgb(100, 200, 120));
                    }
                    else
                    {
                        Log($"   {bf} \u2014 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D!", Color.FromArgb(255, 100, 100));
                        return;
                    }
                }

                string arcdPath = Path.Combine(gamePath, "game.arcd");
                string arciPath = Path.Combine(gamePath, "game.arci");

                // Step 3: Patch fonts
                worker.ReportProgress(20, "\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0448\u0440\u0438\u0444\u0442\u043E\u0432...");
                Log("[3/4] \u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u043A\u0438\u0440\u0438\u043B\u043B\u0438\u0447\u0435\u0441\u043A\u0438\u0445 \u0448\u0440\u0438\u0444\u0442\u043E\u0432...");

                int fontsDone = 0;
                int fontsTotal = FontEntries.Count + 1; // +1 for system font

                foreach (var kv in FontEntries.OrderBy(x => x.Key))
                {
                    string fontPath = Path.Combine(tempDir, "font_test", "main", "fonts", kv.Value);
                    if (!File.Exists(fontPath))
                    {
                        Log($"   [--] {kv.Value} \u2014 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D", Color.FromArgb(255, 200, 100));
                        continue;
                    }
                    byte[] data = File.ReadAllBytes(fontPath);
                    PatchEntry(arcdPath, arciPath, kv.Key, data);
                    fontsDone++;
                    int pct = 20 + (fontsDone * 20 / fontsTotal);
                    worker.ReportProgress(pct, $"\u0428\u0440\u0438\u0444\u0442\u044B: {fontsDone}/{fontsTotal}...");
                }

                // System font
                string sysFontPath = Path.Combine(tempDir, "font_test", "builtins", "fonts", SystemFontName);
                if (File.Exists(sysFontPath))
                {
                    byte[] data = File.ReadAllBytes(sysFontPath);
                    PatchEntry(arcdPath, arciPath, SystemFontEntryIndex, data);
                    fontsDone++;
                    Log($"   \u0428\u0440\u0438\u0444\u0442\u043E\u0432 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u043E: {fontsDone}", Color.FromArgb(100, 200, 120));
                }
                else
                {
                    Log($"   [--] {SystemFontName} (system) \u2014 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D", Color.FromArgb(255, 200, 100));
                }

                // Step 4: Patch translations
                worker.ReportProgress(45, "\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u043F\u0435\u0440\u0435\u0432\u043E\u0434\u043E\u0432...");
                Log("[4/4] \u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0444\u0430\u0439\u043B\u043E\u0432 \u043F\u0435\u0440\u0435\u0432\u043E\u0434\u0430...");

                int transDone = 0;
                int transTotal = TranslationEntries.Count;

                foreach (var kv in TranslationEntries.OrderBy(x => x.Key))
                {
                    string filePath = Path.Combine(tempDir, "translated", "intl", kv.Key);
                    if (!File.Exists(filePath))
                    {
                        Log($"   [--] {kv.Key} \u2014 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D", Color.FromArgb(255, 200, 100));
                        continue;
                    }
                    byte[] data = File.ReadAllBytes(filePath);
                    PatchEntry(arcdPath, arciPath, kv.Value, data);
                    transDone++;
                    int pct = 45 + (transDone * 50 / transTotal);
                    worker.ReportProgress(pct, $"\u041F\u0435\u0440\u0435\u0432\u043E\u0434\u044B: {transDone}/{transTotal}...");
                }

                Log($"   \u041F\u0435\u0440\u0435\u0432\u043E\u0434\u043E\u0432 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u043E: {transDone}", Color.FromArgb(100, 200, 120));

                // Done
                worker.ReportProgress(100, "\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043D\u0430!");
                Log("");
                Log($"\u0423\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0430 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043D\u0430! \u0428\u0440\u0438\u0444\u0442\u043E\u0432: {fontsDone}, \u043F\u0435\u0440\u0435\u0432\u043E\u0434\u043E\u0432: {transDone}", Color.FromArgb(100, 180, 255));
                Log("\u0417\u0430\u043F\u0443\u0441\u0442\u0438\u0442\u0435 \u0438\u0433\u0440\u0443 \u0438 \u0432\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u0440\u0443\u0441\u0441\u043A\u0438\u0439 \u044F\u0437\u044B\u043A \u0432 \u043D\u0430\u0441\u0442\u0440\u043E\u0439\u043A\u0430\u0445.");
            }
            catch (Exception ex)
            {
                Log("\u041E\u0428\u0418\u0411\u041A\u0410: " + ex.Message, Color.Red);
                worker.ReportProgress(100, "\u041E\u0448\u0438\u0431\u043A\u0430 \u0443\u0441\u0442\u0430\u043D\u043E\u0432\u043A\u0438");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }

        private void DoUninstall(string gamePath)
        {
            worker.ReportProgress(10, "\u0412\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u0438\u0435 \u043E\u0440\u0438\u0433\u0438\u043D\u0430\u043B\u043E\u0432...");
            Log("\u0412\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u0438\u0435 \u043E\u0440\u0438\u0433\u0438\u043D\u0430\u043B\u044C\u043D\u044B\u0445 \u0444\u0430\u0439\u043B\u043E\u0432...");

            string backupDir = Path.Combine(gamePath, "backup");
            int restored = 0;
            int errors = 0;

            var filesToRestore = new[]
            {
                ("game.arcd.original", "game.arcd"),
                ("game.arci.original", "game.arci"),
                ("game.dmanifest.original", "game.dmanifest"),
            };

            foreach (var (backupName, targetName) in filesToRestore)
            {
                string backupFile = Path.Combine(backupDir, backupName);
                string targetFile = Path.Combine(gamePath, targetName);

                if (!File.Exists(backupFile))
                {
                    Log($"   {backupName} \u2014 \u043D\u0435 \u043D\u0430\u0439\u0434\u0435\u043D", Color.FromArgb(255, 100, 100));
                    errors++;
                    continue;
                }

                try
                {
                    File.Copy(backupFile, targetFile, true);
                    Log($"   {targetName} \u2014 \u0432\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D", Color.FromArgb(100, 200, 120));
                    restored++;
                }
                catch (Exception ex)
                {
                    Log($"   {targetName} \u2014 \u043E\u0448\u0438\u0431\u043A\u0430: {ex.Message}", Color.FromArgb(255, 100, 100));
                    errors++;
                }

                worker.ReportProgress(10 + (restored + errors) * 30);
            }

            worker.ReportProgress(100, "\u0423\u0434\u0430\u043B\u0435\u043D\u0438\u0435 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043D\u043E");
            Log("");

            if (errors == 0)
                Log($"\u041F\u0435\u0440\u0435\u0432\u043E\u0434 \u0443\u0434\u0430\u043B\u0451\u043D. \u0412\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u043E: {restored} \u0444\u0430\u0439\u043B\u043E\u0432", Color.FromArgb(100, 180, 255));
            else
                Log("\u0412\u043E\u0441\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u0438\u0435 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043D\u043E \u0441 \u043E\u0448\u0438\u0431\u043A\u0430\u043C\u0438. \u041F\u0440\u043E\u0432\u0435\u0440\u044C\u0442\u0435 \u0446\u0435\u043B\u043E\u0441\u0442\u043D\u043E\u0441\u0442\u044C \u0447\u0435\u0440\u0435\u0437 Steam.", Color.FromArgb(255, 200, 100));
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = Math.Min(e.ProgressPercentage, 100);
            if (e.UserState is string msg)
                statusLabel.Text = msg;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetButtonsEnabled(true);
            CheckInstalledState(pathTextBox.Text.Trim().Trim('"'));
        }

        // ========== DEFOLD ARCHIVE PATCHING ==========

        private static void PatchEntry(string arcdPath, string arciPath, int entryIndex, byte[] newData)
        {
            // LZ4 high-compression, no frame/size prefix
            int maxLen = LZ4Codec.MaximumOutputSize(newData.Length);
            byte[] compBuf = new byte[maxLen];
            int compLen = LZ4Codec.Encode(newData, 0, newData.Length, compBuf, 0, maxLen, LZ4Level.L12_MAX);
            byte[] compressed = new byte[compLen];
            Array.Copy(compBuf, compressed, compLen);

            // Read archive index entry
            uint entryOffset;
            uint oldOffset, oldCompressed, oldFlags;

            using (var fs = new FileStream(arciPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] header = new byte[32];
                fs.Read(header, 0, 32);
                entryOffset = ReadBE32(header, 20);

                fs.Seek(entryOffset + entryIndex * 16, SeekOrigin.Begin);
                byte[] entry = new byte[16];
                fs.Read(entry, 0, 16);

                oldOffset = ReadBE32(entry, 0);
                oldCompressed = ReadBE32(entry, 8);
                oldFlags = ReadBE32(entry, 12);
            }

            uint newOffset;

            if ((uint)compressed.Length <= oldCompressed)
            {
                // Fits in original slot
                using (var fs = new FileStream(arcdPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.Seek(oldOffset, SeekOrigin.Begin);
                    fs.Write(compressed, 0, compressed.Length);
                    int pad = (int)(oldCompressed - (uint)compressed.Length);
                    if (pad > 0)
                        fs.Write(new byte[pad], 0, pad);
                }
                newOffset = oldOffset;
            }
            else
            {
                // Append to end of archive
                long archiveSize = new FileInfo(arcdPath).Length;
                int padding = (int)((4 - (archiveSize % 4)) % 4);
                newOffset = (uint)(archiveSize + padding);

                using (var fs = new FileStream(arcdPath, FileMode.Append, FileAccess.Write))
                {
                    if (padding > 0)
                        fs.Write(new byte[padding], 0, padding);
                    fs.Write(compressed, 0, compressed.Length);
                }
            }

            // Update index entry
            using (var fs = new FileStream(arciPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(entryOffset + entryIndex * 16, SeekOrigin.Begin);
                fs.Write(WriteBE32(newOffset), 0, 4);
                fs.Write(WriteBE32((uint)newData.Length), 0, 4);
                fs.Write(WriteBE32((uint)compressed.Length), 0, 4);
                fs.Write(WriteBE32(oldFlags), 0, 4);
            }
        }

        private static uint ReadBE32(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                          (data[offset + 2] << 8) | data[offset + 3]);
        }

        private static byte[] WriteBE32(uint value)
        {
            return new byte[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
        }

        // ========== GAME PATH DETECTION ==========

        private static string DetectGamePath()
        {
            // Method 1: Common Steam library paths
            string[] drives = { "C", "D", "E", "F", "G", "H" };
            string[] prefixes =
            {
                @":\Program Files (x86)\Steam\steamapps\common\" + GameFolderName,
                @":\Program Files\Steam\steamapps\common\" + GameFolderName,
                @":\Steam\steamapps\common\" + GameFolderName,
                @":\SteamLibrary\steamapps\common\" + GameFolderName,
                @":\Games\Steam\steamapps\common\" + GameFolderName,
                @":\Games\SteamLibrary\steamapps\common\" + GameFolderName,
            };

            foreach (var drive in drives)
            {
                foreach (var prefix in prefixes)
                {
                    string path = drive + prefix;
                    if (File.Exists(Path.Combine(path, GameExeName)))
                        return path;
                }
            }

            // Method 2: Steam registry + library folders
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string steamPath = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(steamPath))
                        {
                            steamPath = steamPath.Replace("/", "\\");

                            string mainLib = Path.Combine(steamPath, "steamapps", "common", GameFolderName);
                            if (File.Exists(Path.Combine(mainLib, GameExeName)))
                                return mainLib;

                            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                            if (File.Exists(vdfPath))
                            {
                                string vdf = File.ReadAllText(vdfPath);
                                int idx = 0;
                                while (true)
                                {
                                    idx = vdf.IndexOf("\"path\"", idx);
                                    if (idx < 0) break;
                                    int q1 = vdf.IndexOf("\"", idx + 6);
                                    if (q1 < 0) break;
                                    int q2 = vdf.IndexOf("\"", q1 + 1);
                                    if (q2 < 0) break;
                                    string libPath = vdf.Substring(q1 + 1, q2 - q1 - 1).Replace("\\\\", "\\");
                                    string check = Path.Combine(libPath, "steamapps", "common", GameFolderName);
                                    if (File.Exists(Path.Combine(check, GameExeName)))
                                        return check;
                                    idx = q2 + 1;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // Method 3: Installer next to game
            string selfDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (File.Exists(Path.Combine(selfDir, GameExeName)))
                return selfDir;

            return null;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
