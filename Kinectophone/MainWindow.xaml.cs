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
//TODO: fix checkbox for piano mode
//TODO: text labeling the notes


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
        private int soundVelocity = 120;

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
        private Joint hipLeft = new Joint();
        private Joint hipRight = new Joint();

        private Pitch pitchToPlay = Pitch.GSharpNeg1; //this is the "zero" of our pitches

        private Clock pianoUp = new Clock(120); //TODO: figure out a better way to set BPM;
        private Clock pianoDown = new Clock(120); //TODO: figure out a better way to set BPM

        enum RegionToPitchDictType { Random, Piano, ModBeats, GestureMusic };

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

            this.dictType = RegionToPitchDictType.GestureMusic;
            this.regionToPitch = regionToPitchDict();
                        
            switch (this.dictType)
            {
                case RegionToPitchDictType.Piano:
                    setPianoParams();
                    drawPianoSquares();
                    break;
            }

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
            SkeletonData skeleton = (from s in e.SkeletonFrame.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();

            if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                this.handRight = getAndDrawJoint(skeleton, JointID.HandRight, handRightEllipse);
                this.handLeft = getAndDrawJoint(skeleton, JointID.HandLeft, handLeftEllipse);
                this.spine = getAndDrawJoint(skeleton, JointID.Spine, spineEllipse);
                this.head = getAndDrawJoint(skeleton, JointID.Head, headEllipse);
                this.hipLeft = getAndDrawJoint(skeleton, JointID.HipLeft, hipLeftEllipse);
                this.hipRight = getAndDrawJoint(skeleton, JointID.HipRight, hipRightEllipse);

                if (dictType == RegionToPitchDictType.Random)
                {
                    this.shoulderCenter = getAndDrawJoint(skeleton, JointID.ShoulderCenter, shoulderCenterEllipse);
                    this.shoulderLeft = getAndDrawJoint(skeleton, JointID.ShoulderLeft, shoulderLeftEllipse);
                    this.shoulderRight = getAndDrawJoint(skeleton, JointID.ShoulderRight, shoulderRightEllipse);
                    this.elbowLeft = getAndDrawJoint(skeleton, JointID.ElbowLeft, elbowLeftEllipse);
                    this.elbowRight = getAndDrawJoint(skeleton, JointID.ElbowRight, elbowRightEllipse);
                    this.wristLeft = getAndDrawJoint(skeleton, JointID.WristLeft, wristLeftEllipse);
                    this.wristRight = getAndDrawJoint(skeleton, JointID.WristRight, wristRightEllipse);
                }

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
                        this.jointsOnList.Add(this.handLeft);
                        this.jointsOnList.Add(this.handRight);
                        break;
                    case RegionToPitchDictType.ModBeats:
                        break;
                    case RegionToPitchDictType.GestureMusic:
                        if (!(this.jointsOnList.Count == 6))
                        {
                            this.jointsOnList.Clear();
                        }
                        this.jointsOnList.Add(this.handRight);
                        this.jointsOnList.Add(this.handLeft);
                        this.jointsOnList.Add(this.spine);
                        this.jointsOnList.Add(this.head);
                        this.jointsOnList.Add(this.hipRight);
                        this.jointsOnList.Add(this.hipLeft);
                        break;
                }

                if (this.soundOut.IsOpen)
                {
                    //only for GestureMusic
                    if (dictType == RegionToPitchDictType.GestureMusic)
                    {
                        //TODO: do real gestures. for now do a hacky version of gestures

                        Microsoft.Research.Kinect.Nui.Vector rightHandPos = this.handRight.Position;
                        Microsoft.Research.Kinect.Nui.Vector leftHandPos = this.handLeft.Position;
                        Microsoft.Research.Kinect.Nui.Vector spinePos = this.spine.Position;
                        Microsoft.Research.Kinect.Nui.Vector headPos = this.head.Position;
                        Microsoft.Research.Kinect.Nui.Vector hipLeftPos = this.hipLeft.Position;
                        Microsoft.Research.Kinect.Nui.Vector hipRightPos = this.hipRight.Position;

                        
                        //if the hands are close together above the spine
                        if ((distance(rightHandPos, leftHandPos) < 50.0) && (rightHandPos.Y < spinePos.Y))
                        {
                            //to the right of the spine
                            if (rightHandPos.X > kinectColorOut.Width / 2)
                            {
                                if (!(this.pianoUp.IsRunning))
                                {
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.C4, this.soundVelocity, 0, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.D4, this.soundVelocity, .5F, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.E4, this.soundVelocity, 1, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.F4, this.soundVelocity, 1.5F, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.G4, this.soundVelocity, 2, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.A4, this.soundVelocity, 2.5F, pianoUp, .5F));
                                    this.pianoUp.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.B4, this.soundVelocity, 3, pianoUp, .5F));
                                    this.pianoUp.Start();
                                }
                            }
                            //to the left of the spine
                            else
                            {
                                if (!(this.pianoDown.IsRunning))
                                {
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.B4, this.soundVelocity, 0, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.A4, this.soundVelocity, .25F, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.G4, this.soundVelocity, .5F, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.F4, this.soundVelocity, .75F, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.E4, this.soundVelocity, 1, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.D4, this.soundVelocity, 1.25F, pianoDown, .25F));
                                    this.pianoDown.Schedule(new NoteOnOffMessage(this.soundOut, Channel.Channel1, Pitch.C4, this.soundVelocity, 1.5F, pianoDown, .25F));
                                    this.pianoDown.Start();
                                }
                            }
                        }
                        //raise both hands above the head when one is on one side and one is on the other
                        else if ((rightHandPos.X > kinectColorOut.Width / 2) && (leftHandPos.X < kinectColorOut.Width / 2)
                            && (rightHandPos.Y < headPos.Y) && (leftHandPos.Y < headPos.Y) && (distance(rightHandPos, leftHandPos) > 50.0))
                        {
                            this.soundOut.SendPercussion(Percussion.CrashCymbal1, this.soundVelocity);
                            this.soundOut.SendPercussion(Percussion.CrashCymbal2, this.soundVelocity);
                        }
                            //raise right hand and play a really high note
                        else if ((distance(headPos, rightHandPos) > 200.0) && (rightHandPos.X > kinectColorOut.Width / 2)
                            && (rightHandPos.Y < headPos.Y))
                        {
                            this.soundOut.SendNoteOn(Channel.Channel1, Pitch.A6, this.soundVelocity);
                        }
                        //both hands on hips is a cowbell
                        else if ((distance(hipLeftPos, leftHandPos) < 150.0) && (distance(hipRightPos, rightHandPos) < 150.0))
                        {
                            this.soundOut.SendPercussion(Percussion.Cowbell, this.soundVelocity);
                        }
                        //right hand on hip is a bass drum
                        else if (distance(hipRightPos, rightHandPos) < 50.0)
                        {
                            this.soundOut.SendPercussion(Percussion.BassDrum1, this.soundVelocity);
                            this.soundOut.SendPercussion(Percussion.BassDrum2, this.soundVelocity);
                        }
                        else
                        {
                            if (this.pianoUp.IsRunning)
                            {
                                this.pianoUp.Stop();
                            }
                            this.pianoUp.Reset();

                            if (this.pianoDown.IsRunning)
                            {
                                this.pianoDown.Stop();
                            }
                            this.pianoDown.Reset();

                        }
                    }

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

                        if (dictType == RegionToPitchDictType.Piano)
                        {
                            this.soundOut.SendNoteOff(Channel.Channel1, pitchToPlay, this.soundVelocity);
                        }

                        //only play the sound if the key is contained in the dictionary
                        if (regionToPitch.ContainsKey(pitchreg))
                        {
                            if (!(regionToPitch[pitchreg].Equals(pitchToPlay)))
                            {
                                pitchToPlay = regionToPitch[pitchreg];

                                //make sure the pitch is not "zero"
                                if (!(pitchToPlay.Equals(Pitch.GSharpNeg1)))
                                {
                                    this.soundOut.SendNoteOn(Channel.Channel1, pitchToPlay, this.soundVelocity);

                                    //Console.Out.WriteLine(pitchToPlay.ToString());
                                }
                            }
                            else
                            {
                                //make sure the pitch is not "zero"
                                if (!(pitchToPlay.Equals(Pitch.GSharpNeg1)))
                                {
                                    this.soundOut.SendControlChange(Channel.Channel1, Midi.Control.SustainPedal, this.soundVelocity);
                                }
                            }

                        }
                    }
                }
            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            this.kinectColorOut.Source = e.ImageFrame.ToBitmapSource();
        }

        private Joint getAndDrawJoint(SkeletonData skel, JointID jointID, Ellipse ellipse)
        {
            Joint jt = skel.Joints[jointID].ScaleTo((int)canvas1.Height, (int)(canvas1.Width), .8f, .8f);

            Canvas.SetLeft(ellipse, jt.Position.X + ellipse.Width / 2);
            Canvas.SetTop(ellipse, jt.Position.Y + ellipse.Height / 2);

            ellipse.Visibility = System.Windows.Visibility.Visible;
            return jt;
        }
        
        //distance between two skeletal points
        private double distance(Microsoft.Research.Kinect.Nui.Vector v1, Microsoft.Research.Kinect.Nui.Vector v2)
        {
            return (Math.Sqrt(Math.Pow(v1.W - v2.W, 2) + Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Y - v2.Y, 2)));
        }

        //this method is called once at the beginning of execution of this program
        //it sets the int, int region mapping to the pitch, so it already knows how many regions there are
        private Dictionary<Tuple<int, int>, Pitch> regionToPitchDict()
        {
            //Pitch.GSharpNeg1 is not included because it is used as "zero"
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
                                            Pitch.GSharp3, Pitch.GSharp4, Pitch.GSharp5, Pitch.GSharp6, Pitch.GSharp7, Pitch.GSharp8};

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
                //setPianoParams();

                //for now, manually set pitches for the piano until can do something better
                intToPitch.Add(Tuple.Create(1, 2), Pitch.C4);
                intToPitch.Add(Tuple.Create(2, 2), Pitch.C4);
                intToPitch.Add(Tuple.Create(3, 2), Pitch.D4);
                intToPitch.Add(Tuple.Create(4, 2), Pitch.D4);
                intToPitch.Add(Tuple.Create(5, 2), Pitch.E4);
                intToPitch.Add(Tuple.Create(6, 2), Pitch.E4);
                intToPitch.Add(Tuple.Create(7, 2), Pitch.F4);
                intToPitch.Add(Tuple.Create(8, 2), Pitch.F4);
                intToPitch.Add(Tuple.Create(9, 2), Pitch.G4);
                intToPitch.Add(Tuple.Create(10, 2), Pitch.G4);
                intToPitch.Add(Tuple.Create(11, 2), Pitch.A4);
                intToPitch.Add(Tuple.Create(12, 2), Pitch.A4);
                intToPitch.Add(Tuple.Create(13, 2), Pitch.B4);
                intToPitch.Add(Tuple.Create(14, 2), Pitch.B4);
                intToPitch.Add(Tuple.Create(2, 1), Pitch.CSharp4);
                intToPitch.Add(Tuple.Create(3, 1), Pitch.CSharp4);
                intToPitch.Add(Tuple.Create(4, 1), Pitch.DSharp4);
                intToPitch.Add(Tuple.Create(5, 1), Pitch.DSharp4);
                intToPitch.Add(Tuple.Create(8, 1), Pitch.FSharp4);
                intToPitch.Add(Tuple.Create(9, 1), Pitch.FSharp4);
                intToPitch.Add(Tuple.Create(10, 1), Pitch.GSharp4);
                intToPitch.Add(Tuple.Create(11, 1), Pitch.GSharp4);
                intToPitch.Add(Tuple.Create(12, 1), Pitch.ASharp4);
                intToPitch.Add(Tuple.Create(13, 1), Pitch.ASharp4);
            }
            else if (this.dictType == RegionToPitchDictType.ModBeats)
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();
            }
            else if (this.dictType == RegionToPitchDictType.GestureMusic)
            {
                intToPitch = new Dictionary<Tuple<int, int>, Pitch>();
                
                //TODO: shouldn't need an intToPitch dictionary here
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
            for (int i = 1; i <= 7; i++)
            {
                setRectangleParams(new Rectangle(), 2, i);

                if (i != 3 && i != 7)
                {
                    setRectangleParams(new Rectangle(), 1, i + .5);
                }
            }
        }

        private void setRectangleParams(Rectangle rect, double xmult, double ymult)
        {
            rect.Stroke = System.Windows.Media.Brushes.Cyan;
            rect.StrokeThickness = 2;
            rect.Width = 2 * kinectColorOut.Width / this.pitchRegionsX;
            rect.Height = kinectColorOut.Height / this.pitchRegionsY;
            canvas1.Children.Add(rect);

            Canvas.SetTop(rect, rect.Height * xmult);
            Canvas.SetLeft(rect, rect.Width * ymult);
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
    }
}
