using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using WMPLib;

#if DEBUG
#pragma warning disable IDE1006 // Naming Styles violation intended (VERYUNCOOLSpawnMoreRalseisExtremeRiskPleaseGodHelpThisUsersPCBeforeItAbsolutelyFuckingDisintegratesIntoEndlessOblivionAndProbablyMaybeMostDefinitelyWillJustFuckingDieYeahIdAssumeThatYeahIllJustPrayForYouThatYouDontExplodeMaybeDontThinkThatllMakeYourChanceOfSurvivingAnyBiggerThoughLmaoButUhhhIAmDefinitelyOneHundredPercentNoScamNotResponsibleForYourDeathNeitherWillIAttendYourFuneralIfYouDieOkay)...
#endif

namespace WinRalseiShimeji
{
    public static class RalseiManager
    {
        public static List<RalseiDoobie> RalseiInstances = new List<RalseiDoobie>();
        public static bool MusicIsPlaying = false;
    }

    public class CustomMessageBox : Form
    {
        public bool DontShowAgainChecked { get; private set; }
        public string ClickedButtonText { get; private set; }
        private Label labelMsg;
        private CheckBox chkDontShowAgain;
        private Button btnOK;
        private TextBox inputBox;
        private List<Button> dynamicButtons = new List<Button>();

        public CustomMessageBox(string message, string title, params object[] buttons)
        {
            // If buttons is null, set up a safety guard
            if (buttons == null)
            {
                buttons = new object[0];
            }

            // Form properties
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;
            this.AutoSize = true;

            // Message label
            labelMsg = new Label()
            {
                Text = message,
                Location = new Point(15, 20),
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(labelMsg);

            int verticalPosition = labelMsg.Bottom + 10;

            // Input box
            if (buttons.Contains((object)3) || buttons.Contains("INPUT"))
            {
                inputBox = new TextBox()
                {
                    Location = new Point(15, verticalPosition),
                    Size = new Size(290, 32),
                    Text = ""
                };
                this.Controls.Add(inputBox);
                verticalPosition = inputBox.Bottom + 10;
            }

            // OK button
            if (buttons.Contains((object)1) || buttons.Contains("OK"))
            {
                btnOK = new Button()
                {
                    Text = "OK",
                    Size = new Size(75, 30),
                    Location = new Point(60, verticalPosition)
                };
                btnOK.Click += BtnOK_Click;
                btnOK.Click += (s, e) =>
                {
                    ClickedButtonText = btnOK.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnOK);
            }

            if (buttons.Contains((object)2) || buttons.Contains("DSA"))
            {
                chkDontShowAgain = new CheckBox()
                {
                    Text = "Don't Show Again",
                    Size = new Size(140, 30),
                };
                int chkX = btnOK != null ? btnOK.Right + 10 : 60;
                int chkY = btnOK != null ? btnOK.Top : verticalPosition;
                chkDontShowAgain.Location = new Point(chkX, chkY);
                this.Controls.Add(chkDontShowAgain);

                // If checkbox is below input and no OK, advance verticalPosition to avoid overlapping dynamic buttons.
                if (btnOK == null)
                    verticalPosition = Math.Max(verticalPosition, chkDontShowAgain.Bottom + 10);
            }

            var stringButtons = buttons.OfType<string>().Where(s => s != "OK" && s != "DSA" && s != "INPUT");
            int currentX = 60;
            int margin = 10;
            int buttonRowY = (btnOK != null) ? btnOK.Top : verticalPosition;
            foreach (var btnLabel in stringButtons)
            {
                int width = Math.Max(75, TextRenderer.MeasureText(btnLabel, this.Font).Width + 20);
                var btnExt = new Button()
                {
                    Text = btnLabel,
                    Size = new Size(width, 30),
                    Location = new Point(currentX, buttonRowY),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseCompatibleTextRendering = true,
                };
                btnExt.Click += (s, e) =>
                {
                    ClickedButtonText = btnExt.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };

                this.Controls.Add(btnExt);
                dynamicButtons.Add(btnExt);

                currentX += width + margin;
            }

            // Finally, resize the form height to fit the lowest control + padding
            int maxBottom = labelMsg.Bottom;
            if (inputBox != null) maxBottom = Math.Max(maxBottom, inputBox.Bottom);
            if (btnOK != null) maxBottom = Math.Max(maxBottom, btnOK.Bottom);
            if (chkDontShowAgain != null) maxBottom = Math.Max(maxBottom, chkDontShowAgain.Bottom);
            foreach (var b in dynamicButtons)
                maxBottom = Math.Max(maxBottom, b.Bottom);

            this.ClientSize = new Size(
                Math.Min(360, Screen.PrimaryScreen.WorkingArea.Width - 40),
                maxBottom + 20
            );
        }
        public string UserInput => inputBox?.Text ?? string.Empty;

        private void BtnOK_Click(object sender, EventArgs e)
        {
            DontShowAgainChecked = chkDontShowAgain?.Checked ?? false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Note: VERY IMPORTANT FOR CAPTURING KEY INPUTS!!! ;P
            this.KeyPreview = true;
            if (keyData == (Keys.Control | Keys.C))
            {
                // Build the text that will be copied to clipboard similar to standard MessageBox format

                string clipText = $"---------------------------------\r\n{this.Text}\r\n---------------------------------\r\n{labelMsg.Text}\r\n---------------------------------\r\n";

                var buttonsText = new List<string>();
                if (btnOK != null)
                    buttonsText.Add(btnOK.Text);
                buttonsText.AddRange(dynamicButtons.Select(b => b.Text));

                foreach (var buttonText in buttonsText)
                {
                    clipText += string.Join("   ", buttonsText) + "\r\n---------------------------------\r\n";
                }

                Clipboard.SetText(clipText);
                return true; // Indicate key was handled
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public partial class RalseiDoobie : Form
    {
        private int formWidth;
        private int formHeight;
        private readonly Timer timer;
        private int dx = 6, dy = 4; // Movement speed
        private float speedMultiplier = 1.0f;
        private readonly Image spriteImage;
        private int ralseiWidth;
        private int ralseiHeight;
        private bool ralseiIsFollowingMouse;
        private bool ralseiWasLastFollowingMouseTracker;
        private WindowsMediaPlayer wplayer;
        private List<SmokeParticle> smokeParticles = new List<SmokeParticle>();
        private float ralseiScale = 2.0f;
        private bool ralseiUndragDSA = false; // DSA stands for 'Don't Show Again', if you were wondering.
        private ContextMenuStrip ralseiMenu;
        private readonly static List<RalseiDoobie> ralseiObj;
        private Point lastMousePos;
        private DateTime lastMouseTime;
        private int Ddx = 0, Ddy = 0;
        private bool dragging = false;
        private bool draggable = false;
        private float velocityDamping = 1.0f;
        private double velocityX = 0.0, velocityY = 0.0;
        private double accelerationStrength = 3.0;
        private Point mouseOffset;
        private Queue<(Point pos, DateTime time)> mousePositions = new Queue<(Point, DateTime)>();
        private readonly static Random rand = new Random();
        private Image COOLpuffImage;
        private Timer COOLpuffTimer;
        private Image UNCOOLpuffImage;
        private Timer UNCOOLpuffTimer;
        private float COOLpuffWidth;
        private float COOLpuffHeight;
        private float UNCOOLpuffWidth;
        private float UNCOOLpuffHeight;
        private int frameCount = 0;
        private readonly Stopwatch fpsTimer = new Stopwatch();
        private Random random = new Random();
        private static bool VERYUNCOOLSpawnMoreRalseisExtremeRiskPleaseGodHelpThisUsersPCBeforeItAbsolutelyFuckingDisintegratesIntoEndlessOblivionAndProbablyMaybeMostDefinitelyWillJustFuckingDieYeahIdAssumeThatYeahIllJustPrayForYouThatYouDontExplodeMaybeDontThinkThatllMakeYourChanceOfSurvivingAnyBiggerThoughLmaoButUhhhIAmDefinitelyOneHundredPercentNoScamNotResponsibleForYourDeathNeitherWillIAttendYourFuneralIfYouDieOkay
            = false;
        #if DEBUG
        #pragma warning restore IDE1006 // Ehh... it can chill now. :3
        #endif
        private readonly static string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        private readonly static string assetPath = Path.Combine(
            userProfile,
            "Downloads",
            "Ralsei_Shimeji",
            "Ralsei_Shimeji",
            "assets ;p"
        );
        private string musicUrl = Path.Combine(assetPath, "dump.mp3");
        private bool isUsingCustomMusic = false;
        private bool suppressCustomMusicPromptOnce = false;

        private readonly int TBOK = 1;
        private readonly int TBDSA = 2;
        private readonly int TBInput = 3;

        public enum PuffType
        {
            Cool,
            Uncool
        }

        public class SmokeParticle
        {
            public PointF Position;
            public float Alpha = 1.0f;
            public float RiseSpeed = 5.0f;
            public Image PuffImage;
            public PuffType Type;

            public SmokeParticle(PointF startPos, Image image, PuffType type)
            {
                Position = startPos;
                RiseSpeed = 0.5f + (float)rand.NextDouble();
                PuffImage = image;
                Type = type;
            }

            public void Update()
            {
                Position = new PointF(Position.X, Position.Y - RiseSpeed);
                Alpha -= 0.01f;
            }

            public bool IsDead => Alpha <= 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (draggable && e.Button == MouseButtons.Left)
            {
                dragging = true;
                mouseOffset = new Point(e.X, e.Y);
                lastMousePos = PointToScreen(e.Location);
                lastMouseTime = DateTime.Now;
                Ddx = 0;
                Ddy = 0;
                timer.Stop();  // stop movement while dragging
            }
            else if (e.Button == MouseButtons.Right)
            {
                ralseiMenu.Show(this, e.Location);
            }
        }

        public RalseiDoobie()
        {
            // Set up the form.
            this.FormBorderStyle = FormBorderStyle.None; // No border.
            this.BackColor = Color.LightPink;
            this.TransparencyKey = Color.LightPink; // Make the background invisible.
            this.TopMost = true; // This makes the form always be on top.
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;

            // Note: VERY IMPORTANT FOR CAPTURING KEY INPUTS!!! ;P
            this.KeyPreview = true;

            // Some screen size necessities.
            formWidth = this.ClientSize.Width;
            formHeight = this.ClientSize.Height;

            // Load the sprite.
            spriteImage = Image.FromFile(Path.Combine(assetPath, "ralsei_doobieee.png"));
            ralseiWidth = (int)(spriteImage.Width * ralseiScale);
            ralseiHeight = (int)(spriteImage.Height * ralseiScale);
            COOLpuffImage = Image.FromFile(Path.Combine(assetPath, "smokerson_COOL.png"));
            UNCOOLpuffImage = Image.FromFile(Path.Combine(assetPath, "smokerson_UNCOOL.png"));
            float COOLpuffWidth = COOLpuffImage.Width * 0.6f * ralseiScale; // puff base scale (0.6f) * ralseiScale
            float COOLpuffHeight = COOLpuffImage.Height * 0.6f * ralseiScale;
            float UNCOOLpuffWidth = UNCOOLpuffImage.Width * 0.6f * ralseiScale; // puff base scale (0.6f) * ralseiScale
            float UNCOOLpuffHeight = UNCOOLpuffImage.Height * 0.6f * ralseiScale;

            this.Width = (int)(spriteImage.Width * ralseiScale);
            this.Height = (int)(spriteImage.Height * ralseiScale);

            this.Left = random.Next(0, Screen.PrimaryScreen.Bounds.Width - this.Width);
            this.Top = random.Next(0, Screen.PrimaryScreen.Bounds.Height - this.Height);

            ralseiMenu = new ContextMenuStrip();

            ralseiMenu.Items.Add("Scale: Tiny", null, (s, e) => { ralseiScale = 0.5f; ResizeRalsei(); });
            ralseiMenu.Items.Add("Scale: Normal", null, (s, e) => { ralseiScale = 1.0f; ResizeRalsei(); });
            ralseiMenu.Items.Add("Scale: Chonky", null, (s, e) => { ralseiScale = 2.0f; ResizeRalsei(); });
            ralseiMenu.Items.Add("Scale: Custom", null, (s, e) =>
            {
                using (var msgBox = new CustomMessageBox("Enter scale (recommendation: Below 3, scale > 0):", "I wonder how big I'll be... ([[BIG SHOT]]!!)", TBInput, TBOK))
                {
                    DialogResult result = msgBox.ShowDialog();
                    if (result == DialogResult.OK && string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        DialogResult res = MessageBox.Show("You didn't input anything, please try again!!", "Input something, silly!! ;p", MessageBoxButtons.OK);
                        if (res == DialogResult.OK)
                        {
                            ralseiMenu.Close();
                        }
                    }
                    else if (result == DialogResult.OK && !string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        if (float.TryParse(msgBox.UserInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float customScale))
                        {
                            if (customScale > 0 && customScale < 3)
                            {
                                ralseiScale = customScale;
                                ResizeRalsei();
                            }
                            else if (customScale >= 3)
                            {
                                DialogResult _res = MessageBox.Show(
                                    "Are you very sure that you want to oversize Ralsei? This might slow down your computer or break the process.",
                                    "YOU CAN BE A [[BIG SHOT]] NOW!!! SO [[Proud Of Its $9.99 Life]]!!",
                                    MessageBoxButtons.YesNo);
                                if (_res == DialogResult.Yes)
                                {
                                    ralseiScale = customScale;
                                    ResizeRalsei();
                                }
                                else if (_res == DialogResult.No)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                            else
                            {
                                DialogResult res = MessageBox.Show("Please insert a valid number, please try again!!", "That's too small... :(", MessageBoxButtons.OK);
                                if (res == DialogResult.OK)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                        }
                        else
                        {
                            DialogResult res = MessageBox.Show("Your input wasn't a valid number, please try again!!", "Actually choose something valid, silly... ;p", MessageBoxButtons.OK);
                            if (res == DialogResult.OK)
                            {
                                ralseiMenu.Close();
                            }
                        }
                    }

                }
            });

            ralseiMenu.Items.Add(new ToolStripSeparator());

            ralseiMenu.Items.Add("Speed: Slow", null, (s, e) => { speedMultiplier = 0.5f; });
            ralseiMenu.Items.Add("Speed: Normal", null, (s, e) => { speedMultiplier = 1.0f; });
            ralseiMenu.Items.Add("Speed: Zoom", null, (s, e) => { speedMultiplier = 2.0f; });
            ralseiMenu.Items.Add("Speed: Custom", null, (s, e) => {
                using (var msgBox = new CustomMessageBox("Enter speed (recommendation: Below 5, scale > 0):", "WANT TO GET THOSE [[Speedos]] [[Pipi-Speedy]]??", TBInput, TBOK))
                {
                    DialogResult result = msgBox.ShowDialog();
                    if (result == DialogResult.OK && string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        DialogResult res = MessageBox.Show("You didn't input anything, please try again!!", "Input something, silly!! ;p", MessageBoxButtons.OK);
                        if (res == DialogResult.OK)
                        {
                            ralseiMenu.Close();
                        }
                    }
                    else if (result == DialogResult.OK && !string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        if (float.TryParse(msgBox.UserInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float customSpeed))
                        {
                            if (customSpeed > 0 && customSpeed < 5)
                            {
                                speedMultiplier = (float)customSpeed;
                            }
                            else if (customSpeed >= 5)
                            {
                                DialogResult _res = MessageBox.Show(
                                    "Are you very sure that you want to make Ralsei go SuperSonic™? This might slow down your computer or break the process.",
                                    "YOU CAN BE A [[Pipi-Speedos]] NOW!!! SO [[Proud Of Its $9.99 Life]]!!",
                                    MessageBoxButtons.YesNo);
                                if (_res == DialogResult.Yes)
                                {
                                    speedMultiplier = (float)customSpeed;
                                }
                                else if (_res == DialogResult.No)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                            else
                            {
                                DialogResult res = MessageBox.Show("Please insert a valid number, please try again!!", "That's too small... :(", MessageBoxButtons.OK);
                                if (res == DialogResult.OK)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                        }
                        else
                        {
                            DialogResult res = MessageBox.Show("Your input wasn't a valid number, please try again!!", "Actually choose something valid, silly... ;p", MessageBoxButtons.OK);
                            if (res == DialogResult.OK)
                            {
                                ralseiMenu.Close();
                            }
                        }
                    }

                }
            });

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var musicPlayMenuItem = new ToolStripMenuItem("Play Music")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.M,
                ShortcutKeyDisplayString = "Ctrl + Shift + M"
            };
            musicPlayMenuItem.Click += (s, e) => { PlayMusic(); };
            ralseiMenu.Items.Add(musicPlayMenuItem);
            /////////////////////////////////////////////////////////////
            var musicStopMenuItem = new ToolStripMenuItem("Stop Music")
            {
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.M,
                ShortcutKeyDisplayString = "Ctrl + Alt + M"
            };
            musicStopMenuItem.Click += (s, e) => { StopMusic(); };
            ralseiMenu.Items.Add(musicStopMenuItem);
            /////////////////////////////////////////////////////////////
            var customMusicMenuItem = new ToolStripMenuItem("Play Custom Music")
            {
                ShortcutKeys = Keys.Control | Keys.C | Keys.M,
                ShortcutKeyDisplayString = "Ctrl + C + M"
            };
            customMusicMenuItem.Click += (s, e) => { CustomMusic(); };
            ralseiMenu.Items.Add(customMusicMenuItem);

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var trackMouseMenuItem = new ToolStripMenuItem("Make Ralsei Track The Mouse")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.T,
                ShortcutKeyDisplayString = "Ctrl + Shift + T"
            };
            trackMouseMenuItem.Click += (s, e) => { ralseiIsFollowingMouse = true; };
            ralseiMenu.Items.Add(trackMouseMenuItem);
            ////////////////////////////////////////////////////////////////////////////////
            var untrackMouseMenuItem = new ToolStripMenuItem("Make Ralsei Un-track The Mouse")
            {
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.T,
                ShortcutKeyDisplayString = "Ctrl + Alt + T"
            };
            untrackMouseMenuItem.Click += (s, e) => { ralseiIsFollowingMouse = false; };
            ralseiMenu.Items.Add(untrackMouseMenuItem);
            //////////////////////////////////////////////////////////////////////////////
            var setCSpeedMouseTrackingMenuItem = new ToolStripMenuItem("Set Custom Tracking Speed")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.I,
                ShortcutKeyDisplayString = "Ctrl + Shift + I"
            }
            ;
            setCSpeedMouseTrackingMenuItem.Click += (s, e) => { // Yes, I know that I just copied this from Ralsei's movement speed menu item... o(╥﹏╥)o BUT, IT WORKS, IF IT WORKS, IT WORKS!!!!!!!!
                using (var msgBox = new CustomMessageBox("Enter speed (recommendation: Below 7-10, scale > 0):", "WANT TO GET THOSE [[Speedos]] [[Pipi-Speedy]]??", TBInput, TBOK))
                {
                    DialogResult result = msgBox.ShowDialog();
                    if (result == DialogResult.OK && string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        DialogResult res = MessageBox.Show("You didn't input anything, please try again!!", "Input something, silly!! ;p", MessageBoxButtons.OK);
                        if (res == DialogResult.OK)
                        {
                            ralseiMenu.Close();
                        }
                    }
                    else if (result == DialogResult.OK && !string.IsNullOrEmpty(msgBox.UserInput))
                    {
                        if (double.TryParse(msgBox.UserInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double customSpeed))
                        {
                            if (customSpeed > 0 && customSpeed < 7)
                            {
                                accelerationStrength = (double)customSpeed;
                            }
                            else if (customSpeed >= 7)
                            {
                                DialogResult _res = MessageBox.Show(
                                    "Are you very sure that you want to make Ralsei go SuperSonic™? This might slow down your computer or break the process.",
                                    "YOU CAN BE A [[Pipi-Speedos]] NOW!!! SO [[Proud Of Its $9.99 Life]]!!",
                                    MessageBoxButtons.YesNo);
                                if (_res == DialogResult.Yes)
                                {
                                    accelerationStrength = (double)customSpeed;
                                }
                                else if (_res == DialogResult.No)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                            else
                            {
                                DialogResult res = MessageBox.Show("Please insert a valid number, please try again!!", "That's too small... :(", MessageBoxButtons.OK);
                                if (res == DialogResult.OK)
                                {
                                    ralseiMenu.Close();
                                }
                            }
                        }
                        else
                        {
                            DialogResult res = MessageBox.Show("Your input wasn't a valid number, please try again!!", "Actually choose something valid, silly... ;p", MessageBoxButtons.OK);
                            if (res == DialogResult.OK)
                            {
                                ralseiMenu.Close();
                            }
                        }
                    }

                }
            };
            ralseiMenu.Items.Add(setCSpeedMouseTrackingMenuItem);

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var ralsDraggableMenuItem = new ToolStripMenuItem("Make Ralsei Draggable")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.D,
                ShortcutKeyDisplayString = "Ctrl + Shift + D"
            };
            ralsDraggableMenuItem.Click += (s, e) => { draggable = true; MessageBox.Show("Toss me around as much as you want! " +
                "I would be lying if I were to say that I don't enjoy some action...", "Message From Doobie Ralsei", MessageBoxButtons.OK); };
            ralseiMenu.Items.Add(ralsDraggableMenuItem);
            ////////////////////////////////////////////////////////////////////////////////
            var ralsUndraggableMenuItem = new ToolStripMenuItem("Make Ralsei Undraggable")
            {
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.D
            };
            ralsUndraggableMenuItem.Click += (s, e) => {
                draggable = false;
                if (!timer.Enabled)
                {
                    timer.Start();
                }
                dx = 6;
                dy = 4;
                var parts = userProfile.Split('\\'); string userName = parts[parts.Length - 1];
                if (!ralseiUndragDSA)
                {
                    using (var msgBox = new CustomMessageBox($"Thanks for the fun, {userName}-kun!\n\nOo-woo...", "Message From Ralsei", new object[] { "No problie at all! :3", TBDSA }))
                    {
                        DialogResult result = msgBox.ShowDialog();
                        if (result == DialogResult.OK && msgBox.ClickedButtonText == "No problie at all!\n:3")
                        {
                            ralseiMenu.Close();
                            ralseiUndragDSA = msgBox.DontShowAgainChecked;
                        }
                    }
                }
            };
            ralseiMenu.Items.Add(ralsUndraggableMenuItem);

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var ralsSpawnMenuItem = new ToolStripMenuItem("Spawn Another Ralsei")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.S,
                ShortcutKeyDisplayString = "Ctrl + Shift + S"
            };
            ralsSpawnMenuItem.Click += (s, e) => { SpawnRalsei(); };
            ralseiMenu.Items.Add(ralsSpawnMenuItem);

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var controlsHelpMenuItem = new ToolStripMenuItem("Controls Help")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.OemQuestion,
                ShortcutKeyDisplayString = "Ctrl + Shift + ?"
            };
            controlsHelpMenuItem.Click += (s, e) => { ShowControls(); };
            ralseiMenu.Items.Add(controlsHelpMenuItem);

            ralseiMenu.Items.Add(new ToolStripSeparator());

            var exitMenuItem = new ToolStripMenuItem("Exit")
            {
                ShortcutKeys = Keys.Alt | Keys.Escape,
                ShortcutKeyDisplayString = "Alt + Esc"
            };
            exitMenuItem.Click += (s, e) => { this.Close(); };
            ralseiMenu.Items.Add(exitMenuItem);

            // Set up a timer for the movement.
            timer = new Timer
            {
                Interval = 33 // ~30FPS. 
            };
            timer.Tick += MoveSprite;
            timer.Start();
            COOLpuffTimer = new Timer();
            COOLpuffTimer.Tick += COOLpuffTimer_Tick;
            COOLpuffTimer.Start();

            UNCOOLpuffTimer = new Timer();
            UNCOOLpuffTimer.Tick += UNCOOLpuffTimer_Tick;
            UNCOOLpuffTimer.Start();
            fpsTimer.Start();
            RalseiManager.RalseiInstances.Add(this);
            PlayMusic();
        }

        private void COOLpuffTimer_Tick(object sender, EventArgs e)
        {
            List<PointF> COOLpuffPositions = new List<PointF>
            {
                new PointF(formWidth * 0.3f, formHeight * 0.2f),
                new PointF(formWidth * 0.2f, formHeight * 0.3f),
                new PointF(formWidth * 0.4f, formHeight * 0.35f),
                new PointF(formWidth * 0.5f, formHeight * 0.5f)
            };
            int index = random.Next(COOLpuffPositions.Count);
            PointF localChosenPos = COOLpuffPositions[index];
            smokeParticles.Add(new SmokeParticle(localChosenPos, COOLpuffImage, PuffType.Cool)); // Relative to Ralsei

            COOLpuffTimer.Interval = rand.Next(500, 2000); // More frequent
        }

        private void UNCOOLpuffTimer_Tick(object sender, EventArgs e)
        {
            float mouthX = 280 * ralseiScale;
            float mouthY = 350 * ralseiScale; // I don't know why I can't just do this in the new object in the first place... but, it's fine ;3
            List<PointF> UNCOOLpuffPositions = new List<PointF>
            {
                new PointF(formWidth * 0.6f, formHeight * 0.8f),
                new PointF(formWidth * 0.7f, formHeight * 0.85f),
                new PointF(formWidth * 0.65f, formHeight * 0.5f),
                new PointF(mouthX * 0.65f, mouthY * 0.55f)
            };
            int index = random.Next(UNCOOLpuffPositions.Count);
            PointF localChosenPos = UNCOOLpuffPositions[index];
            smokeParticles.Add(new SmokeParticle(localChosenPos, UNCOOLpuffImage, PuffType.Uncool));

            UNCOOLpuffTimer.Interval = rand.Next(3000, 6000); // Less frequent
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int newWidth = (int)(spriteImage.Width * ralseiScale);
            int newHeight = (int)(spriteImage.Height * ralseiScale);

            // Draw smoke particles
            foreach (var puff in smokeParticles)
            {
                ColorMatrix matrix = new ColorMatrix { Matrix33 = puff.Alpha };
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                float baseScale = puff.Type == PuffType.Uncool ? 0.2f : 0.6f;
                float p_scale = baseScale * ralseiScale;
                int width = (int)(puff.PuffImage.Width * p_scale);
                int height = (int)(puff.PuffImage.Height * p_scale);

                Rectangle destRect = new Rectangle(
                (int)(puff.Position.X - width / 2),   // Center on X
                (int)(puff.Position.Y - height / 2),  // Center on Y
                width,
                height
                );

                e.Graphics.DrawImage(
                    puff.PuffImage,
                    destRect,
                    0, 0,
                    puff.PuffImage.Width,
                    puff.PuffImage.Height,
                    GraphicsUnit.Pixel,
                    attributes
                );
            }
            // Draw Ralsei
            e.Graphics.DrawImage(spriteImage, new Rectangle(0, 0, newWidth, newHeight));
        }

        private void MoveSprite(object sender, EventArgs e)
        {
            if (ralseiIsFollowingMouse)
            {
                // Find the center of our sprite.
                int ralseiCenterX = this.Left + (this.Width / 2);
                int ralseiCenterY = this.Top + (this.Height / 2);

                // Find the vector toward our cursor's positions.
                int dxToCursor = Cursor.Position.X - ralseiCenterX;
                int dyToCursor = Cursor.Position.Y - ralseiCenterY;

                // Distance (AVOID dividing by zero!! Very bad >;c)
                double distance = Math.Sqrt(dxToCursor * dxToCursor + dyToCursor * dyToCursor);

                //accelerationStrength = 3.0; // The acceleration for the speed build-up!! (This is now a public variable!! It's still used, but in the local class range :3)
                                             // This builds up over time/distance, but it doesn't quite overshoot the cursor. ^^
                velocityX += dxToCursor * accelerationStrength / Math.Max(distance, 20); // This prevents HUGE acceleration when very close! ;p
                velocityY += dyToCursor * accelerationStrength / Math.Max(distance, 20);

                // Velocity damping!! Of course, for the smoothie-ness (Smooth??? Smoothie?? Smooth Smoothie?????????? I'm... hungry :3)
                double damping = 0.90;
                velocityX *= damping;
                velocityY *= damping;

                if (distance < 3)
                {
                    // If my beautiful husband, Doobie Ralsei, gets close enough to the cursor, we'll slow him down and eventually make him stop moving
                    // once his coordinates align with the cursor's coordinates! :3
                    velocityX = 0;
                    velocityY = 0;
                    this.Left = Cursor.Position.X - (this.Width / 2);
                    this.Top = Cursor.Position.Y - (this.Height / 2);
                }
                else
                {
                    // Butttt, otherwise, we just move him by the current available velocity :333 (the husband ((((((Ralsei ;3)))))) material is [[Real]])
                    this.Left += (int)Math.Round(velocityX);
                    this.Top += (int)Math.Round(velocityY);
                }
                ralseiWasLastFollowingMouseTracker = true;
            }
            else
            {
                if (ralseiWasLastFollowingMouseTracker) // Yes, I know this is just the same block as the draggable if statemennt, BUT IT WORKS THE SAME!!!!!!!!!!!!!!!
                // And, uh... I don't really want to use goto here or anywhere else, because I'm stupid!!! :3
                {
                    // Here we go again... (I WAS SO DEPRESSED WHILE WRITING THIS... PLEASE. IGNORE THE DRY COMMENTS, FOR I BEG YOU ( ˶°ㅁ°) !!)
                    velocityDamping = 0.95f;
                    // Apply velocity decay to slow down over time.
                    dx = (int)(dx * velocityDamping);
                    dy = (int)(dy * velocityDamping);

                    // Stop movemennt if velocity is very low.
                    if (Math.Abs(dx) < 1)
                    {
                        dx = 0;
                    }
                    if (Math.Abs(dy) < 1)
                    {
                        dy = 0;
                    }
                    ralseiWasLastFollowingMouseTracker = false;
                }

                this.Left += (int)(dx * speedMultiplier);
                this.Top += (int)(dy * speedMultiplier);
            }

            // Bounce horizontally
            if (this.Left < 0 || this.Left > Screen.PrimaryScreen.Bounds.Width - this.Width)
            {
                dx = -dx;
            }

            // Bounce vertically
            if (this.Top < 0 || this.Top > Screen.PrimaryScreen.Bounds.Height - this.Height)
            {
                dy = -dy;
            }

            if (draggable)
            {
                const float velocityDamping = 0.95f;
                // Apply velocity decay to slow down over time
                dx = (int)(dx * velocityDamping);
                dy = (int)(dy * velocityDamping);

                // Optionally, stop movement if velocity is very low
                if (Math.Abs(dx) < 1) dx = 0;
                if (Math.Abs(dy) < 1) dy = 0;
            }

            COOLpuffTimer.Tick += (s, _e) =>
            {
                // Position relative to Ralsei's current location

                COOLpuffTimer.Interval = rand.Next(500, 5000); // Reset interval
            };
            UNCOOLpuffTimer.Tick += (s, _e) =>
            {
                // The same with this one...

                UNCOOLpuffTimer.Interval = rand.Next(200, 5000); // Reset interval
            };

            // Update and remove dead particles
            foreach (var puff in smokeParticles.ToList())
            {
                puff.Update();
                if (puff.IsDead)
                    smokeParticles.Remove(puff);
            }

            if (smokeParticles.Count > 100)
            {
                smokeParticles.RemoveRange(0, smokeParticles.Count - 100);
            }

            this.Invalidate();

            frameCount++;

            if (fpsTimer.ElapsedMilliseconds >= 1500)
            {
                Console.WriteLine($"FPS: {frameCount}");
                Console.WriteLine($"Puff Particles: {smokeParticles.Count}");
                if (smokeParticles.Count > 0)
                {
                    Console.WriteLine($"Last Puff Position: {smokeParticles[smokeParticles.Count - 1].Position}");
                }
                Console.WriteLine($"COOL Timer interval: {COOLpuffTimer.Interval}");
                Console.WriteLine($"UNCOOL Timer interval: {UNCOOLpuffTimer.Interval}");
                frameCount = 0;
                fpsTimer.Restart();
            }
        }

        private void PlayMusic(bool promptIfCustomSet = true)
        {
            if (!RalseiManager.MusicIsPlaying)
            {
                wplayer = new WindowsMediaPlayer
                {
                    URL = musicUrl
                };
                if (wplayer.URL != Path.Combine(assetPath, "dump.mp3") && isUsingCustomMusic && promptIfCustomSet)
                {
                    DialogResult result = MessageBox.Show("Custom music is currently in queue, do you want to play the default music instead? ^^ " +
                        "(You'll always be able to add custom music back again!)",
                        "TEAR DOWN MY KIDS!! TEAR ME DOWN!! (Can I say that on TV..?)", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        wplayer.URL = Path.Combine(assetPath, "dump.mp3");
                        isUsingCustomMusic = false;
                    }
                }
                wplayer.settings.setMode("loop", true);
                wplayer.controls.play();
                RalseiManager.MusicIsPlaying = true;
            }
        }

        private void StopMusic()
        {
            if (RalseiManager.MusicIsPlaying && wplayer != null)
            {
                wplayer.settings.setMode("loop", false);
                wplayer.controls.stop();
                RalseiManager.MusicIsPlaying = false;
            }
        }

        private void CustomMusic()
        {
            OpenFileDialog fileUrl = new OpenFileDialog();
            fileUrl.Filter = "Audio Files|*.mp3;*.wav;*.ogg;*.wma;*.flac;*.m4a;*.aac|All Files|*.*";
            if (fileUrl.ShowDialog() == DialogResult.OK)
            {
                musicUrl = fileUrl.FileName;
                isUsingCustomMusic = true;
                if (wplayer != null)
                {
                    if (RalseiManager.MusicIsPlaying)
                    {
                        wplayer.controls.stop();
                        RalseiManager.MusicIsPlaying = false;
                    }
                }
                PlayMusic(false);
            }
        }


        public static void ShowControls()
        {
            using (var msgBox = new CustomMessageBox("Hi! Doobie Ralsei here- *cough*... I'm so sorry, let me recover from and absolutely DEMOLISH my lung cancer real quick!! :p" +
                "\n\nThere we *cough*- go! (I won) Either way, going straight to the point, I'll show you how you can efficiently use the controls as your own." +
                "\n\nYes, there might not be much, but they're something to begin with, at least! In case you'll need a shortcut to execute a specific action, " +
                "you'll need to use the shortcut keys/controls to efficiently make that happen." +
                "\n\n- Ctrl + Shift + T: Press all of these after each other and that'll make Ralsei track your mouse as you move it." +
                "\n- Ctrl + Alt + T: You should also press all of these after each other and this'll make Ralsei stop tracking your mouse and he'll " +
                "keep moving at a fixed pace, starting from the coordinates he currently has." +
                "\n\nCtrl + Shift + D: Like the last one, press all of these keys after each other and you'll be able to freely play around with your Doobie Ralsei Instance. " +
                "You'll be able to drag him around and throw him all over the screen to your liking! I promise he won't get hurt, he's definitely a strong boy. ^^" +
                "\nCtrl + Alt + D: This'll make your Doobie Ralsei stop being able to be dragged on the screen and will progress in moving at a fixed pace " +
                "(the same as Ctrl + Shift + T), starting from the coordinates he currently has (the coordinate where he's been dragged to, to say the least! ;p)." +
                "\n\n- Ctrl + Shift + (Up, Down): This sets the speed of movement for Ralsei. Like Ctrl + Shift + T, you press these after each other. " +
                "Up (the upper arrow key on your keyboard) makes him go faster, " +
                "Down (the lower arrow key on your keyboard) makes him go slower." +
                "\n\n- Ctrl + Shift + M: This plays Doobie Ralsei's music (032 - Dump by Toby Fox, originating in DELTARUNE, Chapter 3(+4)). " +
                "like the rest, you should press these keys after each other." +
                "\n- Ctrl + Alt + M: This stops the music currently playing (if the song is playing in the current). " +
                "This works exactly like Ctrl + Alt + T, for clarification. " +
                "\nCtrl + C + M: This gives you the ability to replace the default music with your own sound file (usually .mp3, .ogg or .wav). If you'd rather listen to " +
                "something groovy and NEVER glooby, then feel free to change it whenever you'd like! ^^" +
                "\n\nCtrl + Shift + S: This spawns a new Ralsei Doobie instance. This is a separate window that gets layered over your previous Ralsei Doobie instance, " +
                "this key is quite anti-performant and might cause latency or even personal issues if done excessively. Please be cautioned about this." +
                "\n(Also, one small thing, in order to focus your preferred Doobie Ralsei instance on the screen is to simply click the image of the instance " +
                "on the screen or to click the window of your preferred Doobie Ralsei instance! Otherwise, all these controls may not work on the Doobie Ralsei instance " +
                "you prefer to perform these actions on.)" +
                "\n\nCtrl + Shift + ? (question mark on your keyboard): These specific keys together open up this very page! If you feel like you need to look up a " +
                "key sequence/control, then simply press Ctrl + Shift + ? and this will open up for you as well. Both clicking through the menu and Shortcut Keys are fine." +
                "\n\nAlt + Escape (or, 'Esc'): This quits the current Doobie Ralsei instance. This might take a while to manually do if more than 5 Doobie Ralsei instances are " +
                "on the screen, these keys might also not exit out of your preferred Ralsei Doobie instance if not focused properly. It's better practice to right click " +
                "the Ralsei Doobie instance on the screen that you want to select and to manually click 'Exit' on your preferred Ralsei Doobie instance(s)." +
                "\n\n\nAlso, my creator told me to share this with you: \"Happy Doobie, just like always! I hope you find my silly little creation (named 'Ralsei-Doobie' on GitHub) " +
                "somewhat entertainable! ^-^'\"" +
                "\n\n- Clyde (also TRS (Team Rainy Spiritual) or 'clydezzz-sleepy' on GitHub!)" +
                "\n(今後も幸運を祈っています、えへへ～)",
                "EVERYTHING IN THIS [$4.99 Life] NEEDS [TASTY KROMER]", "Thank you, got everything noted! :3"))
            {
                DialogResult result = msgBox.ShowDialog();
                if (result == DialogResult.OK && msgBox.ClickedButtonText == "Thank you, got everything noted! :3")
                {
                    msgBox.Close();
                }
            }
        }

        public static void SpawnRalsei()
        {
            // If there are 5 or more Ralseis and the user hasn't acknowledged the risk
            if (RalseiManager.RalseiInstances.Count >= 5 &&
                !VERYUNCOOLSpawnMoreRalseisExtremeRiskPleaseGodHelpThisUsersPCBeforeItAbsolutelyFuckingDisintegratesIntoEndlessOblivionAndProbablyMaybeMostDefinitelyWillJustFuckingDieYeahIdAssumeThatYeahIllJustPrayForYouThatYouDontExplodeMaybeDontThinkThatllMakeYourChanceOfSurvivingAnyBiggerThoughLmaoButUhhhIAmDefinitelyOneHundredPercentNoScamNotResponsibleForYourDeathNeitherWillIAttendYourFuneralIfYouDieOkay)
            {
                DialogResult result = MessageBox.Show(
                    "There are (more than) 5 Ralsei Shimeji's being spawned at the moment.\n" +
                    "Do you acknowledge the risk of spawning more Ralsei's?\n" +
                    "Continuing might make your computer temporarily or even permanently unresponsive.",
                    "IT'S SUCH A STEAL, I'M [$!X$]ING MYSELF!!!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    MessageBox.Show("[[MAMA]]!!\nWATCH ME BE A [[BIG SHOT]] AND SEND MY [Electrical Waste] TO [H E A V E N]!\nTHIS FEELS LIKE [[Doobie.]]\n\n" +
                        "(I AM [[Not Parental Controls Physically Advised]] FOR YOUR [[Fun(s)]]!!!!)", 
                        "HOLY CUNGADEROS [[Ralsei Plush With Its $4.99 Life]]!!",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk);
                    VERYUNCOOLSpawnMoreRalseisExtremeRiskPleaseGodHelpThisUsersPCBeforeItAbsolutelyFuckingDisintegratesIntoEndlessOblivionAndProbablyMaybeMostDefinitelyWillJustFuckingDieYeahIdAssumeThatYeahIllJustPrayForYouThatYouDontExplodeMaybeDontThinkThatllMakeYourChanceOfSurvivingAnyBiggerThoughLmaoButUhhhIAmDefinitelyOneHundredPercentNoScamNotResponsibleForYourDeathNeitherWillIAttendYourFuneralIfYouDieOkay
                        = true;
                }
                else
                {
                    return; // Cancel spawn
                }
            }

            // Always spawn if under limit or risk acknowledged
            RalseiDoobie newRalsei = new RalseiDoobie();
            newRalsei.Show();
        }

        private void ResizeRalsei()
        {
            this.Width = (int)(spriteImage.Width * ralseiScale);
            this.Height = (int)(spriteImage.Height * ralseiScale);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift))
            {
                if (keyData.HasFlag(Keys.Up))
                {
                    speedMultiplier = Math.Max(0.1f, speedMultiplier + 0.1f);
                }
                else if (keyData.HasFlag(Keys.Down))
                {
                    speedMultiplier = Math.Max(0.1f, speedMultiplier - 0.1f);
                }
                return true; // Indicate the key was handled!! Just a little boop (Ralsei) >o<
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift) && keyData.HasFlag(Keys.T))
            {
                ralseiIsFollowingMouse = true;
                return true; // (The same goes with every other key!! ^^^ (the last if statement))
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Alt) && keyData.HasFlag(Keys.T))
            {
                ralseiIsFollowingMouse = false;
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift) && keyData.HasFlag(Keys.D))
            {
                draggable = true;
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Alt) && keyData.HasFlag(Keys.D))
            {
                draggable = false;
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Alt) && keyData.HasFlag(Keys.T))
            {
                ralseiIsFollowingMouse = false;
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift) && keyData.HasFlag(Keys.M))
            {
                PlayMusic();
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Alt) && keyData.HasFlag(Keys.M))
            {
                StopMusic();
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.C) && keyData.HasFlag(Keys.M))
            {
               CustomMusic();
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift) && keyData.HasFlag(Keys.S))
            {
                SpawnRalsei();
                return true;
            }
            else if (keyData.HasFlag(Keys.Control) && keyData.HasFlag(Keys.Shift) && keyData.HasFlag(Keys.OemQuestion))
            {
                ShowControls();
                return true;
            }
            else if (keyData.HasFlag(Keys.Alt) && keyData.HasFlag(Keys.Escape))
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging && draggable)
            {
                Point currentPos = PointToScreen(e.Location);
                DateTime currentTime = DateTime.Now;

                // Keep last 5 samples
                mousePositions.Enqueue((currentPos, currentTime));
                while (mousePositions.Count > 5)
                    mousePositions.Dequeue();

                // Move window
                Location = new Point(currentPos.X - mouseOffset.X, currentPos.Y - mouseOffset.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false;

                if (mousePositions.Count >= 2)
                {
                    var (pos, time) = mousePositions.Peek();
                    var last = mousePositions.Last();

                    double totalMs = (last.time - time).TotalMilliseconds;
                    if (totalMs > 0)
                    {
                        dx = (int)((last.pos.X - pos.X) / totalMs * 30); // Adjust multiplier for speed
                        dy = (int)((last.pos.Y - pos.Y) / totalMs * 30);
                    }
                    else
                    {
                        dx = dy = 0;
                    }
                }

                mousePositions.Clear();
                timer.Start();
            }
        }

        static class MainProgram
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RalseiDoobie());
        }
    }
  }
}
