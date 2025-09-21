using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
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
            this.ClientSize = new Size(320, 130);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Message label
            labelMsg = new Label()
            {
                Text = message,
                Location = new Point(15, 20),
                Size = new Size(this.ClientSize.Width - 30, 40),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            this.Controls.Add(labelMsg);

            // OK button
            if (buttons.Contains((object)1) || buttons.Contains("OK"))
            {
                btnOK = new Button()
                {
                    Text = "OK",
                    Size = new Size(75, 30),
                    Location = new Point(60, 80),
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
                    Location = new Point(150, 80),
                };
                this.Controls.Add(chkDontShowAgain);
            }

            if (buttons.Contains((object)3) || buttons.Contains("INPUT"))
            {
                inputBox = new TextBox()
                {
                    Location = new Point(15, 60),
                    Size = new Size(this.ClientSize.Width - 30, 32),
                    Text = ""
                };
                this.Controls.Add(inputBox);
            }

            var stringButtons = buttons.OfType<string>().Where(s => s != "OK" && s != "DSA" && s != "INPUT");
            int currentX = 60; // starting X location
            int margin = 10;
            foreach (var label in stringButtons)
            {
                // Calculate button (width) size for a temporary/fallback placeholder
                int width = Math.Max(75, TextRenderer.MeasureText(label, this.Font).Width + 20);

                var btnExt = new Button()
                {
                    Text = label,
                    Size = new Size(width, 30),
                    Location = new Point(currentX, 80),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseCompatibleTextRendering = true
                };

                // Calculate the real size
                var size = TextRenderer.MeasureText(label, btnExt.Font, new Size(200, 0), TextFormatFlags.WordBreak);
                btnExt.Size = new Size(size.Width + 20, size.Height + 10);

                btnExt.Click += (s, e) =>
                {
                    ClickedButtonText = btnExt.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                btnExt.Size = new Size(width, 30);
                chkDontShowAgain.Location = new Point(btnExt.Right + 10, 80);
                this.Controls.Add(btnExt);
                dynamicButtons.Add(btnExt);
                currentX += width + margin;
            }
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
        private readonly string assetPath = Path.Combine(
            userProfile,
            "Downloads",
            "Ralsei_Shimeji",
            "Ralsei_Shimeji",
            "assets ;p"
        );

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

            ralseiMenu.Items.Add("Play Music", null, (s, e) => { PlayMusic(); });
            ralseiMenu.Items.Add("Stop Music", null, (s, e) => { StopMusic(); });

            ralseiMenu.Items.Add("Make Ralsei Draggable", null, (s, e) => { draggable = true;  MessageBox.Show("Toss me around as much as you want!", "Message From Ralsei", MessageBoxButtons.OK); });
            ralseiMenu.Items.Add("Make Ralsei Undraggable", null, (s, e) =>
            {
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
            });

            ralseiMenu.Items.Add(new ToolStripSeparator());

            ralseiMenu.Items.Add("Spawn Another Ralsei", null, (s, e) => { SpawnRalsei(); });

            ralseiMenu.Items.Add("Exit", null, (s, e) => { this.Close(); });

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
            //PointF scaledPos = new PointF(
            //    this.Left + chosenPos.X * ralseiScale,
            //    this.Top + chosenPos.Y * ralseiScale
            //);
            //PointF spawnPos = new PointF(
            //this.Left + chosenPos.X - COOLpuffWidth / 2,
            //this.Top + chosenPos.Y - COOLpuffHeight / 2
            //);
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
            //PointF scaledPos = new PointF(
            //    this.Left + chosenPos.X * ralseiScale,
            //    this.Top + chosenPos.Y * ralseiScale
            //);
            //PointF spawnPos = new PointF(
            //this.Left + chosenPos.X - UNCOOLpuffWidth / 2,
            //this.Top + chosenPos.Y - UNCOOLpuffHeight / 2
            //);
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
                //int alpha = (int)(puff.Alpha * 255);

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
            this.Left += (int)(dx * speedMultiplier);
            this.Top += (int)(dy * speedMultiplier);

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

        private void PlayMusic()
        {
            if (!RalseiManager.MusicIsPlaying)
            {
                wplayer = new WindowsMediaPlayer
                {
                    URL = Path.Combine(assetPath, "dump.mp3")
                };
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
                    "Warning",
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
            switch (keyData)
            {
                case Keys.Up:
                    ralseiScale += 0.1f;
                    ResizeRalsei();
                    break;
                case Keys.Down:
                    ralseiScale = Math.Max(0.5f, ralseiScale - 0.1f);
                    ResizeRalsei();
                    break;
                case Keys.Right:
                    speedMultiplier += 0.1f;
                    break;
                case Keys.Left:
                    speedMultiplier = Math.Max(0.1f, speedMultiplier - 0.1f);
                    break;
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
}using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
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
            this.ClientSize = new Size(320, 130);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Message label
            labelMsg = new Label()
            {
                Text = message,
                Location = new Point(15, 20),
                Size = new Size(this.ClientSize.Width - 30, 40),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            this.Controls.Add(labelMsg);

            // OK button
            if (buttons.Contains((object)1) || buttons.Contains("OK"))
            {
                btnOK = new Button()
                {
                    Text = "OK",
                    Size = new Size(75, 30),
                    Location = new Point(60, 80),
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
                    Location = new Point(150, 80),
                };
                this.Controls.Add(chkDontShowAgain);
            }

            if (buttons.Contains((object)3) || buttons.Contains("INPUT"))
            {
                inputBox = new TextBox()
                {
                    Location = new Point(15, 60),
                    Size = new Size(this.ClientSize.Width - 30, 32),
                    Text = ""
                };
                this.Controls.Add(inputBox);
            }

            var stringButtons = buttons.OfType<string>().Where(s => s != "OK" && s != "DSA" && s != "INPUT");
            int currentX = 60; // starting X location
            int margin = 10;
            foreach (var label in stringButtons)
            {
                // Calculate button (width) size for a temporary/fallback placeholder
                int width = Math.Max(75, TextRenderer.MeasureText(label, this.Font).Width + 20);

                var btnExt = new Button()
                {
                    Text = label,
                    Size = new Size(width, 30),
                    Location = new Point(currentX, 80),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseCompatibleTextRendering = true
                };

                // Calculate the real size
                var size = TextRenderer.MeasureText(label, btnExt.Font, new Size(200, 0), TextFormatFlags.WordBreak);
                btnExt.Size = new Size(size.Width + 20, size.Height + 10);

                btnExt.Click += (s, e) =>
                {
                    ClickedButtonText = btnExt.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                btnExt.Size = new Size(width, 30);
                chkDontShowAgain.Location = new Point(btnExt.Right + 10, 80);
                this.Controls.Add(btnExt);
                dynamicButtons.Add(btnExt);
                currentX += width + margin;
            }
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
        private readonly string assetPath = Path.Combine(
            userProfile,
            "Downloads",
            "Ralsei_Shimeji",
            "Ralsei_Shimeji",
            "assets ;p"
        );

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

            // Some screen size necessities.
            formWidth = this.ClientSize.Width;
            formHeight = this.ClientSize.Height;

            // Load the sprite.
            spriteImage = Image.FromFile(Path.Combine(assetPath, "ralsei_doobieee.png"));
            ralseiWidth = (int)(spriteImage.Width * ralseiScale);
            ralseiHeight = (int)(spriteImage.Height * ralseiScale); // <- ^^ These two aren't going to be used for the time being...
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

            ralseiMenu.Items.Add("Play Music", null, (s, e) => { PlayMusic(); });
            ralseiMenu.Items.Add("Stop Music", null, (s, e) => { StopMusic(); });

            ralseiMenu.Items.Add("Make Ralsei Draggable", null, (s, e) => { draggable = true;  MessageBox.Show("Toss me around as much as you want!", "Message From Ralsei", MessageBoxButtons.OK); });
            ralseiMenu.Items.Add("Make Ralsei Undraggable", null, (s, e) =>
            {
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
            });

            ralseiMenu.Items.Add(new ToolStripSeparator());

            ralseiMenu.Items.Add("Spawn Another Ralsei", null, (s, e) => { SpawnRalsei(); });

            ralseiMenu.Items.Add("Exit", null, (s, e) => { this.Close(); });

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
            //PointF scaledPos = new PointF(
            //    this.Left + chosenPos.X * ralseiScale,
            //    this.Top + chosenPos.Y * ralseiScale
            //);
            //PointF spawnPos = new PointF(
            //this.Left + chosenPos.X - COOLpuffWidth / 2,
            //this.Top + chosenPos.Y - COOLpuffHeight / 2
            //);
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
            //PointF scaledPos = new PointF(
            //    this.Left + chosenPos.X * ralseiScale,
            //    this.Top + chosenPos.Y * ralseiScale
            //);
            //PointF spawnPos = new PointF(
            //this.Left + chosenPos.X - UNCOOLpuffWidth / 2,
            //this.Top + chosenPos.Y - UNCOOLpuffHeight / 2
            //);
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
                //int alpha = (int)(puff.Alpha * 255);

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
            this.Left += (int)(dx * speedMultiplier);
            this.Top += (int)(dy * speedMultiplier);

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

        private void PlayMusic()
        {
            if (!RalseiManager.MusicIsPlaying)
            {
                wplayer = new WindowsMediaPlayer
                {
                    URL = Path.Combine(assetPath, "dump.mp3")
                };
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
                    "Warning",
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
            switch (keyData)
            {
                case Keys.Up:
                    ralseiScale += 0.1f;
                    ResizeRalsei();
                    break;
                case Keys.Down:
                    ralseiScale = Math.Max(0.5f, ralseiScale - 0.1f);
                    ResizeRalsei();
                    break;
                case Keys.Right:
                    speedMultiplier += 0.1f;
                    break;
                case Keys.Left:
                    speedMultiplier = Math.Max(0.1f, speedMultiplier - 0.1f);
                    break;
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
