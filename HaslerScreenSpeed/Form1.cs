using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace HaslerScreenSpeed
{
    public partial class Form1 : Form
    {
        private SerialPort _serialPort;
        private System.Windows.Forms.Timer _loopTimer;
        private Dictionary<int, Mat> _templates = new Dictionary<int, Mat>();

        // Volatile variable ensures cross-thread visibility between the UI thread and the serial port thread
        private volatile bool _canSendNext = false;

        // Thread-safe buffer and lock object for serial communication
        private List<byte> _serialBuffer = new List<byte>();
        private readonly object _serialLock = new object();

        // Bounding box configuration for screen capture (X, Y, Width, Height)
        private readonly int _capX = 1650;
        private readonly int _capY = 920;
        private readonly int _capW = 126;
        private readonly int _capH = 80;

        // Confidence threshold for template matching (ranges from 0.0 to 1.0)
        private readonly double _threshold = 0.75;

        public Form1()
        {
            InitializeComponent();
            LoadTemplates();
            InitializeSerial();
        }

        private void InitializeSerial()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += SerialPort_DataReceived;

            _loopTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _loopTimer.Tick += LoopTimer_Tick;
        }

        private void LoadTemplates()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "templates\\";
                for (int i = 0; i <= 9; i++)
                {
                    Mat t = Cv2.ImRead($"{path}{i}.png", ImreadModes.Grayscale);
                    if (!t.Empty())
                        _templates.Add(i, t);
                    else
                        MessageBox.Show($"Error loading template: {i}.png");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Template loading error: " + ex.Message);
            }
        }

        // --- MICROCONTROLLER DATA RECEIVE HANDLER ---
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead == 0) return;

            byte[] tempBuffer = new byte[bytesToRead];
            _serialPort.Read(tempBuffer, 0, bytesToRead);

            // Protect against frame fragmentation and corrupted/garbage data
            lock (_serialLock)
            {
                _serialBuffer.AddRange(tempBuffer);

                while (_serialBuffer.Count >= 20)
                {
                    if (_serialBuffer[0] == 0xEF && _serialBuffer[1] == 0xEF)
                    {
                        _canSendNext = true;
                        _serialBuffer.RemoveRange(0, 20); // Remove the complete, valid data frame
                    }
                    else
                    {
                        _serialBuffer.RemoveAt(0); // Invalid header - shift by 1 byte and continue searching
                    }
                }
            }
        }

        private void buttonComStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.PortName = textBoxComNumber.Text.Trim();
                    _serialPort.BaudRate = int.Parse(textBoxComSpeed.Text.Trim());
                    _serialPort.Open();

                    lock (_serialLock) { _serialBuffer.Clear(); } // Clear buffer before starting
                    _canSendNext = false;

                    // Fetch the initial speed (blocks the UI only for a fraction of a second at startup)
                    int initialSpeed = GetSpeedFromScreen();
                    SendHaslerData((byte)Math.Min(initialSpeed, 255));

                    _loopTimer.Start();
                    MessageBox.Show("Communication started (Ping-Pong mode).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Port opening error: " + ex.Message);
            }
        }

        private void buttonComStop_Click(object sender, EventArgs e)
        {
            _loopTimer.Stop();
            if (_serialPort.IsOpen) _serialPort.Close();
            _canSendNext = false;
            MessageBox.Show("Stopped.");
        }

        // Using async void allows using 'await' inside the Timer tick event handler
        private async void LoopTimer_Tick(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen && _canSendNext)
            {
                _canSendNext = false; // Block immediately for the duration of processing time

                // Offload the heavy OCR operation to a background thread (Thread Pool)
                int speed = await Task.Run(() => GetSpeedFromScreen());

                // After the 'await', execution automatically returns to the interface thread (UI Thread).
                // Control.Invoke() is no longer required!
                labelCurrentSpeed.Text = $"{speed} km/h";

                SendHaslerData((byte)Math.Min(speed, 255));
            }
        }

        private void SendHaslerData(byte speed)
        {
            byte[] buffer = new byte[52];
            buffer[0] = 0xEF; buffer[1] = 0xEF; buffer[2] = 0xEF; buffer[3] = 0xEF;
            buffer[4] = speed;

            try { _serialPort.Write(buffer, 0, buffer.Length); }
            catch { _loopTimer.Stop(); }
        }

        private int GetSpeedFromScreen()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(_capW, _capH))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(_capX, _capY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                    }

                    // Proper management of unmanaged memory for Mat instances using 'using' statements
                    using (Mat source = bmp.ToMat())
                    using (Mat graySource = new Mat())
                    {
                        Cv2.CvtColor(source, graySource, ColorConversionCodes.BGR2GRAY);
                        List<DetectedDigit> foundDigits = new List<DetectedDigit>();

                        foreach (var template in _templates)
                        {
                            using (Mat res = new Mat())
                            {
                                Cv2.MatchTemplate(graySource, template.Value, res, TemplateMatchModes.CCoeffNormed);

                                // Fast peak location instead of slow pixel-by-pixel nested loop iterations
                                while (true)
                                {
                                    Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                                    if (maxVal >= _threshold)
                                    {
                                        foundDigits.Add(new DetectedDigit { Value = template.Key, X = maxLoc.X, Confidence = (float)maxVal });

                                        // Clear out the detected region to potentially find the same digit adjacent to it (e.g., "11")
                                        int startX = Math.Max(0, maxLoc.X - template.Value.Width / 2);
                                        int width = Math.Min(res.Cols - startX, template.Value.Width);
                                        using (Mat roi = new Mat(res, new Rect(startX, 0, width, res.Rows)))
                                        {
                                            roi.SetTo(new Scalar(0)); // Reset match scores in this specific region to 0
                                        }
                                    }
                                    else
                                    {
                                        break; // Break the loop when there are no more matches for this digit
                                    }
                                }
                            }
                        }

                        // Proximity filtering / Non-maximum suppression
                        var finalDigits = foundDigits.OrderByDescending(d => d.Confidence).ToList();

                        for (int i = 0; i < finalDigits.Count; i++)
                        {
                            finalDigits.RemoveAll(d => d != finalDigits[i] && Math.Abs(d.X - finalDigits[i].X) < 10);
                        }

                        string result = string.Concat(finalDigits.OrderBy(d => d.X).Select(d => d.Value));
                        return int.TryParse(result, out int speed) ? speed : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Capture or tracking error: " + ex.Message);
                return 0;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _loopTimer?.Stop();
            if (_serialPort != null && _serialPort.IsOpen) _serialPort.Close();
            foreach (var t in _templates.Values) t.Dispose(); // Release template resources loaded from disk
            base.OnFormClosing(e);
        }
    }

    public class DetectedDigit
    {
        public int Value;
        public int X;
        public float Confidence;
    }
}