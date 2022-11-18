using NAudio.Wave;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ApplicationRecorderCORE
{
    public partial class Form1 : Form
    {
        List<Bitmap> allPictures= new List<Bitmap>();
        bool isRecording = false;

        // Microphone Output
        public WaveInEvent micWaveSource;
        // Speaker Output
        public WasapiLoopbackCapture sysWaveSource;
        
        // Microphone Output
        public WaveFileWriter micWaveFile;
        // Speaker Output
        public WaveFileWriter sysWaveFile;

        int frmFrameRate = 30;
        string frmVideoPath = "1.mp4";
        IntPtr frmWindHandle = (IntPtr)0x208E0;

        public Form1()
        {
            InitializeComponent();
        }

        // Start
        private void button1_Click(object sender, EventArgs e)
        // Test Chrome handle: 0x208E0
        {
            isRecording= true;
            // Delete previous recorded files, If found.
            string[] directoryFiles1 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.png");
            foreach (string directoryFile in directoryFiles1)
            {
                File.Delete(directoryFile);
            }
            string[] directoryFiles2 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.wav");
            foreach (string directoryFile in directoryFiles2)
            {
                File.Delete(directoryFile);
            }
            /*string[] directoryFiles3 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.mp4");
            foreach (string directoryFile in directoryFiles3)
            {
                File.Delete(directoryFile);
            }*/
            MessageBox.Show("Recording is ready, Press OK button to begin.", "Alert!", MessageBoxButtons.OK);
            Task.Run(
                () => RecordFrames(frmWindHandle));
            Task.Run(
                () => RecordingBeing());
        }

        private void RecordingBeing()
        {
            if (radioButton1.Checked)
            {
                SysRecordSound();
            }
            else if (radioButton2.Checked)
            {
                MicRecordSound();
            }
            else if (radioButton3.Checked)
            {
                Task.Run(() => MicRecordSound());
                Task.Run(() => SysRecordSound());
            }
        }

        private void SysRecordSound()
        {
            sysWaveSource = new WasapiLoopbackCapture();
            sysWaveSource.WaveFormat = new WaveFormat(44100, 1);

            sysWaveSource.DataAvailable += new EventHandler<WaveInEventArgs>(sysWaveSource_DataAvailable);
            sysWaveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(sysWaveSource_RecordingStopped);

            sysWaveFile = new WaveFileWriter("1.wav", sysWaveSource.WaveFormat);
            sysWaveSource.StartRecording();
        }

        private void MicRecordSound()
        {
            micWaveSource = new WaveInEvent();
            micWaveSource.WaveFormat = new WaveFormat(44100, 1);

            micWaveSource.DataAvailable += new EventHandler<WaveInEventArgs>(micWaveSource_DataAvailable);
            micWaveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(micWaveSource_RecordingStopped);

            micWaveFile = new WaveFileWriter("2.wav", micWaveSource.WaveFormat);
            micWaveSource.StartRecording();
        }

        void sysWaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (sysWaveFile != null)
            {
                sysWaveFile.Write(e.Buffer, 0, e.BytesRecorded);
                sysWaveFile.Flush();
            }
        }

        void sysWaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (sysWaveFile != null)
            {
                sysWaveFile.Dispose();
                sysWaveFile = null;
            }

            if (sysWaveFile != null)
            {
                sysWaveFile.Dispose();
                sysWaveFile = null;
            }

        }

        void micWaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (micWaveFile != null)
            {
                micWaveFile.Write(e.Buffer, 0, e.BytesRecorded);
                micWaveFile.Flush();
            }
        }

        void micWaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (micWaveFile != null)
            {
                micWaveFile.Dispose();
                micWaveFile = null;
            }

            if (micWaveFile != null)
            {
                micWaveFile.Dispose();
                micWaveFile = null;
            }

        }

        private void RecordFrames(IntPtr windHandle)
        {
            // Check if FullScreen or not.
            int i = 0;
            if (checkBox1.Checked)
            {
                Thread job = new Thread(() =>
                {
                    Rectangle bounds = Screen.GetBounds(Point.Empty);
                    using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            while (isRecording)
                            {
                                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                                bitmap.Save($"{i++}.png", ImageFormat.Png);
                            }
                        }
                    }
                });
                job.Start();
            }
            else
            {
                while (isRecording)
                {
                    PrintWindow(windHandle).Save($"{i++}.png", ImageFormat.Png);
                }
            }
            
        }

        // Stop
        private void button2_Click(object sender, EventArgs e)
        {
            isRecording= false;
            if (micWaveFile != null)
            {
                micWaveSource.StopRecording();
            }
            if (sysWaveFile != null)
            {
                sysWaveSource.StopRecording();
            }
            SaveFullVideo(frmVideoPath, frmFrameRate);
        }

        private void SaveFullVideo(string videoPath, int frameRate)
        {
            Thread.Sleep(1000);
            // .\ffmpeg -framerate 30 -pattern_type sequence -i D:\tempPic\X_%d.png -c:v libx264 -pix_fmt yuv420p out.mp4
            Process ffmpegProc = new Process();
            ffmpegProc.StartInfo.FileName = "powershell";
            ffmpegProc.StartInfo.CreateNoWindow = false;
            // Concat all wav files, incase we are recording both audios.
            string[] wavFiles = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.wav");
            if (wavFiles.Length == 1)
            {
                foreach (var file in wavFiles)
                {
                    File.Move(file, Path.Combine(Directory.GetParent(file).FullName, "output.wav"));
                }
            }
            else
            {
                ffmpegProc.StartInfo.Arguments
                    =
                    $"ffmpeg -i 1.wav -i 2.wav -filter_complex \"[0:a][1:a]amerge=inputs=2,pan=stereo|c0<c0+c2|c1<c1+c3[a]\" -map \"[a]\" output.wav";
                ffmpegProc.Start();
                ffmpegProc.WaitForExit();
            }
            ffmpegProc.StartInfo.Arguments
                =
                $".\\ffmpeg -framerate {frameRate} -pattern_type sequence -i '%d.png' -i 'output.wav' -c:v libx264 -pix_fmt yuv420p '{videoPath}.mp4'";
            ffmpegProc.Start();
            // Unload..
            allPictures.Clear();
            ffmpegProc.WaitForExit();
            // Delete previous recorded files, If found.
            string[] directoryFiles1 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.png");
            foreach (string directoryFile in directoryFiles1)
            {
                File.Delete(directoryFile);
            }
            string[] directoryFiles2 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.wav");
            foreach (string directoryFile in directoryFiles2)
            {
                File.Delete(directoryFile);
            }
            if (ffmpegProc.ExitCode == 0)
            {
                MessageBox.Show("Video saved successfully!");
            }
        }



        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        public static Bitmap PrintWindow(IntPtr hwnd)
        {
            RECT rc;
            GetWindowRect(hwnd, out rc);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            // 2 Value for DirectComposition windows..
            PrintWindow(hwnd, hdcBitmap, 2);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();


            return bmp;
        }

        // Save Settings
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "" && (textBox3.Text != "" || checkBox1.Checked))
            {
                frmFrameRate = Convert.ToInt32(textBox2.Text);
                frmVideoPath = textBox1.Text;
                frmWindHandle = textBox3.Text != "" ? (IntPtr)Convert.ToInt32(textBox3.Text, 16) : IntPtr.Zero;
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                    (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
        {
        }
        public RECT(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left;
            _Top = Top;
            _Right = Right;
            _Bottom = Bottom;
        }

        public int X
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Y
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Left
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Top
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Right
        {
            get { return _Right; }
            set { _Right = value; }
        }
        public int Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }
        public int Height
        {
            get { return _Bottom - _Top; }
            set { _Bottom = value + _Top; }
        }
        public int Width
        {
            get { return _Right - _Left; }
            set { _Right = value + _Left; }
        }
        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }
        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(RECT Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }
        public static implicit operator RECT(Rectangle Rectangle)
        {
            return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }
        public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }
        public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT Rectangle)
        {
            return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT)Object);
            }
            else if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle)Object));
            }

            return false;
        }
    }
}