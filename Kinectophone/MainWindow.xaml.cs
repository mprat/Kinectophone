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
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using Midi;

namespace Kinectophone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime nui = Runtime.Kinects[0];
        OutputDevice soundOut = OutputDevice.InstalledDevices[0];
        int pitchRegionsX = 4;
        int pitchRegionsY = 4;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            nui.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);

            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

            //soundOut.Open();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
            soundOut.SilenceAllNotes();
            soundOut.Close();
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            int soundVelocity = 120;

            SkeletonData skeleton = (from s in e.SkeletonFrame.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();

            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
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

                if (soundOut.IsOpen)
                {
                    if (canvas1.Height / 3 >= (double)head.Position.Y)
                    {
                        soundOut.SendNoteOn(Channel.Channel1, Pitch.C4, soundVelocity);
                    }
                    if ((double)handRight.Position.Y >= canvas1.Height / 2)
                    {
                        soundOut.SendNoteOn(Channel.Channel1, Pitch.D7, soundVelocity);
                    }
                    if ((double)handLeft.Position.Y >= canvas1.Height / 2)
                    {
                        soundOut.SendNoteOn(Channel.Channel1, Pitch.B3, soundVelocity);
                    }
                }
            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            kinectColorOut.Source = e.ImageFrame.ToBitmapSource();
        }

        private Joint getAndDrawJoint(SkeletonData skel, JointID jointID, UIElement ellipse)
        {
            Joint jt = skel.Joints[jointID].ScaleTo((int)canvas1.Height, (int)canvas1.Width, .5f, .5f);

            Canvas.SetLeft(ellipse, jt.Position.X);
            Canvas.SetTop(ellipse, jt.Position.Y);
            return jt;
        }

        //this method is called once at the beginning of execution of this program
        //it sets the int, int region mapping to the pitch, so it already knows how many regions there are
        private Dictionary<Tuple<int, int>, Pitch> regionToPitchDict()
        {
            Dictionary<Tuple<int, int>, Pitch> intToPitch = new Dictionary<Tuple<int, int>, Pitch>();


            return intToPitch;
        }

        private Tuple<int, int> coordToRegion(double x, double y)
        {
            double workingCoord = 0;

            //break x
            double partWidth = canvas1.Width / pitchRegionsX;
            while (Math.Abs(x - workingCoord) > partWidth)
            {
                workingCoord += partWidth;
            }
            int xTuple = (int)(workingCoord / partWidth);
            workingCoord = 0;
            //break y
            double partHeight = canvas1.Height / pitchRegionsY;
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
            if (soundOut.IsOpen)
            {
                soundOut.Close();
            }
            else
            {
                soundOut.Open();
            }
        }
    }
}
