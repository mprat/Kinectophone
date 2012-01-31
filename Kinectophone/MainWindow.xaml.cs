using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
//using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
//using System.Runtime.InteropServices;
using Midi;
//using System.Media;
//using NAudio.CoreAudioApi;
//using NAudio.Dsp;
//using NAudio.Midi;
//using NAudio.Mixer;
//using NAudio.Sfz;
//using NAudio.Utils;
//using NAudio.Wave;


//TODO: make sure canvas1 and kinectColorOut have the same dimensions
//TODO: set ellipses invisible when they are out of bounds


namespace Kinectophone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Runtime nui = Runtime.Kinects[0];
        private OutputDevice soundOut = OutputDevice.InstalledDevices[0];
        private int pitchRegionsX = 6;
        private int pitchRegionsY = 6;
        private Random random = new Random();
        private Dictionary<Tuple<int, int>, Pitch> regionToPitch = new Dictionary<Tuple<int, int>, Pitch>();
        private HashSet<Joint> jointsOnList = new HashSet<Joint>();

        private Joint head = new Joint();
        private Joint shoulderCenter = new Joint();
        private Joint shoulderLeft = new Joint();
        private Joint shoulderRight = new Joint();
        private Joint elbowLeft = new Joint();
        private Joint elbowRight = new Joint();
        private Joint wristLeft = new Joint();
        private Joint wristRight = new Joint();
        private Joint handLeft = new Joint();
        private Joint handRight = new Joint();
        private Joint spine = new Joint();

        enum RegionToPitchDictType { Random, Piano, ModBeats };

        //music setting booleans (defaults)
        private RegionToPitchDictType dictType;

        //private IWavePlayer waveOut;
        //private SoundPlayer seedSoundPlayer = new SoundPlayer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            this.nui.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

            this.nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            this.nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);

            this.nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

            this.regionToPitch = regionToPitchDict();
            this.dictType = RegionToPitchDictType.Piano;

            //foreach (Pitch p in this.regionToPitch.Values)
            //{
            //    Console.Out.WriteLine(p.ToString());
            //}
            //soundOut.Open(); //you don't want to turn the sound on right away - it's now in a toggle button
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.nui.Uninitialize();
            this.soundOut.SilenceAllNotes();
            this.soundOut.Close();
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            int soundVelocity = 120;

            SkeletonData skeleton = (from s in e.SkeletonFrame.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();

            if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                this.head = getAndDrawJoint(skeleton, JointID.Head, headEllipse);
                this.shoulderCenter = getAndDrawJoint(skeleton, JointID.ShoulderCenter, shoulderCenterEllipse);
                this.shoulderLeft = getAndDrawJoint(skeleton, JointID.ShoulderLeft, shoulderLeftEllipse);
                this.shoulderRight = getAndDrawJoint(skeleton, JointID.ShoulderRight, shoulderRightEllipse);
                this.elbowLeft = getAndDrawJoint(skeleton, JointID.ElbowLeft, elbowLeftEllipse);
                this.elbowRight = getAndDrawJoint(skeleton, JointID.ElbowRight, elbowRightEllipse);
                this.wristLeft = getAndDrawJoint(skeleton, JointID.WristLeft, wristLeftEllipse);
                this.wristRight = getAndDrawJoint(skeleton, JointID.WristRight, wristRightEllipse);
                this.handLeft = getAndDrawJoint(skeleton, JointID.HandLeft, handLeftEllipse);
                this.handRight = getAndDrawJoint(skeleton, JointID.HandRight, handRightEllipse);
                this.spine = getAndDrawJoint(skeleton, JointID.Spine, spineEllipse);

                switch (dictType)
                {
                    case RegionToPitchDictType.Random:
                        
                        if (!(this.jointsOnList.Count == 11))
                        {
                            this.jointsOnList.Clear();
                        }
                        this.jointsOnList.Add(this.head);
                        this.jointsOnList.Add(this.shoulderCenter);
                        this.jointsOnList.Add(this.shoulderLeft);
                        this.jointsOnList.Add(this.shoulderRight);
                        this.jointsOnList.Add(this.elbowLeft);
                        this.jointsOnList.Add(this.elbowRight);
                        this.jointsOnList.Add(this.wristLeft);
                        this.jointsOnList.Add(this.wristRight);
                        this.jointsOnList.Add(this.handLeft);
                        this.jointsOnList.Add(this.handRight);
                        this.jointsOnList.Add(this.spine);
                        break;
                    case RegionToPitchDictType.Piano:
                        if (!(this.jointsOnList.Count == 2))
                        {
                            this.jointsOnList.Clear();
                        }
                        //this.jointsOnList.Add(this.handLeft);
                        this.jointsOnList.Add(this.handRight);
                        break;
                    case RegionToPitchDictType.ModBeats:
                        break;
                }

                if (this.soundOut.IsOpen)
                {
                    foreach (Joint noteJoint in this.jointsOnList)
                    {
                        //Console.Out.WriteLine(noteJoint.ID.ToString());
                        
                        if (!(bool)multNoteCheck.IsChecked)
                        {
                            this.soundOut.SilenceAllNotes();
                        }

                        double xreg = (double)noteJoint.Position.X;
                        double yreg = (double)noteJoint.Position.Y;
                        Tuple<int, int> pitchreg = coordToRegion(xreg, yreg);

                        //Console.Out.WriteLine(pitchreg.ToString());

                        //only play the sound if the key is contained in the dictionary
                        if (regionToPitch.ContainsKey(pitchreg))
                        {
                            this.soundOut.SendNoteOn(Channel.Channel1, regionToPitch[pitchreg], soundVelocity);

                            //Console.Out.WriteLine(regionToPitch[pitchreg].ToString());
                        }
                    }
                }
            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            this.kinectColorOut.Source = e.ImageFrame.ToBitmapSource();
        }

        private Joint getAndDrawJoint(SkeletonData skel, JointID jointID, UIElement ellipse)
        {
            Joint jt = skel.Joints[jointID].ScaleTo((int)kinectColorOut.Height, (int)kinectColorOut.Width, .5f, .5f);

            Canvas.SetLeft(ellipse, jt.Position.X);
            Canvas.SetTop(ellipse, jt.Position.Y);

            ellipse.Visibility = System.Windows.Visibility.Visible;
            return jt;
        }

        //this method is called once at the beginning of execution of this program
        //it sets the int, int region mapping to the pitch, so it already knows how many regions there are
        private Dictionary<Tuple<int, int>, Pitch> regionToPitchDict()
        {
            Pitch[] pitches = new Pitch[]{Pitch.A0, Pitch.A1, Pitch.A2, Pitch.A3, Pitch.A4, Pitch.A5, Pitch.A6, Pitch.A7, Pitch.A8,
                                            Pitch.ANeg1, Pitch.ASharp0, Pitch.ASharp1, Pitch.ASharp2, Pitch.ASharp3, Pitch.ASharp4, Pitch.ASharp5,
                                            Pitch.ASharp6, Pitch.ASharp7, Pitch.ASharp8, Pitch.ASharpNeg1, Pitch.B0, Pitch.B1, Pitch.B2, Pitch.B3,
                                            Pitch.B4, Pitch.B5, Pitch.B6, Pitch.B7, Pitch.B8, Pitch.BNeg1, Pitch.C0, Pitch.C1, Pitch.C2, Pitch.C3,
                                            Pitch.C4, Pitch.C5, Pitch.C6, Pitch.C7, Pitch.C8, Pitch.C9, Pitch.CNeg1, Pitch.CSharp0, Pitch.CSharp1,
                                            Pitch.CSharp2, Pitch.CSharp3, Pitch.CSharp4, Pitch.CSharp5, Pitch.CSharp6, Pitch.CSharp7, Pitch.CSharp8,
                                            Pitch.CSharp9, Pitch.CSharpNeg1, Pitch.D0, Pitch.D1, Pitch.D2, Pitch.D3, Pitch.D4, Pitch.D5, Pitch.D6,
                                            Pitch.D7, Pitch.D8, Pitch.D9, Pitch.DNeg1, Pitch.DSharp0, Pitch.DSharp1, Pitch.DSharp2, Pitch.DSharp3,
                                            Pitch.DSharp4, Pitch.DSharp5, Pitch.DSharp6, Pitch.DSharp7, Pitch.DSharp8, Pitch.DSharp9, Pitch.DSharpNeg1,
                                            Pitch.E0, Pitch.E1, Pitch.E2, Pitch.E3, Pitch.E4, Pitch.E5, Pitch.E6, Pitch.E7, Pitch.E8, Pitch.E9, Pitch.ENeg1,
                                            Pitch.F0, Pitch.F1, Pitch.F2, Pitch.F3, Pitch.F4, Pitch.F5, Pitch.F6, Pitch.F7, Pitch.F8, Pitch.F9, Pitch.FNeg1,
                                            Pitch.FSharp0, Pitch.FSharp1, Pitch.FSharp2, Pitch.FSharp3, Pitch.FSharp4, Pitch.FSharp5, Pitch.FSharp6,
                                            Pitch.FSharp7, Pitch.FSharp8, Pitch.FSharp9, Pitch.FSharpNeg1, Pitch.G0, Pitch.G1, Pitch.G2, Pitch.G3, Pitch.G4,
                                            Pitch.G5, Pitch.G6, Pitch.G7, Pitch.G8, Pitch.G9, Pitch.GNeg1, Pitch.GSharp0, Pitch.GSharp1, Pitch.GSharp2,
                                            Pitch.GSharp3, Pitch.GSharp4, Pitch.GSharp5, Pitch.GSharp6, Pitch.GSharp7, Pitch.GSharp8, Pitch.GSharpNeg1};

            Dictionary<Tuple<int, int>, Pitch> intToPitch;

            if (this.dictType == RegionToPitchDictType.Random)
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();

                for (int i = 0; i < this.pitchRegionsX; i++)
                {
                    for (int j = 0; j < this.pitchRegionsY; j++)
                    {
                        intToPitch.Add(Tuple.Create(i, j), pitches[random.Next(0, pitches.Length)]);
                    }
                }
            }
            else if (this.dictType == RegionToPitchDictType.Piano)
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();
                
                // TODO: eventually be able to choose what chord/scale you want
                setPianoParams();

                //for now, manually set pitches for the piano until can do something better
                intToPitch.Add(Tuple.Create(2, 2), Pitch.C4);
                intToPitch.Add(Tuple.Create(2, 3), Pitch.C4);
                intToPitch.Add(Tuple.Create(2, 4), Pitch.D4);
                intToPitch.Add(Tuple.Create(2, 5), Pitch.D4);
                intToPitch.Add(Tuple.Create(2, 6), Pitch.E4);
                intToPitch.Add(Tuple.Create(2, 7), Pitch.E4);
                intToPitch.Add(Tuple.Create(2, 8), Pitch.F4);
                intToPitch.Add(Tuple.Create(2, 9), Pitch.F4);
                intToPitch.Add(Tuple.Create(2, 10), Pitch.G4);
                intToPitch.Add(Tuple.Create(2, 11), Pitch.G4);
                intToPitch.Add(Tuple.Create(2, 12), Pitch.A4);
                intToPitch.Add(Tuple.Create(2, 13), Pitch.A4);
                intToPitch.Add(Tuple.Create(2, 14), Pitch.B4);
                intToPitch.Add(Tuple.Create(2, 15), Pitch.B4);
                intToPitch.Add(Tuple.Create(1, 3), Pitch.CSharp4);
                intToPitch.Add(Tuple.Create(1, 4), Pitch.CSharp4);
                intToPitch.Add(Tuple.Create(1, 5), Pitch.DSharp4);
                intToPitch.Add(Tuple.Create(1, 6), Pitch.DSharp4);
                intToPitch.Add(Tuple.Create(1, 9), Pitch.FSharp4);
                intToPitch.Add(Tuple.Create(1, 10), Pitch.FSharp4);
                intToPitch.Add(Tuple.Create(1, 11), Pitch.GSharp4);
                intToPitch.Add(Tuple.Create(1, 12), Pitch.GSharp4);
                intToPitch.Add(Tuple.Create(1, 13), Pitch.ASharp4);
                intToPitch.Add(Tuple.Create(1, 14), Pitch.ASharp4);
            }
            else if (this.dictType == RegionToPitchDictType.ModBeats)
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();
            }
            else
                //if nothing else is here. it should never go here.
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();

                //TODO: should throw an exception, since it should never go here
            }

            return intToPitch;
        }

        //only draw the squares for the piano so you know where the notes are
        private void drawPianoSquares()
        {
            Rectangle c = new Rectangle();

            canvas1.Children.Add(c);
        }

        private void setPianoParams()
        {
            //the notes are going to look like a piano, with the sharps/flats above the regular notes
            //only one octave (the one with the middle C)
            //TODO: options for shifting the octave one higher and one lower
            //you want the two rows to be about the height of your shoulders, which are about rows 2 and 3 out of 5
            this.pitchRegionsX = 18; 
            this.pitchRegionsY = 5;

            //for now, manually set the regions to notes unless there is a better way of doing it 
            //(notes are manually set in regionToPitchDict)

        }

        private Tuple<int, int> coordToRegion(double x, double y)
        {
            double workingCoord = 0;

            //break x
            double partWidth = kinectColorOut.Width / this.pitchRegionsX;
            while (Math.Abs(x - workingCoord) > partWidth)
            {
                workingCoord += partWidth;
            }
            int xTuple = (int)(workingCoord / partWidth);
            workingCoord = 0;
            //break y
            double partHeight = kinectColorOut.Height / this.pitchRegionsY;
            while (Math.Abs(y - workingCoord) > partHeight)
            {
                workingCoord += partHeight;
            }
            int yTuple = (int)(workingCoord / partHeight);
            
            Tuple<int, int> region = Tuple.Create(xTuple, yTuple);
            return region;
        }

        private void toggleSound_Click(object sender, RoutedEventArgs e)
        {
            if (this.soundOut.IsOpen)
            {
                this.soundOut.Close();
                Console.Out.Write("sound closed\n");
            }
            else
            {
                this.soundOut.Open();
                Console.Out.Write("sound open\n");
            }
        }

        private void modeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string comboString = modeComboBox.SelectedItem.ToString();
            if (comboString == "Random")
            {
                dictType = RegionToPitchDictType.Random;
            }
            else if (comboString == "Piano")
            {
                dictType = RegionToPitchDictType.Piano;
            }
            else if (comboString == "Mod")
            {

            }
        }
    }
}
