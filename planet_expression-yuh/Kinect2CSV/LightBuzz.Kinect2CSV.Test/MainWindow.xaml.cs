using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Speech.Synthesis;
using System.Collections.ObjectModel;
using System.Threading;

namespace LightBuzz.Kinect2CSV.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor _sensor = null;

        /* readers */
        BodyFrameReader _reader = null;
        ColorFrameReader _color = null;

        KinectCSVManager _recorder = null;
        WriteableBitmap colorBitmap = null;
        CoordinateMapper coordinateMapper = null;

        Body body = null;

        /* constants */
        const double HandSize = 30;
        const double JointThickness = 3;
        const double ClipBoundsThickness = 10;
        const float InferredZPositionClamp = 0.1f;

        readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        readonly Brush inferredJointBrush = Brushes.Yellow;
        readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        DrawingGroup drawingGroup;
        DrawingImage imageSource;

        int displayWidth;
        int displayHeight;

        IList<Body> _bodies = null;
        List<Tuple<JointType, JointType>> bones;
        List<Pen> bodyColors;

        /// <summary>
        /// Minimum energy of audio to display (a negative number in dB value, where 0 dB is full scale)
        /// </summary>
        private const int MinEnergy = -20;

        /// <summary>
        /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
        /// </summary>
        private readonly byte[] audioBuffer = null;

        /// <summary>
        /// Sum of squares of audio samples being accumulated to compute the next energy value.
        /// </summary>
        private float accumulatedSquareSum;

        /// <summary>
        /// Number of audio samples accumulated so far to compute the next energy value.
        /// </summary>
        private int accumulatedSampleCount;

        /// <summary>
        /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
        /// </summary>
        private const int BytesPerSample = sizeof(float);

        /// <summary>
        /// Number of audio samples represented by each column of pixels in wave bitmap.
        /// </summary>
        private const int SamplesPerColumn = 40;

        /// <summary>
        /// Reader for audio frames
        /// </summary>
        private AudioBeamFrameReader reader = null;

        /// <summary>
        /// Last observed audio beam angle in radians, in the range [-pi/2, +pi/2]
        /// </summary>
        private float beamAngle = 0;

        /// <summary>
        /// Last observed audio beam angle confidence, in the range [0, 1]
        /// </summary>
        private float beamAngleConfidence = 0;

        /// <summary>
        /// Object for locking energy buffer to synchronize threads.
        /// </summary>
        private readonly object energyLock = new object();

        //int counter = 0;

        private string trainExePath = "";
        private string classifyExePath = "";

        List<Definitions> defList;
        private bool trainStuff = false;

        private float prevLeft = 0;
        private float prevRight = 0;
        private int counter = 0;

        CmdExecutor cmd = null;

        ObservableCollection<Definitions> def;
        SpeechSynthesizer synthesizer = null;

        public MainWindow()
        {
            _sensor = KinectSensor.GetDefault();

            /* display color frames */
            _color = _sensor.ColorFrameSource.OpenReader();
            _color.FrameArrived += ColorReader_FrameArrived;
            FrameDescription colorFrameDescription = _sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            coordinateMapper = _sensor.CoordinateMapper;
            FrameDescription frameDescription = _sensor.DepthFrameSource.FrameDescription;
            displayWidth = frameDescription.Width;
            displayHeight = frameDescription.Height;

            bones = new List<Tuple<JointType, JointType>>();

            // Torso
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            DataContext = this;

            drawingGroup = new DrawingGroup();
            imageSource = new DrawingImage(drawingGroup);

            InitializeComponent();

            cmd = new CmdExecutor();
            // set IsAvailableChanged event notifier
            this._sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            if (_sensor != null)
            {
                _sensor.Open();

                // Get its audio source
                AudioSource audioSource = this._sensor.AudioSource;

                // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
                // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
                // With 4 bytes per sample, that gives us 1024 bytes.
                this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

                // Open the reader for the audio frames
                this.reader = audioSource.OpenReader();

                _bodies = new Body[_sensor.BodyFrameSource.BodyCount];

                _reader = _sensor.BodyFrameSource.OpenReader();
                _reader.FrameArrived += BodyReader_FrameArrived;

                _recorder = new KinectCSVManager();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.reader != null)
            {
                // Subscribe to new audio frame arrived events
                this.reader.FrameArrived += this.Reader_FrameArrived;
            }

            //List<string[]> rows = File.ReadAllLines("Dictionary.csv").Select(x => x.Split(',')).ToList();
            Definitions d = new Definitions();
            if (d.ReadCSV("Definitions.csv") != null)
            {
                def = new ObservableCollection<Definitions>(d.ReadCSV("Definitions.csv"));
            }
            else def = new ObservableCollection<Definitions>();

            ListViewDef.ItemsSource = def;

            webBrowser.Navigate("http://alexa.amazon.com");

            defList = new List<Definitions>();
            if (def != null)
            {
                defList = def.ToList();
            }
            //var value = defList.First(item => item.Gesture == "lol").ID;
            //System.Diagnostics.Debug.Write(value);

            synthesizer = new SpeechSynthesizer();
            recordStatusText.Text = "Not recording";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.reader != null)
            {
                // AudioBeamFrameReader is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }

            if (_color != null)
            {
                _color.Dispose();
                _color = null;
            }

            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_sensor != null)
            {
                _sensor.Close();
                _sensor = null;
            }
        }

        public ImageSource ColorStream
        {
            get
            {
                return colorBitmap;
            }
        }

        public ImageSource ImageSource
        {
            get
            {
                return imageSource;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;

            if (_recorder.IsRecording)
            {
                _recorder.Stop();
                _recorder.Reset();

                button.Content = "Start";
                inputText.IsReadOnly = false;

                var confirmResult = System.Windows.MessageBox.Show("Add recorded data to dictionary?", "Confirm add", MessageBoxButton.YesNo);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    using (StreamWriter file = new StreamWriter("Dictionary.csv", true, Encoding.UTF8))
                    {
                        if (!string.IsNullOrEmpty(inputText.Text))
                        {
                            var gest = defList.FirstOrDefault(item => item.Gesture == inputText.Text.ToString());

                            if (gest != null)
                            {
                                string s = string.Format("{0},", gest.ID.ToString());
                                if (_recorder.Result_ != null)
                                {
                                    s += _recorder.Result_;
                                    s = s.Remove(s.LastIndexOf(","));
                                    file.WriteLine(s);
                                }
                            }
                            else
                            {
                                //Random rnd = new Random();
                                //int rand = rnd.Next(0, 500000);

                                string id_ = DateTime.Now.ToString("MMddHHmmss");

                                Int64 uniqueID = Int64.Parse(id_) % 100000;

                                Definitions d = new Definitions(uniqueID, inputText.Text.ToString());
                                defList.Add(d);

                                using (StreamWriter f = new StreamWriter("Definitions.csv", true, Encoding.UTF8))
                                {
                                    string str = string.Format("{0},{1}", d.ID, d.Gesture);
                                    f.WriteLine(str);
                                }

                                def.Add(d);

                                string s = string.Format("{0},", uniqueID.ToString());
                                if (_recorder.Result_ != null)
                                {
                                    s += _recorder.Result_;
                                    s = s.Remove(s.LastIndexOf(","));
                                    file.WriteLine(s);
                                }
                            }

                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Please input a name for the recorded gesture", "Oops");
                        }
                        //file.WriteLine(_recorder.Result_);
                    }
                }
                else if (confirmResult == MessageBoxResult.No)
                {
                    //_recorder.Reset();
                }

                //SaveFileDialog dialog = new SaveFileDialog
                //{
                //    Filter = "Excel files|*.csv"
                //};

                //dialog.ShowDialog();

                //if (!string.IsNullOrWhiteSpace(dialog.FileName))
                //{
                //    System.IO.File.Copy(_recorder.Result, dialog.FileName);
                //}
            }
            else
            {
                //MessageBox.Show(inputText.Text);
                _recorder.Start();
                inputText.IsReadOnly = true;

                button.Content = "Stop";
            }
        }

        void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_bodies);

                    body = _bodies.Where(b => b != null && b.IsTracked).FirstOrDefault();

                    if (body != null)
                    {
                        _recorder.Update(body);
                        //counter++;
                        dataReceived = true;
                    }
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Gray, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    Pen drawPen = this.bodyColors[penIndex++];

                    if (body.IsTracked)
                    {
                        this.DrawClippedEdges(body, dc);

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        float hip = body.Joints[JointType.HipLeft].Position.Y;
                        float leftHand = body.Joints[JointType.HandLeft].Position.Y;
                        float rightHand = body.Joints[JointType.HandRight].Position.Y;

                        if (_recorder.IsRecording && trainStuff)
                        {
                            //System.Diagnostics.Debug.Write(counter + "\n");

                            // check to see if there has been any changes in 1 second
                            if (counter == 150)
                            {
                                counter = 0;
                                float leftDiff = (Math.Abs(prevLeft - leftHand) / ((prevLeft + leftHand) / 2)) * 100; 
                                float rightDiff = (Math.Abs(prevRight - rightHand) / ((prevRight + rightHand) / 2)) * 100;

                                System.Diagnostics.Debug.Write("prevLeft: " + prevLeft + " leftHand: " + leftHand + "\n");
                                System.Diagnostics.Debug.Write("leftDiff: " + leftDiff + " rightDiff: " + rightDiff + "\n\n");

                                // save data to reference 1 second later
                                prevLeft = leftHand;
                                prevRight = rightHand;

                                // left/right hand has 1.2% difference than the position of the hands 60 frames ago
                                //if (leftDiff < 10 && rightDiff < 10)
                                //{
                                //    System.Diagnostics.Debug.Write("Gesture end detected.\n");

                                //    _recorder.Stop();
                                //    using (StreamWriter file = new StreamWriter("Tester.csv", true, Encoding.UTF8))
                                //    {
                                //        Random rnd = new Random();
                                //        int rand = rnd.Next(0, 500);

                                //        string s = string.Format("{0},", rand.ToString());
                                //        s += _recorder.Result_;

                                //        s = s.Remove(s.LastIndexOf(","));

                                //        //System.Diagnostics.Debug.Write(s + "\n\n");
                                //        file.WriteLine(s);
                                //    }

                                //    _recorder.Start();
                                //}
                            }

                            counter++;

                            if (leftHand < hip && rightHand < hip)
                            {
                                System.Diagnostics.Debug.Write("Training has been stopped.\n");
                                counter = 0;

                                _recorder.Stop();
                                _recorder.Reset();

                                using (StreamWriter file = new StreamWriter("Tester.csv", true, Encoding.UTF8))
                                {
                                    Random rnd = new Random();
                                    int rand = rnd.Next(0, 500);

                                    string s = string.Format("{0},", rand.ToString());
                                    s += _recorder.Result_;

                                    s = s.Remove(s.LastIndexOf(","));

                                    //System.Diagnostics.Debug.Write(s + "\n\n");
                                    file.WriteLine(s);
                                }

                                recordStatusText.Foreground = new SolidColorBrush(Colors.Black);
                                recordStatusText.Text = "Not recording";

                                if (classifyExe.IsChecked)
                                {
                                    // recognize the gesture
                                    cmd.Classify(classifyExePath, "Tester.csv");
                                    List<Int64> list = cmd.Result;

                                    string query = "Hey Alexa... ";

                                    foreach (var elem in list)
                                    {
                                        var value = defList.FirstOrDefault(item => item.ID == elem);

                                        if (value != null)
                                        {
                                            query += value.Gesture;
                                            //query += "?";
                                        }
                                    }
                                    inputQuery.Text = query;

                                    System.Diagnostics.Debug.Write(query + "\n");

                                    synthesizer.SpeakAsync(query);
                                }
                                else
                                {
                                    //System.Windows.MessageBox.Show("Please set target executables under Options > Set \"*\" target executable", "Oops");
                                }

                                // delete the file since we are done with it
                                if (File.Exists("Tester.csv"))
                                {
                                    File.Delete("Tester.csv");
                                }
                                trainStuff = false;
                            }
                        }

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        this.DrawBody(joints, jointPoints, dc, drawPen);

                        this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                        this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                    }
                    

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the audio frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;
            AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

            if (frameList != null)
            {
                // AudioBeamFrameList is IDisposable
                using (frameList)
                {
                    // Only one audio beam is supported. Get the sub frame list for this beam
                    IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                    // Loop over all sub frames, extract audio buffer and beam information
                    foreach (AudioBeamSubFrame subFrame in subFrameList)
                    {
                        // Check if beam angle and/or confidence have changed

                        if (subFrame.BeamAngle != this.beamAngle)
                        {
                            this.beamAngle = subFrame.BeamAngle;
                        }

                        if (subFrame.BeamAngleConfidence != this.beamAngleConfidence)
                        {
                            this.beamAngleConfidence = subFrame.BeamAngleConfidence;
                        }

                        // Process audio buffer
                        subFrame.CopyFrameDataToArray(this.audioBuffer);

                        for (int i = 0; i < this.audioBuffer.Length; i += BytesPerSample)
                        {
                            // Extract the 32-bit IEEE float sample from the byte array
                            float audioSample = BitConverter.ToSingle(this.audioBuffer, i);

                            this.accumulatedSquareSum += audioSample * audioSample;
                            ++this.accumulatedSampleCount;

                            if (this.accumulatedSampleCount < SamplesPerColumn)
                            {
                                continue;
                            }

                            float meanSquare = this.accumulatedSquareSum / SamplesPerColumn;

                            if (meanSquare > 1.0f)
                            {
                                // A loud audio source right next to the sensor may result in mean square values
                                // greater than 1.0. Cap it at 1.0f for display purposes.
                                meanSquare = 1.0f;
                            }

                            // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                            float energy = MinEnergy;

                            // Convert the beam angle from radians to degrees (rad * 180) / pi
                            float beamAngleInDeg = this.beamAngle * 180.0f / (float)Math.PI;
                            if (meanSquare > 0)
                            {
                                energy = (float)(10.0 * Math.Log10(meanSquare));
                                if (energy > -20)
                                {
                                    if (beamAngleInDeg < 10 && beamAngleInDeg > -10)
                                    {
                                        lock (this.energyLock)
                                        {
                                            if (!(trainStuff || _recorder.IsRecording))
                                            {
                                                System.Diagnostics.Debug.Write("Training has been started.\n");

                                                trainStuff = true;
                                                Thread.Sleep(700);

                                                _recorder.Start();

                                                recordStatusText.Foreground = new SolidColorBrush(Colors.Red);
                                                recordStatusText.Text = "Recording...";
                                            }
                                            //if (!theChoosenOne)
                                            //{
                                            //    shittyFunction();
                                            //    theChoosenOne = true;
                                            //}
                                            //System.Diagnostics.Debug.Write("hi");
                                            //if (!_recorder.IsRecording)
                                            //{
                                            //    //_recorder.Start();

                                            //}
                                        }
                                    }
                                }
                            }

                            this.accumulatedSquareSum = 0;
                            this.accumulatedSampleCount = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);

                // Draw the joints
                DrawJoint(joints, jointPoints, drawingContext, bone.Item1);
                DrawJoint(joints, jointPoints, drawingContext, bone.Item2);
            }
        }

        private void DrawJoint(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, JointType jointType)
        {
            Brush drawBrush = null;

            TrackingState trackingState = joints[jointType].TrackingState;

            if (trackingState == TrackingState.Tracked)
            {
                drawBrush = this.trackedJointBrush;
            }
            else if (trackingState == TrackingState.Inferred)
            {
                drawBrush = this.inferredJointBrush;
            }

            if (drawBrush != null)
            {
                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            }
        }

        private void MenuItem_classifyClick(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FileDialog filedialog = new System.Windows.Forms.OpenFileDialog())
            {
                filedialog.Filter = "EXE files (*.exe)|*.exe";
                if (System.Windows.Forms.DialogResult.OK == filedialog.ShowDialog())
                {
                    //string filename = filedialog.FileName;
                    //System.Diagnostics.Debug.Write(filename);
                    classifyExePath = filedialog.FileName;
                    classifyExe.IsChecked = true;
                }
            }
        }

        private void MenuItem_trainClick(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FileDialog filedialog = new System.Windows.Forms.OpenFileDialog())
            {
                filedialog.Filter = "EXE files (*.exe)|*.exe";
                if (System.Windows.Forms.DialogResult.OK == filedialog.ShowDialog())
                {
                    //string filename = filedialog.FileName;
                    //System.Diagnostics.Debug.Write(filename);
                    trainExePath = filedialog.FileName;
                    trainExe.IsChecked = true;
                }
            }
        }

        private void Button_trainClick(object sender, RoutedEventArgs e)
        {
            if (trainExe.IsChecked)
            {
                cmd.Train(trainExePath);
            }
            else
            {
                System.Windows.MessageBox.Show("Please set target executables under Options > Set \"*\" target executable", "Oops");
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.statusBarText.Text = this._sensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }


        private void Button_classifyClick(object sender, RoutedEventArgs e)
        {
            if (!(trainStuff || _recorder.IsRecording))
            {
                //System.Diagnostics.Debug.Write("Training has been started.\n");

                trainStuff = true;
                _recorder.Start();

                recordStatusText.Foreground = new SolidColorBrush(Colors.Red);
                recordStatusText.Text = "Recording...";
            }
        }
    }
}
