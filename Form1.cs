using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Meow
{
    public partial class Form1 : Form
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;

        private Dictionary<Keys, string> soundMap;
        private WaveOutEvent waveOut;
        private AudioFileReader audioReader;
        private static Form1 _instance;

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        public Form1()
        {
            InitializeComponent();
            _instance = this;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            InitSoundMap();
            InitTrayIcon();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false; // 不在工作列顯示
            //notifyIcon.Icon = new Icon("resources/meow.ico");
        }
        private void InitTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("結束");
            exitItem.Click += (s, e) => Application.Exit();
            trayMenu.Items.Add(exitItem);

            notifyIcon = new NotifyIcon
            {
                //Icon = SystemIcons.Information, // 可換成你自己的圖示 *.ico
                Icon = new Icon("img/icon.ico"),
                ContextMenuStrip = trayMenu,
                Text = "喵喵鍵盤",
                Visible = true
            };
            notifyIcon.DoubleClick += (s, e) =>
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.BringToFront();
            };
        }
        private void InitSoundMap()
        {
            soundMap = new Dictionary<Keys, string>
        {
            { Keys.A, "sounds/cat1.mp3" },
            { Keys.S, "sounds/cat2.mp3" },
            { Keys.F, "sounds/cat4.mp3" }
        };
        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x0100;

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                //if (_instance.soundMap.ContainsKey(key))
                //{
                //    _instance.PlaySound(_instance.soundMap[key]);
                //}
                // 不再限制特定按鍵，所有鍵皆觸發
                _instance.PlayRandomSound();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void PlaySound(string filePath)
        {
            StopCurrentSound();

            if (!System.IO.File.Exists(filePath)) 
            {
                MessageBox.Show("找不到音檔：" + filePath);
                return;
            }


            audioReader = new AudioFileReader(filePath);
            waveOut = new WaveOutEvent();
            waveOut.Init(audioReader);
            waveOut.Play();
        }
        private void PlayRandomSound()
        {
            StopCurrentSound();

            if (soundMap.Count == 0) return;

            var random = new Random();
            var randomSoundPath = soundMap.Values.ElementAt(random.Next(soundMap.Count));

            if (!System.IO.File.Exists(randomSoundPath))
            {
                MessageBox.Show("找不到音檔：" + randomSoundPath);
                return;
            }

            audioReader = new AudioFileReader(randomSoundPath);
            waveOut = new WaveOutEvent();
            waveOut.Init(audioReader);
            waveOut.Play();
        }

        private void StopCurrentSound()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            audioReader?.Dispose();
            audioReader = null;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopCurrentSound();                  // 釋放音效
            UnhookWindowsHookEx(_hookID);        // 移除 Hook
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            trayMenu.Dispose();
            base.OnFormClosing(e);
        }

        // Win32 API declarations
        private const int WH_KEYBOARD_LL = 13;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
