
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

        // Logika przep³ywu danych
        private bool _canSendNext = false; // Czy otrzymaliœmy odpowiedŸ i mo¿emy s³aæ dalej?

        private readonly int _capX = 1650;
        private readonly int _capY = 920;
        private readonly int _capW = 126;
        private readonly int _capH = 80;
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
            // Wa¿ne: dodajemy obs³ugê zdarzenia odbioru danych
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
                    if (!t.Empty()) _templates.Add(i, t);
                    else MessageBox.Show($"B³¹d: {i}.png");
                }
            }
            catch (Exception ex) { MessageBox.Show("B³¹d szablonów: " + ex.Message); }
        }

        // --- OBS£UGA ODBIORU DANYCH Z MIKROKONTROLERA ---
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Sprawdzamy czy w buforze jest wystarczaj¹co danych (nasza ramka ma 20 bajtów)
            if (_serialPort.BytesToRead >= 20)
            {
                byte[] inBuffer = new byte[20];
                _serialPort.Read(inBuffer, 0, 20);

                // Sprawdzamy nag³ówek, aby upewniæ siê, ¿e to poprawna ramka
                if (inBuffer[0] == 0xEF && inBuffer[1] == 0xEF)
                {
                    // Sukces! Otrzymaliœmy odpowiedŸ, pozwalamy na kolejn¹ wysy³kê
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

                    // KROK 1: Wysy³amy pierwsz¹ ramkê na "rozruch"
                    int initialSpeed = GetSpeedFromScreen();
                    SendHaslerData((byte)Math.Min(initialSpeed, 255));

                    // Czekamy na odpowiedŸ (flaga zostanie ustawiona w DataReceived)
                    _canSendNext = false;

                    _loopTimer.Start();
                    MessageBox.Show("Komunikacja wystartowa³a (Ping-Pong mode).");
                }
            }
            catch (Exception ex) { MessageBox.Show("B³¹d portu: " + ex.Message); }
        }

        private void buttonComStop_Click(object sender, EventArgs e)
        {
            _loopTimer.Stop();
            if (_serialPort.IsOpen) _serialPort.Close();
            _canSendNext = false;
            MessageBox.Show("Zatrzymano.");
        }

        private void LoopTimer_Tick(object sender, EventArgs e)
        {
            // KROK 2 i 3: Wysy³amy kolejn¹ ramkê tylko jeœli min¹³ czas (Timer) 
            // ORAZ otrzymaliœmy odpowiedŸ (_canSendNext)
            if (_serialPort.IsOpen && _canSendNext)
            {
                int speed = GetSpeedFromScreen();

                // Aktualizacja UI
                this.Invoke((MethodInvoker)delegate {
                    labelCurrentSpeed.Text = $"{speed} km/h";
                });

                // Wysy³amy dane
                _canSendNext = false; // Blokujemy do czasu otrzymania nowej odpowiedzi
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

                    //// Nazwa pliku bêdzie zawieraæ godziny, minuty, sekundy i milisekundy
                    //string fileName = $"debug_{DateTime.Now:HH-mm-ss-fff}.png";
                    //bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

                    // Konwersja na format OpenCV (Skala szaroœci)
                    Mat source = bmp.ToMat();
                    Mat graySource = new Mat();
                    Cv2.CvtColor(source, graySource, ColorConversionCodes.BGR2GRAY);

                    List<DetectedDigit> foundDigits = new List<DetectedDigit>();

                    // Skanujemy obszar ka¿dym szablonem (0-9)
                    foreach (var template in _templates)
                    {
                        using (Mat res = new Mat())
                        {
                            Cv2.MatchTemplate(graySource, template.Value, res, TemplateMatchModes.CCoeffNormed);

                            // Przeszukujemy wyniki dopasowania
                            for (int y = 0; y < res.Rows; y++)
                            {
                                for (int x = 0; x < res.Cols; x++)
                                {
                                    float score = res.At<float>(y, x);
                                    if (score >= _threshold)
                                    {
                                        foundDigits.Add(new DetectedDigit { Value = template.Key, X = x, Confidence = score });
                                    }
                                }
                            }
                        }
                    }

                    // Czyszczenie wyników: jeœli kilka punktów wskazuje tê sam¹ cyfrê obok siebie, wybierz najlepszy
                    var finalDigits = foundDigits
                        .OrderByDescending(d => d.Confidence)
                        .ToList();

                    for (int i = 0; i < finalDigits.Count; i++)
                    {
                        // Jeœli inna znaleziona cyfra jest bli¿ej ni¿ 10 pikseli - usuñ j¹ (to ta sama cyfra)
                        finalDigits.RemoveAll(d => d != finalDigits[i] && Math.Abs(d.X - finalDigits[i].X) < 10);
                    }

                    // Sortowanie od lewej do prawej i sk³adanie liczby
                    string result = string.Concat(finalDigits.OrderBy(d => d.X).Select(d => d.Value));

                    return int.TryParse(result, out int speed) ? speed : 0;
                }
            }
            catch (Exception ex)
            {
                // Wypisze b³¹d w dolnym oknie Visual Studio
                System.Diagnostics.Debug.WriteLine("B³¹d przechwytywania: " + ex.Message);
                return 0;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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