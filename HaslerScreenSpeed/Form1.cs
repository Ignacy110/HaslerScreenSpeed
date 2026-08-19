using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.Json;
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
        private readonly Dictionary<int, Mat> _templates = new Dictionary<int, Mat>();

        private volatile bool _canSendNext = false;

        private readonly List<byte> _serialBuffer = new List<byte>();
        private readonly object _serialLock = new object();

        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private readonly double _threshold = 0.75;

        // Zmienne pomocnicze dla bufora braku odczytu cyfr
        private DateTime? _noDigitStartTime = null;
        private int _lastValidSpeed = 0;

        public Form1()
        {
            InitializeComponent();
            LoadTemplates();
            InitializeSerial();

            LoadSettings();
        }

        private void InitializeSerial()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += SerialPort_DataReceived;

            _loopTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _loopTimer.Tick += LoopTimer_Tick;
        }

        #region Obs³uga Ustawieñ (Save / Load Config) i Portów COM

        private void RefreshComPorts(string savedPortName)
        {
            comboBoxComNumber.Items.Clear();
            string[] availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length > 0)
            {
                comboBoxComNumber.Items.AddRange(availablePorts);

                if (!string.IsNullOrEmpty(savedPortName) && availablePorts.Contains(savedPortName))
                {
                    comboBoxComNumber.SelectedItem = savedPortName;
                }
                else
                {
                    comboBoxComNumber.SelectedIndex = 0;
                }
            }
            else
            {
                comboBoxComNumber.Text = string.Empty;
            }
        }

        private void LoadSettings()
        {
            AppSettings settings = null;

            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    settings = JsonSerializer.Deserialize<AppSettings>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("B³¹d odczytu pliku konfiguracyjnego: " + ex.Message);
            }

            if (settings == null)
            {
                settings = new AppSettings();
            }

            textBoxSpeedX.Text = settings.CapX.ToString();
            textBoxSpeedY.Text = settings.CapY.ToString();
            textBoxSpeedLength.Text = settings.CapW.ToString();
            textBoxSpeedHeight.Text = settings.CapH.ToString();
            textBoxComSpeed.Text = settings.BaudRate.ToString();
            textBoxWaitingTime.Text = settings.WaitingTime.ToString();

            RefreshComPorts(settings.PortName);
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    CapX = int.TryParse(textBoxSpeedX.Text, out int x) ? x : 0,
                    CapY = int.TryParse(textBoxSpeedY.Text, out int y) ? y : 0,
                    CapW = int.TryParse(textBoxSpeedLength.Text, out int w) ? w : 100,
                    CapH = int.TryParse(textBoxSpeedHeight.Text, out int h) ? h : 100,
                    BaudRate = int.TryParse(textBoxComSpeed.Text, out int b) ? b : 115200,
                    PortName = comboBoxComNumber.SelectedItem?.ToString() ?? comboBoxComNumber.Text.Trim(),
                    WaitingTime = double.TryParse(textBoxWaitingTime.Text, out double wt) ? wt : 3.0
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("B³¹d zapisu pliku konfiguracyjnego: " + ex.Message);
            }
        }

        #endregion

        private void LoadTemplates()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
                for (int i = 0; i <= 9; i++)
                {
                    string filePath = Path.Combine(path, $"{i}.png");
                    Mat t = Cv2.ImRead(filePath, ImreadModes.Grayscale);
                    if (!t.Empty())
                    {
                        _templates.Add(i, t);
                    }
                    else
                    {
                        MessageBox.Show($"B³¹d ³adowania szablonu: {i}.png");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("B³¹d krytyczny podczas ³adowania szablonów: " + ex.Message);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_serialPort.IsOpen) return;

            int bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead == 0) return;

            byte[] tempBuffer = new byte[bytesToRead];
            _serialPort.Read(tempBuffer, 0, bytesToRead);

            lock (_serialLock)
            {
                _serialBuffer.AddRange(tempBuffer);

                while (_serialBuffer.Count >= 20)
                {
                    if (_serialBuffer[0] == 0xEF && _serialBuffer[1] == 0xEF)
                    {
                        _canSendNext = true;
                        _serialBuffer.RemoveRange(0, 20);
                    }
                    else
                    {
                        _serialBuffer.RemoveAt(0);
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
                    string selectedPort = comboBoxComNumber.SelectedItem?.ToString() ?? comboBoxComNumber.Text.Trim();

                    if (string.IsNullOrEmpty(selectedPort))
                    {
                        MessageBox.Show("Brak wybranego lub dostêpnego portu COM w systemie.");
                        return;
                    }

                    _serialPort.PortName = selectedPort;

                    if (!int.TryParse(textBoxComSpeed.Text.Trim(), out int baudRate))
                    {
                        MessageBox.Show("Nieprawid³owa prêdkoœæ portu COM (Baud Rate).");
                        return;
                    }
                    _serialPort.BaudRate = baudRate;
                    _serialPort.Open();

                    lock (_serialLock)
                    {
                        _serialBuffer.Clear();
                    }

                    // Resetujemy zmienne pomocnicze
                    _lastValidSpeed = 0;
                    _noDigitStartTime = null;

                    _loopTimer.Start();

                    // Na starcie transmisji wysy³amy ramkê z prêdkoœci¹ 0 km/h
                    SendHaslerData(0);

                    MessageBox.Show($"Po³¹czenie uruchomione na porcie {selectedPort} (Tryb Ping-Pong).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("B³¹d otwarcia portu: " + ex.Message);
            }
        }

        private void buttonComStop_Click(object sender, EventArgs e)
        {
            StopCommunication();
            MessageBox.Show("Po³¹czenie zatrzymane.");
        }

        private void StopCommunication()
        {
            _loopTimer.Stop();
            _canSendNext = false;

            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // Przy zatrzymaniu transmisji wysy³amy prêdkoœæ 0 km/h
                    SendHaslerData(0);
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("B³¹d podczas wysy³ania ramki zeruj¹cej: " + ex.Message);
                }
                finally
                {
                    try { _serialPort.Close(); } catch { }
                }
            }
        }

        private async void LoopTimer_Tick(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen && _canSendNext)
            {
                _canSendNext = false;

                int capX = int.TryParse(textBoxSpeedX.Text, out int x) ? x : 0;
                int capY = int.TryParse(textBoxSpeedY.Text, out int y) ? y : 0;
                int capW = int.TryParse(textBoxSpeedLength.Text, out int w) ? w : 100;
                int capH = int.TryParse(textBoxSpeedHeight.Text, out int h) ? h : 100;

                int? detectedSpeed = await Task.Run(() => GetSpeedFromScreen(capX, capY, capW, capH));

                if (detectedSpeed.HasValue)
                {
                    // WYKRYTO LICZBÊ
                    _noDigitStartTime = null;
                    _lastValidSpeed = detectedSpeed.Value;

                    labelCurrentSpeed.Text = $"{_lastValidSpeed} km/h";
                    SendHaslerData((byte)Math.Min(_lastValidSpeed, 255));
                }
                else
                {
                    // NIE WYKRYTO ¯ADNEJ LICZBY
                    labelCurrentSpeed.Text = "-";

                    if (_noDigitStartTime == null)
                    {
                        _noDigitStartTime = DateTime.Now;
                    }

                    double waitingTime = double.TryParse(textBoxWaitingTime.Text, out double wt) ? wt : 3.0;
                    double elapsedSeconds = (DateTime.Now - _noDigitStartTime.Value).TotalSeconds;

                    if (elapsedSeconds >= waitingTime)
                    {
                        // Czas min¹³ – wysy³amy 0 km/h
                        SendHaslerData(0);
                    }
                    else
                    {
                        // Czas nie min¹³ – utrzymujemy ostatni¹ znan¹ prêdkoœæ
                        SendHaslerData((byte)Math.Min(_lastValidSpeed, 255));
                    }
                }
            }
        }

        private void SendHaslerData(byte speed)
        {
            byte[] buffer = new byte[52];
            buffer[0] = 0xEF; buffer[1] = 0xEF; buffer[2] = 0xEF; buffer[3] = 0xEF;
            buffer[4] = speed;

            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                _loopTimer.Stop();
                _canSendNext = false;
                try { _serialPort.Close(); } catch { }
            }
        }

        private int? GetSpeedFromScreen(int capX, int capY, int capW, int capH)
        {
            try
            {
                if (capW <= 0 || capH <= 0) return null;

                using (Bitmap bmp = new Bitmap(capW, capH))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(capX, capY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                    }

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

                                while (true)
                                {
                                    Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                                    if (maxVal >= _threshold)
                                    {
                                        foundDigits.Add(new DetectedDigit
                                        {
                                            Value = template.Key,
                                            X = maxLoc.X,
                                            Width = template.Value.Width,
                                            Confidence = (float)maxVal
                                        });

                                        int startX = Math.Max(0, maxLoc.X - template.Value.Width / 2);
                                        int width = Math.Min(res.Cols - startX, template.Value.Width);
                                        using (Mat roi = new Mat(res, new Rect(startX, 0, width, res.Rows)))
                                        {
                                            roi.SetTo(new Scalar(0));
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        var sortedDigits = foundDigits.OrderByDescending(d => d.Confidence).ToList();
                        var finalDigits = new List<DetectedDigit>();

                        foreach (var digit in sortedDigits)
                        {
                            bool overlaps = false;
                            foreach (var accepted in finalDigits)
                            {
                                int overlapStart = Math.Max(digit.X, accepted.X);
                                int overlapEnd = Math.Min(digit.X + digit.Width, accepted.X + accepted.Width);

                                if (overlapStart < overlapEnd)
                                {
                                    int overlapWidth = overlapEnd - overlapStart;
                                    int minWidth = Math.Min(digit.Width, accepted.Width);

                                    if (overlapWidth > minWidth * 0.3)
                                    {
                                        overlaps = true;
                                        break;
                                    }
                                }
                            }

                            if (!overlaps)
                            {
                                finalDigits.Add(digit);
                            }
                        }

                        // Jeœli nie odnaleziono cyfr, zwracamy null
                        if (finalDigits.Count == 0)
                        {
                            return null;
                        }

                        string result = string.Concat(finalDigits.OrderBy(d => d.X).Select(d => d.Value));
                        return int.TryParse(result, out int speed) ? speed : null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("B³¹d OCR/Przechwytywania: " + ex.Message);
                return null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            StopCommunication();

            _loopTimer?.Dispose();

            if (_serialPort != null)
            {
                _serialPort.Dispose();
            }

            foreach (var t in _templates.Values) t.Dispose();
            base.OnFormClosing(e);
        }
    }

    public class AppSettings
    {
        public int CapX { get; set; } = 0;
        public int CapY { get; set; } = 0;
        public int CapW { get; set; } = 100;
        public int CapH { get; set; } = 100;
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
        public double WaitingTime { get; set; } = 3.0;
    }

    public class DetectedDigit
    {
        public int Value;
        public int X;
        public int Width;
        public float Confidence;
    }
}