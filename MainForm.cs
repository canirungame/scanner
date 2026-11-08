using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CanIRunGame
{
    class Card : Panel
    {
        public Color Border = Color.FromArgb(29, 36, 45);

        public Card()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    class ShimmerBar : Control
    {
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int offset;

        public ShimmerBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Height = 8;
            timer.Interval = 16;
            timer.Tick += delegate
            {
                offset = (offset + 7) % (Width + 160);
                Invalidate();
            };
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(16, 20, 26));
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(61, 255, 160)))
            {
                e.Graphics.FillRectangle(brush, offset - 160, 0, 160, Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(9, 11, 14);
        static readonly Color Panel1 = Color.FromArgb(16, 20, 26);
        static readonly Color Line = Color.FromArgb(29, 36, 45);
        static readonly Color Ink = Color.FromArgb(216, 222, 230);
        static readonly Color Mute = Color.FromArgb(122, 134, 150);
        static readonly Color Neon = Color.FromArgb(61, 255, 160);
        static readonly Color Bad = Color.FromArgb(255, 92, 108);
        static readonly Color Warn = Color.FromArgb(255, 194, 71);
        const int BodyWidth = 464;

        readonly string api;
        readonly Panel body = new Panel();
        readonly Label[] steps = new Label[3];
        Dictionary<string, object> specs;
        Label hint;

        public MainForm(string api, bool demo)
        {
            this.api = api;
            Text = "Can I Run Game - PC Scanner";
            ClientSize = new Size(520, 640);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 9.75f);
            LoadIcons();
            BuildFrame();
            if (demo) ShowDemo();
            else StartScan();
        }

        void LoadIcons()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            try
            {
                using (Stream s = asm.GetManifestResourceStream("icon.ico")) if (s != null) Icon = new Icon(s);
            }
            catch (Exception)
            {
            }
        }

        void BuildFrame()
        {
            PictureBox logo = new PictureBox();
            logo.Location = new Point(28, 24);
            logo.Size = new Size(46, 46);
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.png"))
                    if (s != null) logo.Image = new Bitmap(Image.FromStream(s));
            }
            catch (Exception)
            {
            }
            Controls.Add(logo);
            Controls.Add(MakeLabel(this, "Can I Run Game", 17f, FontStyle.Bold, Ink, 86, 22, 380, 30));
            Controls.Add(MakeLabel(this, "PC scanner", 10f, FontStyle.Regular, Mute, 86, 50, 380, 20));

            string[] names = { "Scan", "Review", "Done" };
            int x = 28;
            for (int i = 0; i < 3; i++)
            {
                steps[i] = MakeLabel(this, (i + 1) + "  " + names[i], 10f, FontStyle.Bold, Mute, x, 96, 90, 22);
                Controls.Add(steps[i]);
                x += 96;
            }

            body.Location = new Point(28, 134);
            body.Size = new Size(BodyWidth, 460);
            body.BackColor = Bg;
            Controls.Add(body);

            string host = api.Replace("https://", "").Replace("http://", "");
            Label footer = MakeLabel(this, host, 8f, FontStyle.Regular, Mute, 0, 612, 520, 18);
            footer.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(footer);
        }

        static Label MakeLabel(Control parent, string text, float size, FontStyle style, Color color, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.Text = text;
            label.Font = new Font("Segoe UI", size, style);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.Location = new Point(x, y);
            if (height <= 0)
            {
                Size measured = TextRenderer.MeasureText(text, label.Font, new Size(width, 4000), TextFormatFlags.WordBreak);
                height = measured.Height + 4;
            }
            label.Size = new Size(width, height);
            return label;
        }

        Button MakeButton(string text, bool primary, int x, int y, int width, int height, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 11f, primary ? FontStyle.Bold : FontStyle.Regular);
            button.ForeColor = primary ? Bg : Ink;
            button.BackColor = primary ? Neon : Panel1;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Line;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(125, 255, 196) : Line;
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(90, 230, 160) : Line;
            button.Click += click;
            return button;
        }

        Label MakeLink(string text, int x, int y, int width, EventHandler click, ContentAlignment align)
        {
            Label link = MakeLabel(body, text, 9f, FontStyle.Underline, Mute, x, y, width, 20);
            link.Cursor = Cursors.Hand;
            link.TextAlign = align;
            link.Click += click;
            return link;
        }

        void SetStep(int index)
        {
            for (int i = 0; i < 3; i++) steps[i].ForeColor = i == index ? Neon : (i < index ? Ink : Mute);
        }

        void ClearBody()
        {
            foreach (Control c in body.Controls.Cast<Control>().ToList()) c.Dispose();
            body.Controls.Clear();
        }

        void Post(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((MethodInvoker)delegate { action(); }); } catch (Exception) { }
        }

        void ShowProgress(string title, string subtitle)
        {
            ClearBody();
            body.Controls.Add(MakeLabel(body, title, 18f, FontStyle.Bold, Ink, 0, 24, BodyWidth, 36));
            body.Controls.Add(MakeLabel(body, subtitle, 11f, FontStyle.Regular, Mute, 0, 64, BodyWidth, 24));
            ShimmerBar bar = new ShimmerBar();
            bar.Location = new Point(0, 110);
            bar.Width = BodyWidth;
            body.Controls.Add(bar);
        }

        void StartScan()
        {
            SetStep(0);
            ShowProgress("Scanning your PC", "Looking at your processor...");
            Label status = (Label)body.Controls[1];
            string[] messages = { "Looking at your processor...", "Checking your graphics card...", "Counting your memory...", "Measuring your screen...", "Almost done..." };
            int step = 0;
            System.Windows.Forms.Timer ticker = new System.Windows.Forms.Timer();
            ticker.Interval = 420;
            ticker.Tick += delegate
            {
                if (status.IsDisposed)
                {
                    ticker.Stop();
                    ticker.Dispose();
                    return;
                }
                step = Math.Min(step + 1, messages.Length - 1);
                status.Text = messages[step];
            };
            ticker.Start();
            Thread worker = new Thread(delegate()
            {
                try
                {
                    Stopwatch clock = Stopwatch.StartNew();
                    Dictionary<string, object> result = Scanner.Collect();
                    int remaining = 2300 - (int)clock.ElapsedMilliseconds;
                    if (remaining > 0) Thread.Sleep(remaining);
                    Post(delegate { ShowReview(result); });
                }
                catch (Exception ex)
                {
                    Post(delegate { ShowError("Something went wrong while scanning: " + ex.Message, false); });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        static string Str(object value)
        {
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        static string Gb(object value)
        {
            double d = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return d.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        List<string[]> FriendlyRows()
        {
            Dictionary<string, object> cpu = (Dictionary<string, object>)specs["cpu"];
            Dictionary<string, object> ram = (Dictionary<string, object>)specs["ram"];
            Dictionary<string, object> display = (Dictionary<string, object>)specs["display"];
            Dictionary<string, object> os = (Dictionary<string, object>)specs["os"];
            List<object> gpus = (List<object>)specs["gpus"];

            string processor = Str(cpu["name"]);
            if (processor.Length == 0) processor = "Not detected";
            string graphics = gpus.Count == 0
                ? "Not detected"
                : string.Join("\n", gpus.Select(delegate(object g)
                {
                    Dictionary<string, object> gpu = (Dictionary<string, object>)g;
                    return Str(gpu["name"]) + (gpu["vram_gb"] != null ? "  (" + Gb(gpu["vram_gb"]) + " GB)" : "");
                }).ToArray());
            string memory = Gb(specs["ram_gb"]) + " GB" + (Str(ram["type"]).Length > 0 ? " " + Str(ram["type"]) : "");
            string screen = "Not detected";
            if (Convert.ToInt32(display["width"]) > 0)
            {
                screen = display["width"] + " x " + display["height"];
                if (Convert.ToInt32(display["refresh_hz"]) > 0) screen += "  -  " + display["refresh_hz"] + " Hz";
            }
            List<string[]> rows = new List<string[]>();
            rows.Add(new[] { "Processor", processor });
            rows.Add(new[] { "Graphics", graphics });
            rows.Add(new[] { "Memory", memory });
            rows.Add(new[] { "Screen", screen });
            rows.Add(new[] { "Computer", Str(specs["device_type"]) == "laptop" ? "Laptop" : "Desktop" });
            rows.Add(new[] { "Windows", Str(os["name"]) });
            return rows;
        }

        void ShowReview(Dictionary<string, object> scanned)
        {
            specs = scanned;
            SetStep(1);
            ClearBody();
            body.Controls.Add(MakeLabel(body, "Here is your PC", 18f, FontStyle.Bold, Ink, 0, 6, BodyWidth, 36));

            Card card = new Card();
            card.BackColor = Panel1;
            card.Location = new Point(0, 52);
            int y = 12;
            Font valueFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            foreach (string[] row in FriendlyRows())
            {
                Size size = TextRenderer.MeasureText(row[1], valueFont, new Size(330, 2000), TextFormatFlags.WordBreak);
                int height = Math.Max(size.Height + 2, 22);
                card.Controls.Add(MakeLabel(card, row[0], 10f, FontStyle.Regular, Mute, 16, y, 90, 22));
                card.Controls.Add(MakeLabel(card, row[1], 10f, FontStyle.Bold, Ink, 112, y, 336, height));
                y += height + 12;
            }
            card.Size = new Size(BodyWidth, y + 2);
            body.Controls.Add(card);

            int next = card.Bottom + 14;
            body.Controls.Add(MakeLabel(body, "Only this hardware information is sent. Nothing personal.", 9f, FontStyle.Regular, Mute, 0, next, BodyWidth, 20));
            body.Controls.Add(MakeButton("Send and get my code", true, 0, next + 34, BodyWidth, 48, delegate { StartUpload(); }));
            body.Controls.Add(MakeLink("What exactly is sent?", 0, next + 96, 200, delegate { ShowPayload(); }, ContentAlignment.MiddleLeft));
            body.Controls.Add(MakeLink("Close", BodyWidth - 100, next + 96, 100, delegate { Close(); }, ContentAlignment.MiddleRight));
        }

        void ShowPayload()
        {
            Form form = new Form();
            form.Text = "Data that will be sent";
            form.ClientSize = new Size(480, 520);
            form.StartPosition = FormStartPosition.CenterParent;
            form.BackColor = Bg;
            form.ShowIcon = false;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            TextBox box = new TextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.Dock = DockStyle.Fill;
            box.BackColor = Panel1;
            box.ForeColor = Ink;
            box.BorderStyle = BorderStyle.None;
            box.Font = new Font("Consolas", 9f);
            box.Text = Json.Write(specs, true);
            form.Controls.Add(box);
            form.ShowDialog(this);
            form.Dispose();
        }

        void ShowError(string message, bool canRetry)
        {
            SetStep(0);
            ClearBody();
            body.Controls.Add(MakeLabel(body, "That did not work", 18f, FontStyle.Bold, Bad, 0, 24, BodyWidth, 36));
            Label text = MakeLabel(body, message, 10f, FontStyle.Regular, Mute, 0, 70, BodyWidth, 0);
            body.Controls.Add(text);
            int y = text.Bottom + 24;
            if (canRetry)
            {
                body.Controls.Add(MakeButton("Try again", true, 0, y, BodyWidth, 48, delegate { StartUpload(); }));
                y += 60;
            }
            body.Controls.Add(MakeButton("Close", false, 0, y, BodyWidth, 44, delegate { Close(); }));
        }

        void StartUpload()
        {
            SetStep(1);
            ShowProgress("Sending", "Getting your code...");
            Thread worker = new Thread(delegate()
            {
                try
                {
                    Dictionary<string, object> result = Scanner.Upload(api, specs);
                    Post(delegate { ShowDone(result); });
                }
                catch (Exception ex)
                {
                    Post(delegate { ShowError("Could not send your data: " + ex.Message + "\nCheck your internet connection and try again.", true); });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        void ShowDemo()
        {
            Dictionary<string, object> rig = new Dictionary<string, object>();
            rig["score"] = 1447;
            rig["percentile"] = 48;
            rig["tier"] = "Mainstream";
            Dictionary<string, object> tip = new Dictionary<string, object>();
            tip["text"] = "Your RAM is rated 3200 MHz but runs at 2133 MHz. Enable XMP/EXPO in the BIOS for a free performance boost.";
            rig["tips"] = new object[] { tip };
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["code"] = "K7M2QX";
            result["expires_in"] = 1800;
            result["rig"] = rig;
            ShowDone(result);
        }

        void ShowDone(Dictionary<string, object> result)
        {
            SetStep(2);
            ClearBody();
            string code = Str(result["code"]);
            int minutes = result.ContainsKey("expires_in") ? Convert.ToInt32(result["expires_in"]) / 60 : 30;
            body.Controls.Add(MakeLabel(body, "You're all set", 18f, FontStyle.Bold, Ink, 0, 6, BodyWidth, 36));
            body.Controls.Add(MakeLabel(body, "Type this code on the website:", 11f, FontStyle.Regular, Mute, 0, 46, BodyWidth, 24));

            Card box = new Card();
            box.Border = Neon;
            box.BackColor = Panel1;
            box.Location = new Point(0, 78);
            box.Size = new Size(BodyWidth, 84);
            Label big = MakeLabel(box, string.Join(" ", code.ToCharArray().Select(delegate(char c) { return c.ToString(); }).ToArray()), 34f, FontStyle.Bold, Neon, 1, 12, BodyWidth - 2, 60);
            big.Font = new Font("Consolas", 34f, FontStyle.Bold);
            big.TextAlign = ContentAlignment.MiddleCenter;
            box.Controls.Add(big);
            body.Controls.Add(box);

            hint = MakeLabel(body, "Valid for " + minutes + " minutes", 9f, FontStyle.Regular, Mute, 0, 172, BodyWidth, 20);
            body.Controls.Add(hint);

            string website = api.TrimEnd('/') + "/scan";
            body.Controls.Add(MakeButton("Copy code", true, 0, 204, 226, 46, delegate { CopyCode(code); }));
            body.Controls.Add(MakeButton("Open website", false, 238, 204, 226, 46, delegate { OpenUrl(website); }));

            int y = 272;
            Dictionary<string, object> rig = result.ContainsKey("rig") ? result["rig"] as Dictionary<string, object> : null;
            if (rig != null && rig.ContainsKey("score") && rig["score"] != null)
            {
                int percentile = Convert.ToInt32(rig["percentile"]);
                Card card = new Card();
                card.BackColor = Panel1;
                card.Location = new Point(0, y);
                card.Size = new Size(BodyWidth, 100);
                card.Controls.Add(MakeLabel(card, "Your PC is faster than " + percentile + "% of gaming PCs", 11f, FontStyle.Bold, Ink, 16, 12, BodyWidth - 32, 24));
                card.Controls.Add(MakeLabel(card, "Rig score " + rig["score"] + "  -  " + rig["tier"], 9f, FontStyle.Regular, Mute, 16, 38, BodyWidth - 32, 20));
                Panel track = new Panel();
                track.BackColor = Line;
                track.Location = new Point(16, 72);
                track.Size = new Size(BodyWidth - 32, 8);
                Panel fill = new Panel();
                fill.BackColor = Neon;
                fill.Size = new Size((track.Width * percentile) / 100, 8);
                track.Controls.Add(fill);
                card.Controls.Add(track);
                body.Controls.Add(card);
                y = card.Bottom + 14;
            }

            object[] tips = rig != null && rig.ContainsKey("tips") ? rig["tips"] as object[] : null;
            if (tips != null && tips.Length > 0)
            {
                string text = string.Join("\n", tips.Take(2).Select(delegate(object t)
                {
                    Dictionary<string, object> tipEntry = (Dictionary<string, object>)t;
                    return "Tip: " + Str(tipEntry["text"]);
                }).ToArray());
                body.Controls.Add(MakeLabel(body, text, 9f, FontStyle.Regular, Warn, 0, y, BodyWidth, 0));
            }
        }

        void CopyCode(string code)
        {
            try
            {
                Clipboard.SetText(code);
                hint.Text = "Copied to clipboard";
                hint.ForeColor = Neon;
            }
            catch (Exception)
            {
                hint.Text = "Could not copy. Select the code and copy it by hand.";
            }
        }

        static void OpenUrl(string url)
        {
            try { Process.Start(url); } catch (Exception) { }
        }
    }
}
