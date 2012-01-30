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

        enum RegionToPitchDictType { Random, Piano, ModBeats };

        //music setting booleans (defaults)
        private bool multiple = true;
        private RegionToPitchDictType dictType = RegionToPitchDictType.Random;

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
            //soundOut.Open();
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
                Joint head = getAndDrawJoint(skeleton, JointID.Head, headEllipse);
                Joint shoulderCenter = getAndDrawJoint(skeleton, JointID.ShoulderCenter, shoulderCenterEllipse);
                Joint shoulderLeft = getAndDrawJoint(skeleton, JointID.ShoulderLeft, shoulderLeftEllipse);
                Joint shoulderRight = getAndDrawJoint(skeleton, JointID.ShoulderRight, shoulderRightEllipse);
                Joint elbowLeft = getAndDrawJoint(skeleton, JointID.ElbowLeft, elbowLeftEllipse);
                Joint elbowRight = getAndDrawJoint(skeleton, JointID.ElbowRight, elbowRightEllipse);
                Joint wristLeft = getAndDrawJoint(skeleton, JointID.WristLeft, wristLeftEllipse);
                Joint wristRight = getAndDrawJoint(skeleton, JointID.WristRight, wristRightEllipse);
                Joint handLeft = getAndDrawJoint(skeleton, JointID.HandLeft, handLeftEllipse);
                Joint handRight = getAndDrawJoint(skeleton, JointID.HandRight, handRightEllipse);
                Joint spine = getAndDrawJoint(skeleton, JointID.Spine, spineEllipse);

                Joint[] noteJoints = new Joint[11]{head, shoulderCenter, shoulderLeft, shoulderRight, elbowLeft, elbowRight, wristLeft, wristRight, handLeft, handRight, spine};

                if (this.soundOut.IsOpen)
                {
                    foreach (Joint noteJoint in noteJoints)
                    {
                        if (!multiple)
                        {
                            this.soundOut.SilenceAllNotes();
                        }
                        this.soundOut.SendNoteOn(Channel.Channel1, regionToPitch[coordToRegion((double)noteJoint.Position.X, (double)noteJoint.Position.Y)], soundVelocity);
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
            Joint jt = skel.Joints[jointID].ScaleTo((int)canvas1.Height, (int)canvas1.Width, .5f, .5f);

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

            Dictionary<Tuple<int, int>, Pitch> intToPitch = new Dictionary<Tuple<int, int>, Pitch>();

            if (this.dictType == RegionToPitchDictType.Random)
            {
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
                // TODO: eventually be able to choose what chord/scale you want
                for (int j = 0; j < this.pitchRegionsY; j++)
                {

                }
            }
            else if (this.dictType == RegionToPitchDictType.ModBeats)
            {

            }

            return intToPitch;
        }

        private void drawGridBoundaries()
        {
            double partWidth = canvas1.Width / this.pitchRegionsX;
            double partHeight = canvas1.Height / this.pitchRegionsY;

            double workingCoord = 0;

            for (int i = 0; i < this.pitchRegionsX; i++)
            {
                Line templine = new Line();

                canvas1.Children.Add(templine);
            }
        }

        private Tuple<int, int> coordToRegion(double x, double y)
        {
            double workingCoord = 0;

            //break x
            double partWidth = canvas1.Width / this.pitchRegionsX;
            while (Math.Abs(x - workingCoord) > partWidth)
            {
                workingCoord += partWidth;
            }
            int xTuple = (int)(workingCoord / partWidth);
            workingCoord = 0;
            //break y
            double partHeight = canvas1.Height / this.pitchRegionsY;
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

        private void multNoteCheck_Checked(object sender, RoutedEventArgs e)
        {
            this.multiple = !this.multiple;
            Console.Out.Write("Multiple notes are " + this.multiple);
        }
    }
}
