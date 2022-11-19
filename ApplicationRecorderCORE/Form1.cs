using NAudio.Wave;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ApplicationRecorderCORE
{
    public partial class Form1 : Form
    {
        List<MemoryStream> allStreams = new List<MemoryStream>();
        bool isRecording = false;

        // SharpDX instance
        ScreenStateLogger screenstatelogger;

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
        private async void button1_Click(object sender, EventArgs e)
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
            string[] directoryFiles3 = Directory.GetFiles(Directory.GetParent(Environment.ProcessPath).FullName, "*.mp4");
            foreach (string directoryFile in directoryFiles3)
            {
                if (new FileInfo(directoryFile).Name == frmVideoPath)
                    File.Delete(directoryFile);
            }
            MessageBox.Show("Recording is ready, Press OK button to begin.", "Alert!", MessageBoxButtons.OK);
            Task.Run(
                () => RecordFrames(frmWindHandle));
            //Thread rcrdFrm = new Thread(
            //    () => RecordFrames(frmWindHandle));
            Task.Run(
                () => RecordingBeing());
            //Thread rcrdSnd = new Thread(
            //    () => RecordingBeing());
            //rcrdFrm.Start();
            //rcrdSnd.Start();
        }

        private void RecordingBeing()
        {
            //Thread sysT = new Thread(
            //    () => SysRecordSound());
            //Thread micT = new Thread(
            //    () => MicRecordSound());
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
                SysRecordSound();
                MicRecordSound();
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
                string parentPath = Directory.GetParent(Environment.ProcessPath).FullName + "\\";
                //Thread job = new Thread(() =>
                //{
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
                {
                    GetScreenPicture(bmp, bounds, i++, parentPath);
                }
                //});
                //job.Start();
            }
            else
            {
                while (isRecording)
                {
                    PrintWindow(windHandle).Save($"{i++}.png", ImageFormat.Png);
                }
            }
            
        }

        private void GetScreenPicture(Bitmap bmp, Rectangle bounds, int indexer, string ParentPath)
        {
            //using (Graphics g = Graphics.FromImage(bmp))
            //{
            //    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            //}
            //bmp.Save(ParentPath + $"{indexer}.png", ImageFormat.Png);
            screenstatelogger = new ScreenStateLogger();
            screenstatelogger.ScreenRefreshed += (sender, data) =>
            {
                //new frame in data
            };
            screenstatelogger.Start(allStreams);
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
            screenstatelogger.Stop();
            SaveFullVideo(frmVideoPath, frmFrameRate);
        }

        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private void SaveFullVideo(string videoPath, int frameRate)
        {
            string parentPath = Directory.GetParent(Environment.ProcessPath).FullName + "\\";
            // Wait for pending operations.
            Thread.Sleep(500);
            // Write Images so ffmpeg can use them.
            //var EncoderParamaters = new EncoderParameters(1);
            //EncoderParamaters.Param[0] = new EncoderParameter(Encoder.Quality, (long)90);
            //for (int i = 0; i < allPictures.Count; i++)
            //{
            //    allPictures[i].Save(parentPath + $"{i}.png", GetEncoderInfo("image/png"), EncoderParamaters);
            //}

            for (int i = 0; i < allStreams.Count; i++)
            {
                var tempBitmap = new Bitmap(allStreams[i]);
                tempBitmap.Save(parentPath + $"{i}.png", ImageFormat.Png);
            }
            allStreams.Clear();


            Process ffmpegProc = new Process();
            ffmpegProc.StartInfo.FileName = "powershell";
            ffmpegProc.StartInfo.CreateNoWindow = false;
            // Concat all wav files, incase we are recording both audios.
            string[] wavFiles = Directory.GetFiles(parentPath, "*.wav");
            if (wavFiles.Length == 1)
            {
                foreach (var file in wavFiles)
                {
                    File.Move(file, Path.Combine(parentPath, "output.wav"));
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
            ffmpegProc.WaitForExit();
            // Delete previous recorded files, If found.
            string[] directoryFiles1 = Directory.GetFiles(parentPath, "*.png");
            foreach (string directoryFile in directoryFiles1)
            {
                File.Delete(directoryFile);
            }
            string[] directoryFiles2 = Directory.GetFiles(parentPath, "*.wav");
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
            if (textBox1.Text.Trim() != "" && textBox2.Text.Trim() != "" && (textBox3.Text.Trim() != "" || checkBox1.Checked))
            {
                frmFrameRate = Convert.ToInt32(textBox2.Text);
                frmVideoPath = textBox1.Text;
                frmWindHandle = textBox3.Text.Trim() != "" ? (IntPtr)Convert.ToInt32(textBox3.Text, 16) : IntPtr.Zero;
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (screenstatelogger != null)
            {
                screenstatelogger.Stop();
            }
            isRecording = false;
            Environment.Exit(0);
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