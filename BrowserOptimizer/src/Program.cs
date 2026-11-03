using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SoNGBrowserOptimizer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // A side-effect-free self-test is available to the build validator.
            if (args.Length > 0 && args[0].StartsWith("/selftest=", StringComparison.OrdinalIgnoreCase))
            {
                string path = args[0].Substring("/selftest=".Length).Trim('"');
                try
                {
                    BrowserSnapshot a = BrowserEngine.Read("chrome");
                    BrowserSnapshot b = BrowserEngine.Read("msedge");
                    MemoryInfo m = BrowserEngine.SystemMemory();
                    File.WriteAllText(path, "SELFTEST=OK\r\nChromeProcesses=" + a.Count +
                        "\r\nEdgeProcesses=" + b.Count + "\r\nRAMPercent=" + m.Load +
                        "\r\nNoProcessTrimmed=True\r\n", Encoding.UTF8);
                }
                catch (Exception ex) { File.WriteAllText(path, "SELFTEST=FAILED\r\n" + ex); }
                return;
            }
            bool created;
            using (var mutex = new Mutex(true, @"Local\SoNG_Browser_Optimizer_v1", out created))
            {
                if (!created) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }

    internal static class Style
    {
        internal static readonly Color Base = Color.FromArgb(15, 20, 30);
        internal static readonly Color Panel = Color.FromArgb(25, 32, 44);
        internal static readonly Color Panel2 = Color.FromArgb(31, 40, 54);
        internal static readonly Color Border = Color.FromArgb(53, 65, 83);
        internal static readonly Color White = Color.FromArgb(239, 245, 251);
        internal static readonly Color Muted = Color.FromArgb(146, 160, 179);
        internal static readonly Color Teal = Color.FromArgb(69, 216, 180);
        internal static readonly Color Blue = Color.FromArgb(104, 163, 247);
        internal static Font F(float pt, bool bold = false) { return new Font("맑은 고딕", pt, bold ? FontStyle.Bold : FontStyle.Regular); }
        internal static string MB(long bytes) { return (bytes / 1048576d).ToString("N0") + " MB"; }
        internal static Label Label(string text, int x, int y, int w, int h, float size, Color color, bool bold = false)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                Font = F(size, bold), ForeColor = color, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        }
        internal static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var p = new GraphicsPath(); int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    internal class CardPanel : Panel
    {
        public CardPanel() { BackColor = Style.Panel; DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = Style.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), 13))
            using (var pen = new Pen(Style.Border)) e.Graphics.DrawPath(pen, p);
        }
    }

    internal class ActionButton : Button
    {
        private readonly Color normal;
        public ActionButton(string text, Color back)
        {
            Text = text; normal = back; BackColor = back; ForeColor = Style.Base;
            Font = Style.F(10, true); FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand; UseVisualStyleBackColor = false;
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); BackColor = ControlPaint.Light(normal, .13f); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); BackColor = normal; }
    }

    internal sealed class BrowserSnapshot
    {
        public string ProcessName; public int Count; public long WorkingSet; public long PrivateBytes;
        public long CpuTicks; public double CpuPercent;
    }
    internal sealed class MemoryInfo { public uint Load; public ulong Total; public ulong Available; }
    internal sealed class TrimResult
    {
        public int Success; public int Failed; public long Before; public long After;
    }

    internal static class BrowserEngine
    {
        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetProcessWorkingSetSize(IntPtr h, IntPtr min, IntPtr max);
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys;
            public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual; public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX value);
        public static MemoryInfo SystemMemory()
        {
            var s = new MEMORYSTATUSEX(); s.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (!GlobalMemoryStatusEx(ref s)) throw new System.ComponentModel.Win32Exception();
            return new MemoryInfo { Load = s.dwMemoryLoad, Total = s.ullTotalPhys, Available = s.ullAvailPhys };
        }
        public static BrowserSnapshot Read(string name)
        {
            var r = new BrowserSnapshot { ProcessName = name };
            Process[] processes = Process.GetProcessesByName(name);
            foreach (Process p in processes)
            {
                try
                {
                    r.WorkingSet += p.WorkingSet64;
                    r.PrivateBytes += p.PrivateMemorySize64;
                    r.CpuTicks += p.TotalProcessorTime.Ticks;
                    r.Count++;
                }
                catch { }
                finally { p.Dispose(); }
            }
            return r;
        }
        public static bool IsForeground(string name)
        {
            IntPtr hwnd = GetForegroundWindow(); if (hwnd == IntPtr.Zero) return false;
            uint pid; GetWindowThreadProcessId(hwnd, out pid);
            try { using (Process p = Process.GetProcessById((int)pid)) return p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        public static TrimResult Trim(string name)
        {
            var r = new TrimResult();
            r.Before = Read(name).WorkingSet;
            foreach (Process p in Process.GetProcessesByName(name))
            {
                try
                {
                    IntPtr h = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, p.Id);
                    if (h == IntPtr.Zero) { r.Failed++; continue; }
                    try
                    {
                        if (SetProcessWorkingSetSize(h, new IntPtr(-1), new IntPtr(-1))) r.Success++;
                        else r.Failed++;
                    }
                    finally { CloseHandle(h); }
                }
                catch { r.Failed++; }
                finally { p.Dispose(); }
            }
            Thread.Sleep(450);
            r.After = Read(name).WorkingSet;
            return r;
        }
    }

    internal sealed class BrowserCard : CardPanel
    {
        private Label count, ws, priv, cpu, active;
        private ActionButton action;
        private readonly string processName;
        public event Action<string> OptimizeClicked;
        public BrowserCard(string title, string processName, string letter, Color accent)
        {
            this.processName = processName;
            var brand = Style.Label(letter, 20, 17, 43, 42, 18, accent, true);
            brand.BackColor = Style.Panel2; brand.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(brand);
            Controls.Add(Style.Label(title, 75, 18, 250, 26, 13, Style.White, true));
            active = Style.Label("확인 중...", 75, 45, 270, 20, 9, Style.Muted);
            Controls.Add(active);
            Controls.Add(Style.Label("작업 집합 합계", 22, 84, 170, 20, 9, Style.Muted));
            ws = Style.Label("-", 22, 105, 240, 37, 23, Style.White, true); Controls.Add(ws);
            Controls.Add(Style.Label("Private Bytes", 265, 84, 164, 20, 9, Style.Muted));
            priv = Style.Label("-", 265, 111, 175, 26, 12, Style.White, true); Controls.Add(priv);
            cpu = Style.Label("CPU -", 22, 157, 180, 20, 9, Style.Muted); Controls.Add(cpu);
            action = new ActionButton("이 브라우저 정리", accent) { Location = new Point(260, 151), Size = new Size(191, 36) };
            action.Click += (s, e) => { if (OptimizeClicked != null) OptimizeClicked(this.processName); };
            Controls.Add(action);
            count = active;
        }
        public void SetSnapshot(BrowserSnapshot s)
        {
            count.Text = s.Count == 0 ? "실행 중인 프로세스 없음" : s.Count + "개 프로세스 실행 중";
            ws.Text = Style.MB(s.WorkingSet); priv.Text = Style.MB(s.PrivateBytes);
            cpu.Text = "CPU 약 " + s.CpuPercent.ToString("N1") + "%";
            action.Enabled = s.Count > 0;
        }
        public void SetBusy(bool busy) { action.Enabled = !busy; action.Text = busy ? "정리 중..." : "이 브라우저 정리"; }
    }

    internal sealed class MainForm : Form
    {
        private readonly BrowserCard chrome, edge;
        private readonly Label sysText, totalText, statusText;
        private readonly Panel gaugeFill;
        private readonly CheckBox autoCheck, runCheck;
        private readonly NumericUpDown threshold, interval;
        private readonly ListBox logList;
        private readonly NotifyIcon tray;
        private readonly System.Windows.Forms.Timer timer;
        private readonly ActionButton allButton;
        private bool closing, loading, working;
        private DateTime lastAutomatic = DateTime.MinValue, lastSampleTime = DateTime.MinValue;
        private BrowserSnapshot priorChrome, priorEdge;
        private int consecutivePressure;
        private readonly string settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoNG", "BrowserOptimizer");
        private const string StartupKeyName = "SoNG Browser Optimizer";
        public MainForm()
        {
            Text = "SoNG Browser Optimizer"; BackColor = Style.Base; ForeColor = Style.White;
            ClientSize = new Size(1000, 724); MinimumSize = MaximumSize = Size;
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            Font = Style.F(9); StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            Controls.Add(Style.Label("SoNG", 29, 17, 88, 35, 23, Style.Teal, true));
            Controls.Add(Style.Label("BROWSER OPTIMIZER", 119, 21, 484, 29, 17, Style.White, true));
            Controls.Add(Style.Label("Chrome · Edge  |  Windows 11 메모리 모니터링 및 안전한 작업 집합 정리", 31, 60, 816, 25, 9, Style.Muted));
            var version = Style.Label("v1.1  ·  PORTABLE", 812, 24, 162, 22, 9, Style.Muted);
            version.TextAlign = ContentAlignment.MiddleRight; Controls.Add(version);

            var sys = new CardPanel { Location = new Point(26, 105), Size = new Size(948, 105) }; Controls.Add(sys);
            sys.Controls.Add(Style.Label("시스템 RAM", 21, 13, 240, 28, 12, Style.White, true));
            sysText = Style.Label("확인 중...", 22, 41, 390, 25, 11, Style.Muted); sys.Controls.Add(sysText);
            totalText = Style.Label("-", 596, 14, 330, 31, 14, Style.Teal, true);
            totalText.TextAlign = ContentAlignment.MiddleRight; sys.Controls.Add(totalText);
            var track = new Panel { Location = new Point(22, 77), Size = new Size(902, 8), BackColor = Style.Border }; sys.Controls.Add(track);
            gaugeFill = new Panel { Location = new Point(0, 0), Size = new Size(0, 8), BackColor = Style.Teal }; track.Controls.Add(gaugeFill);

            chrome = new BrowserCard("Google Chrome", "chrome", "C", Style.Teal) { Location = new Point(26, 227), Size = new Size(466, 205) };
            edge = new BrowserCard("Microsoft Edge", "msedge", "E", Style.Blue) { Location = new Point(508, 227), Size = new Size(466, 205) };
            chrome.OptimizeClicked += OptimizeOne; edge.OptimizeClicked += OptimizeOne;
            Controls.Add(chrome); Controls.Add(edge);

            var config = new CardPanel { Location = new Point(26, 449), Size = new Size(948, 128) }; Controls.Add(config);
            config.Controls.Add(Style.Label("자동 최적화 설정", 21, 11, 315, 27, 12, Style.White, true));
            autoCheck = new CheckBox { Text = "스마트 자동 정리 사용", Location = new Point(23, 47), Size = new Size(240, 30), Font = Style.F(10), ForeColor = Style.White, BackColor = Color.Transparent };
            config.Controls.Add(autoCheck);
            runCheck = new CheckBox { Text = "Windows 로그인 시 시작", Location = new Point(23, 83), Size = new Size(245, 29), Font = Style.F(10), ForeColor = Style.White, BackColor = Color.Transparent };
            config.Controls.Add(runCheck);
            config.Controls.Add(Style.Label("시스템 RAM 임계값", 302, 46, 170, 29, 9, Style.Muted));
            threshold = Spin(85, 65, 98, 480, 46, 64); config.Controls.Add(threshold);
            config.Controls.Add(Style.Label("% 이상", 552, 46, 70, 29, 9, Style.Muted));
            config.Controls.Add(Style.Label("최소 정리 간격", 302, 83, 170, 29, 9, Style.Muted));
            interval = Spin(15, 5, 120, 480, 83, 64); config.Controls.Add(interval);
            config.Controls.Add(Style.Label("분", 552, 83, 40, 29, 9, Style.Muted));
            allButton = new ActionButton("지금 모두 정리", Style.Teal) { Location = new Point(738, 44), Size = new Size(186, 61) };
            allButton.Click += (s, e) => OptimizeOne("all"); config.Controls.Add(allButton);
            config.Controls.Add(Style.Label("※ 자동 정리: 비활성 브라우저·낮은 CPU 사용률·RAM 부족 시에만", 615, 105, 308, 15, 7, Style.Muted));

            var activity = new CardPanel { Location = new Point(26, 592), Size = new Size(948, 96) }; Controls.Add(activity);
            activity.Controls.Add(Style.Label("활동 기록", 21, 6, 220, 25, 10, Style.White, true));
            logList = new ListBox { Location = new Point(21, 30), Size = new Size(905, 56), BackColor = Style.Panel, ForeColor = Style.Muted,
                BorderStyle = BorderStyle.None, Font = Style.F(8), IntegralHeight = false };
            activity.Controls.Add(logList);
            statusText = Style.Label("준비 중", 32, 697, 780, 22, 8, Style.Muted); Controls.Add(statusText);
            var hint = Style.Label("닫기 또는 최소화 시 트레이로 이동", 690, 697, 280, 22, 8, Style.Muted);
            hint.TextAlign = ContentAlignment.MiddleRight; Controls.Add(hint);

            var menu = new ContextMenuStrip();
            menu.Items.Add("창 열기", null, (s, e) => ShowWindow());
            menu.Items.Add("지금 모두 정리", null, (s, e) => OptimizeOne("all"));
            menu.Items.Add(new ToolStripSeparator());
            var autoItem = menu.Items.Add("스마트 자동 정리 켜기/끄기");
            autoItem.Click += (s, e) => autoCheck.Checked = !autoCheck.Checked;
            menu.Items.Add("완전히 종료", null, (s, e) => { closing = true; Close(); });
            tray = new NotifyIcon { Icon = Icon, Text = "SoNG Browser Optimizer", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => ShowWindow();
            loading = true; LoadSettings(); loading = false;
            autoCheck.CheckedChanged += (s, e) => { if (!loading) { SaveSettings(); Log(autoCheck.Checked ? "스마트 자동 정리 ON" : "스마트 자동 정리 OFF"); } };
            runCheck.CheckedChanged += (s, e) => { if (!loading) { UpdateStartup(); SaveSettings(); } };
            threshold.ValueChanged += (s, e) => { if (!loading) SaveSettings(); };
            interval.ValueChanged += (s, e) => { if (!loading) SaveSettings(); };
            timer = new System.Windows.Forms.Timer { Interval = 3000 };
            timer.Tick += (s, e) => RefreshData(); timer.Start();
            Log("실행됨 · 자동 정리 " + (autoCheck.Checked ? "ON" : "OFF") + " · 강제 종료 없음");
            RefreshData();
        }
        private NumericUpDown Spin(int val, int min, int max, int x, int y, int width)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = val, Location = new Point(x, y),
                Size = new Size(width, 27), BorderStyle = BorderStyle.FixedSingle, BackColor = Style.Panel2,
                ForeColor = Style.White, Font = Style.F(10), TextAlign = HorizontalAlignment.Center };
        }
        private void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (WindowState == FormWindowState.Minimized) Hide(); }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!closing && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
            if (timer != null) timer.Stop();
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            base.OnFormClosing(e);
        }
        private string ConfigPath { get { return Path.Combine(settingsFolder, "settings.ini"); } }
        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    int i = line.IndexOf('='); if (i <= 0) continue;
                    string k = line.Substring(0, i), v = line.Substring(i + 1);
                    int n;
                    if (k == "auto") autoCheck.Checked = v == "1";
                    if (k == "startup") runCheck.Checked = v == "1";
                    if (k == "threshold" && int.TryParse(v, out n)) threshold.Value = Math.Max(threshold.Minimum, Math.Min(threshold.Maximum, n));
                    if (k == "interval" && int.TryParse(v, out n)) interval.Value = Math.Max(interval.Minimum, Math.Min(interval.Maximum, n));
                }
            }
            catch (Exception ex) { Log("설정 읽기 실패: " + ex.Message); }
        }
        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(settingsFolder);
                File.WriteAllText(ConfigPath, "auto=" + (autoCheck.Checked ? "1" : "0") + "\r\n" +
                    "startup=" + (runCheck.Checked ? "1" : "0") + "\r\nthreshold=" + threshold.Value +
                    "\r\ninterval=" + interval.Value + "\r\n", Encoding.UTF8);
            }
            catch (Exception ex) { Log("설정 저장 실패: " + ex.Message); }
        }
        private void UpdateStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) throw new InvalidOperationException("시작 프로그램 레지스트리를 열 수 없습니다.");
                    string value = "\"" + Application.ExecutablePath + "\"";
                    if (runCheck.Checked) key.SetValue(StartupKeyName, value, RegistryValueKind.String);
                    else
                    {
                        string old = key.GetValue(StartupKeyName, "") as string;
                        if (string.Equals(old, value, StringComparison.OrdinalIgnoreCase)) key.DeleteValue(StartupKeyName, false);
                    }
                }
                Log(runCheck.Checked ? "로그인 시 자동 시작 등록" : "로그인 시 자동 시작 해제");
            }
            catch (Exception ex) { Log("시작 설정 실패: " + ex.Message); }
        }
        private void Log(string message)
        {
            if (logList == null) return;
            if (InvokeRequired) { try { BeginInvoke(new Action<string>(Log), message); } catch { } return; }
            logList.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            while (logList.Items.Count > 80) logList.Items.RemoveAt(logList.Items.Count - 1);
        }
        private void RefreshData()
        {
            if (working) return;
            working = true;
            Task.Run(() =>
            {
                MemoryInfo ram = BrowserEngine.SystemMemory();
                BrowserSnapshot c = BrowserEngine.Read("chrome"), e = BrowserEngine.Read("msedge");
                return Tuple.Create(ram, c, e);
            }).ContinueWith(t =>
            {
                working = false;
                if (IsDisposed || Disposing) return;
                if (t.IsFaulted) { statusText.Text = "조회 실패: " + t.Exception.GetBaseException().Message; return; }
                var ram = t.Result.Item1; var c = t.Result.Item2; var e = t.Result.Item3;
                DateTime now = DateTime.UtcNow;
                if (lastSampleTime != DateTime.MinValue)
                {
                    double seconds = (now - lastSampleTime).TotalSeconds;
                    if (seconds > 0.1)
                    {
                        if (priorChrome != null) c.CpuPercent = Math.Max(0, (c.CpuTicks - priorChrome.CpuTicks) / 10000000d / seconds / Environment.ProcessorCount * 100);
                        if (priorEdge != null) e.CpuPercent = Math.Max(0, (e.CpuTicks - priorEdge.CpuTicks) / 10000000d / seconds / Environment.ProcessorCount * 100);
                    }
                }
                lastSampleTime = now; priorChrome = c; priorEdge = e;
                chrome.SetSnapshot(c); edge.SetSnapshot(e);
                sysText.Text = "사용 " + Style.MB((long)(ram.Total - ram.Available)) + " / 총 " + Style.MB((long)ram.Total);
                totalText.Text = ram.Load + "%  사용 중";
                gaugeFill.Width = (int)(902 * Math.Min(100, ram.Load) / 100);
                gaugeFill.BackColor = ram.Load >= (uint)threshold.Value ? Color.FromArgb(245, 166, 93) : Style.Teal;
                statusText.Text = "자동 정리 " + (autoCheck.Checked ? "켜짐" : "꺼짐") + "  |  RAM " + ram.Load + "%  |  " + DateTime.Now.ToString("HH:mm:ss") + " 갱신";
                if (autoCheck.Checked && ram.Load >= (uint)threshold.Value) consecutivePressure++;
                else consecutivePressure = 0;
                // Two consecutive high-load samples avoid acting on a transient spike.
                if (autoCheck.Checked && consecutivePressure >= 2 && (now - lastAutomatic).TotalMinutes >= (double)interval.Value)
                {
                    string target = null;
                    if (c.WorkingSet >= 512L * 1024 * 1024 && c.CpuPercent < 5 && !BrowserEngine.IsForeground("chrome")) target = "chrome";
                    if (e.WorkingSet >= 512L * 1024 * 1024 && e.CpuPercent < 5 && !BrowserEngine.IsForeground("msedge") && (target == null || e.WorkingSet > c.WorkingSet)) target = "msedge";
                    if (target != null) { lastAutomatic = now; consecutivePressure = 0; OptimizeOne(target, true); }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void OptimizeOne(string target) { OptimizeOne(target, false); }
        private void OptimizeOne(string target, bool automatic)
        {
            if (working && !automatic) { Log("모니터링 중입니다. 잠시 후 다시 눌러 주세요."); return; }
            if (allButton == null || !allButton.Enabled) return;
            allButton.Enabled = false; chrome.SetBusy(true); edge.SetBusy(true);
            Log((automatic ? "자동" : "수동") + " 정리 시작: " + (target == "all" ? "Chrome + Edge" : target));
            Task.Run(() =>
            {
                var r = new List<string>();
                foreach (string p in (target == "all" ? new[] { "chrome", "msedge" } : new[] { target }))
                {
                    TrimResult t = BrowserEngine.Trim(p);
                    long delta = t.Before - t.After;
                    r.Add((p == "chrome" ? "Chrome" : "Edge") + ": 성공 " + t.Success + "개 / 실패 " + t.Failed +
                        "개 · 작업 집합 표시량 " + (delta >= 0 ? "-" : "+") + Style.MB(Math.Abs(delta)));
                }
                return r;
            }).ContinueWith(t =>
            {
                if (IsDisposed || Disposing) return;
                allButton.Enabled = true;
                chrome.SetBusy(false); edge.SetBusy(false);
                if (t.IsFaulted) Log("정리 오류: " + t.Exception.GetBaseException().Message);
                else foreach (string line in t.Result) Log(line);
                // No claim that a transient working-set reduction equals a permanent RAM saving.
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}