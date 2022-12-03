using NAudio.Wave;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Runtime.ExceptionServices;
using RecordingCORE;
using System.Linq;

namespace ApplicationRecorderCORE
{
    public partial class Form1 : Form
    {
        List<Bitmap> allStreams = new List<Bitmap>();
        List<Bitmap> allBitmaps = new List<Bitmap>();
        bool isRecording = false;

        

        int frmFrameRate = 30;
        string frmVideoPath = "hey.mp4";
        IntPtr frmWindHandle = (IntPtr)IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //IntPtr hWnd = (IntPtr)FindWindow(windowName, null);

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
                if (new FileInfo(directoryFile).Name.Contains(frmVideoPath) || new FileInfo(directoryFile).Name.Contains("1"))
                    File.Delete(directoryFile);
            }
            MessageBox.Show("Recording is ready, Press OK button to begin.", "Alert!", MessageBoxButtons.OK);
            Task.Run(() => RecordFrames(frmWindHandle));
            Task.Run(() => RecordSounds());
        }

        private void RecordSounds()
        {
            if (radioButton1.Checked)
            {
                Recorder.StartSysSoundRecording("1.wav");
            }
            else if (radioButton2.Checked)
            {
                Recorder.StartMicSoundRecording("2.wav");
            }
            else if (radioButton3.Checked)
            {
                Recorder.StartSysSoundRecording("1.wav");
                Recorder.StartMicSoundRecording("2.wav");
            }
        }

        

        

        private void RecordFrames(IntPtr windHandle)
        {
            int i = 0;

            // Check if FullScreen or not.
            if (checkBox1.Checked)
            {
                //Recorder.StartFullScreenRecording(allStreams);
                Recorder.StartFullScreenSimpleRecording();
            }
            else
            {
                Recorder.StartWindowScreenRecording(windHandle, allBitmaps);
            }
            
        }

        // Stop
        private void button2_Click(object sender, EventArgs e)
        {
            isRecording= false;
            Recorder.StopMicSoundRecording();
            Recorder.StopSysSoundRecording();
            Recorder.StopFullScreenRecording();
            Recorder.StopWindowScreenRecording();
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
            Thread.Sleep(500);
            for (int i = 0; i < allStreams.Count; i++)
            {
                allStreams[i].Save(parentPath + $"{i}.png", ImageFormat.Png);
                allStreams[i].Dispose();
            }
            allStreams.Clear();

            for (int i = 0; i < allBitmaps.Count; i++)
            {
                allBitmaps[i].Save(parentPath + $"{i}.png", ImageFormat.Png);
                allBitmaps[i].Dispose();
            }
            allBitmaps.Clear();


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
                    $".\\ffmpeg -i 1.wav -i 2.wav -filter_complex amix=inputs=2:duration=longest output.wav";
                ffmpegProc.Start();
                ffmpegProc.WaitForExit();
            }

            // Now check if there is a created mp4 video file, because of simple recording for example.
            string[] mp4Files = Directory.GetFiles(parentPath, "*.mp4");
            if (mp4Files.Length == 1)
            {
                ffmpegProc.StartInfo.Arguments
                =
                $".\\ffmpeg -i 1.mp4 -i output.wav -c:v copy -c:a aac {videoPath}.mp4";
                ffmpegProc.Start();
                ffmpegProc.WaitForExit();
            }
            else if (mp4Files.Length == 0)
            {
                ffmpegProc.StartInfo.Arguments
                =
                $".\\ffmpeg -pattern_type sequence -i '%d.png' -i 'output.wav' -c:v libx264 -pix_fmt yuv420p '{videoPath}.mp4'";
                ffmpegProc.Start();
                ffmpegProc.WaitForExit();
            }

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
            string[] directoryFiles3 = Directory.GetFiles(parentPath, "*.mp4");
            foreach (string directoryFile in directoryFiles3)
            {
                if(new FileInfo(directoryFile).Name.Contains("1"))
                    File.Delete(directoryFile);
            }
            if (ffmpegProc.ExitCode == 0)
            {
                MessageBox.Show("Video saved successfully!");
            }
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
            Recorder.StopMicSoundRecording();
            Recorder.StopSysSoundRecording();
            Recorder.StopFullScreenRecording();
            Recorder.StopWindowScreenRecording();
            isRecording = false;
            Environment.Exit(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null && comboBox1.SelectedItem.ToString() != "")
            {
                textBox3.Text = string.Format("{0:X8}", new IntPtr(int.Parse(comboBox1.SelectedItem.GetType().GetProperty("Value").GetValue(comboBox1.SelectedItem, null).ToString())));
            }
        }

        [HandleProcessCorruptedStateExceptionsAttribute()]
        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            comboBox1.DisplayMember = "Text";
            comboBox1.ValueMember = "Value";
            comboBox1.Items.Clear();
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    comboBox1.Items.Add(new { Text = p.MainWindowTitle, Value = p.MainWindowHandle });
                }
            }
        }
    }

    
}