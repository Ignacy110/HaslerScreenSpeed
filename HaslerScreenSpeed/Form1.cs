
using System.IO.Ports;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace HaslerScreenSpeed
{
    public partial class Form1 : Form
    {
        private SerialPort _serialPort;
        private System.Windows.Forms.Timer _loopTimer;
        private Dictionary<int, Mat> _templates = new Dictionary<int, Mat>();

        // Data flow control flag: ensures we only send the next frame after receiving an acknowledgment
        private bool _canSendNext = false;

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
            // Attach an event handler to manage asynchronous incoming serial data
            _serialPort.DataReceived += SerialPort_DataReceived;

            _loopTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _loopTimer.Tick += LoopTimer_Tick;
        }

        /// <summary>
        /// Loads template images (0-9.png) from the application directory for custom OCR template matching.
        /// </summary>
        private void LoadTemplates()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "templates\\";
                for (int i = 0; i <= 9; i++)
                {
                    // Load templates in grayscale mode to simplify and optimize matching
                    Mat t = Cv2.ImRead($"{path}{i}.png", ImreadModes.Grayscale);
                    if (!t.Empty()) _templates.Add(i, t);
                    else MessageBox.Show($"Error loading template: {i}.png");
                }
            }
            catch (Exception ex) { MessageBox.Show("Template loading error: " + ex.Message); }
        }

        // --- MICROCONTROLLER DATA RECEIVE HANDLER ---
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Verify if the buffer contains a full 20-byte data frame from the microcontroller
            if (_serialPort.BytesToRead >= 20)
            {
                byte[] inBuffer = new byte[20];
                _serialPort.Read(inBuffer, 0, 20);

                // Validate the response frame header (0xEF 0xEF)
                if (inBuffer[0] == 0xEF && inBuffer[1] == 0xEF)
                {
                    // Handshake success: allow the main loop to send the next speed value
                    _canSendNext = true;
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

                    // STEP 1: Transmit the initial data frame to kickstart the ping-pong communication cycle
                    int initialSpeed = GetSpeedFromScreen();
                    SendHaslerData((byte)Math.Min(initialSpeed, 255));

                    // Block transmission until the microcontroller acknowledges this initial frame
                    _canSendNext = false;

                    _loopTimer.Start();
                    MessageBox.Show("Communication started (Ping-Pong mode).");
                }
            }
            catch (Exception ex) { MessageBox.Show("Port opening error: " + ex.Message); }
        }

        private void buttonComStop_Click(object sender, EventArgs e)
        {
            _loopTimer.Stop();
            if (_serialPort.IsOpen) _serialPort.Close();
            _canSendNext = false;
            MessageBox.Show("Stopped.");
        }

        private void LoopTimer_Tick(object sender, EventArgs e)
        {
            // STEPS 2 & 3: Transmit data only if the timer ticks AND the acknowledgment flag is true
            if (_serialPort.IsOpen && _canSendNext)
            {
                int speed = GetSpeedFromScreen();

                // Safely update the Windows Forms UI from the background thread context
                this.Invoke((MethodInvoker)delegate {
                    labelCurrentSpeed.Text = $"{speed} km/h";
                });

                // Lock data flow immediately before sending the next packet
                _canSendNext = false;
                SendHaslerData((byte)Math.Min(speed, 255));
            }
        }

        /// <summary>
        /// Assembles and writes a 52-byte payload with a sync header to the open serial interface.
        /// </summary>
        private void SendHaslerData(byte speed)
        {
            byte[] buffer = new byte[52];
            // Write 4-byte synchronization header sequence
            buffer[0] = 0xEF; buffer[1] = 0xEF; buffer[2] = 0xEF; buffer[3] = 0xEF;
            buffer[4] = speed;

            try { _serialPort.Write(buffer, 0, buffer.Length); }
            catch { _loopTimer.Stop(); }
        }

        /// <summary>
        /// Captures the designated desktop area and extracts numerical values using OpenCV template matching.
        /// </summary>
        private int GetSpeedFromScreen()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(_capW, _capH))
                {
                    // Step 1: Perform screen capture of the configured desktop coordinates
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(_capX, _capY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                    }

                    // Debug utility: uncomment to save captured frame intervals to disk
                    // string fileName = $"debug_{DateTime.Now:HH-mm-ss-fff}.png";
                    // bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

                    // Step 2: Convert system Bitmap object into an OpenCV grayscale Mat
                    Mat source = bmp.ToMat();
                    Mat graySource = new Mat();
                    Cv2.CvtColor(source, graySource, ColorConversionCodes.BGR2GRAY);

                    List<DetectedDigit> foundDigits = new List<DetectedDigit>();

                    // Step 3: Scan the captured area using all preloaded templates (0-9)
                    foreach (var template in _templates)
                    {
                        using (Mat res = new Mat())
                        {
                            Cv2.MatchTemplate(graySource, template.Value, res, TemplateMatchModes.CCoeffNormed);

                            // Inspect match results across the matrix
                            for (int y = 0; y < res.Rows; y++)
                            {
                                for (int x = 0; x < res.Cols; x++)
                                {
                                    float score = res.At<float>(y, x);
                                    // Collect detections that meet or exceed our minimum confidence threshold
                                    if (score >= _threshold)
                                    {
                                        foundDigits.Add(new DetectedDigit { Value = template.Key, X = x, Confidence = score });
                                    }
                                }
                            }
                        }
                    }

                    // Step 4: Proximity filtering / Non-maximum suppression. Sort detections by highest confidence first.
                    var finalDigits = foundDigits
                        .OrderByDescending(d => d.Confidence)
                        .ToList();

                    for (int i = 0; i < finalDigits.Count; i++)
                    {
                        // Deduplication: if another match sits within 10 pixels on the X-axis, 
                        // treat it as the same digit instance and drop the lower confidence score.
                        finalDigits.RemoveAll(d => d != finalDigits[i] && Math.Abs(d.X - finalDigits[i].X) < 10);
                    }

                    // Step 5: Order finalized single digits from left to right to reconstruct the final speed string
                    string result = string.Concat(finalDigits.OrderBy(d => d.X).Select(d => d.Value));

                    return int.TryParse(result, out int speed) ? speed : 0;
                }
            }
            catch (Exception ex)
            {
                // Print execution anomalies directly into the Visual Studio Output panel
                System.Diagnostics.Debug.WriteLine("Capture or tracking error: " + ex.Message);
                return 0;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Release unmanaged OpenCV mats, stop timers, and shut down serial connections safely
            _loopTimer?.Stop();
            if (_serialPort != null && _serialPort.IsOpen) _serialPort.Close();
            foreach (var t in _templates.Values) t.Dispose();
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