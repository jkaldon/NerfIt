using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;using Pololu.Usc;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace NerfItLib
{
  /// <summary>
  /// Gun.
  /// </summary>
  public class Gun : IDisposable
  {
    private PointF RETICLE_CENTER_OFFSET = new PointF (15, -10);
    private Usc _Usc;
    private UscSettings _UscSettings;
    private ChannelSetting _Channel0Pan;
    private ChannelSetting _Channel1Tilt;
    private ChannelSetting _Channel2Fire;
    private HaarCascade _HaarCascade;
    private ImageCodecInfo _JpegCodecInfo;
    private EncoderParameters _EncoderParams;
    private int _CameraIndex;
    private Capture _ImageCapture;
    private System.Threading.Thread _CameraThread;
    private bool _Exiting;
    private bool _IsFiring;
    private int _FiringImageCount;

    /// <summary>
    /// Pans and tilts, returning after stablization.
    /// </summary>
    /// <param name='pan'>
    /// Pan.
    /// </param>
    /// <param name='tilt'>
    /// Tilt.
    /// </param>
    public void PanTilt (decimal pan, decimal tilt)
    {
      Pan = pan;
      Tilt = tilt;
      WaitForCompletion ();
    }

    /// <summary>
    /// Waits for completion of all Pan/Tilt operations.
    /// </summary>
    public void WaitForCompletion ()
    {
      bool Stablized = false;
      int TotalSleepTime = 0;

      while (!Stablized && TotalSleepTime < 3500) {
        Stablized = PanStablized && TiltStablized;
        System.Threading.Thread.Sleep (100);
        TotalSleepTime += 100;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="NerfItLib.Gun"/> pan stablized.
    /// </summary>
    /// <value>
    /// <c>true</c> if pan stablized; otherwise, <c>false</c>.
    /// </value>
    public bool PanStablized {
      get {
        return Math.Abs (Pan - ActualPan) < .0001m;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="NerfItLib.Gun"/> tilt stablized.
    /// </summary>
    /// <value>
    /// <c>true</c> if tilt stablized; otherwise, <c>false</c>.
    /// </value>
    public bool TiltStablized {
      get {
        return Math.Abs (Tilt - ActualTilt) < .0001m;
      }
    }

    /// <summary>
    /// Gets the actual pan.
    /// </summary>
    /// <value>
    /// The actual pan.
    /// </value>
    public decimal ActualPan {
      get {
        ServoStatus[] Servos;
        _Usc.getVariables (out Servos);
        ServoStatus PanServo = Servos [0];
        return ((decimal)(PanServo.position - _Channel0Pan.minimum)) / ((decimal)(_Channel0Pan.maximum - _Channel0Pan.minimum));
      }
    }

    /// <summary>
    /// Gets the actual tilt.
    /// </summary>
    /// <value>
    /// The actual tilt.
    /// </value>
    public decimal ActualTilt {
      get {
        ServoStatus[] Servos;
        _Usc.getVariables (out Servos);
        ServoStatus TiltServo = Servos [1];
        return ((decimal)(TiltServo.position - _Channel1Tilt.minimum)) / ((decimal)(_Channel1Tilt.maximum - _Channel1Tilt.minimum));
      }
    }

    /// <summary>
    /// Gets or sets the pan.
    /// </summary>
    /// <value>
    /// Between 0 and 1
    /// </value>
    public decimal Pan {
      get {
        ServoStatus[] Servos;
        _Usc.getVariables (out Servos);
        ServoStatus PanServo = Servos [0];
        return ((decimal)(PanServo.target - _Channel0Pan.minimum)) / ((decimal)(_Channel0Pan.maximum - _Channel0Pan.minimum));
      }
      set {
        if (value > 1m || value < 0m)
          throw new ArgumentOutOfRangeException ("Cannot pan outside the range of 0 thru 1!");
        var Target = (ushort)(_Channel0Pan.minimum + (value * (_Channel0Pan.maximum - _Channel0Pan.minimum)));
        _Usc.setTarget (0, Target);
      }
    }

    /// <summary>
    /// Gets or sets the tilt.
    /// </summary>
    /// <value>
    /// The tilt.
    /// </value>
    /// <exception cref='ArgumentOutOfRangeException'>
    /// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
    /// specified by the method.
    /// </exception>
    public decimal Tilt {
      get {
        ServoStatus[] Servos;
        _Usc.getVariables (out Servos);
        ServoStatus TiltServo = Servos [1];
        return ((decimal)(TiltServo.target - _Channel1Tilt.minimum)) / ((decimal)(_Channel1Tilt.maximum - _Channel1Tilt.minimum));
      }
      set {
        if (value > 1m || value < 0m)
          throw new ArgumentOutOfRangeException ("Cannot tilt outside the range of 0 thru 1!");
        var Target = (ushort)(_Channel1Tilt.minimum + (value * (_Channel1Tilt.maximum - _Channel1Tilt.minimum)));
        _Usc.setTarget (1, Target);
      }
    }

    /// <summary>
    /// Fire one shot.
    /// </summary>
    public void Fire ()
    {
      _Usc.setTarget (2, _Channel2Fire.minimum);
      System.Threading.Thread.Sleep (300);
      _Usc.setTarget (2, _Channel2Fire.maximum);
      System.Threading.Thread.Sleep (400);
      _IsFiring = true;
      System.Threading.Thread.Sleep (600);
      _IsFiring = false;
      _FiringImageCount = 0;
    }

    private void CameraThread ()
    {
      while (!_Exiting) {
        using (Image<Bgr, byte> NextFrame = _ImageCapture.QueryFrame ()) {
          if (NextFrame != null) {
            // Run face detection and draw a box around each face.
            using (Image<Gray, byte> grayframe = NextFrame.Convert<Gray, byte> ()) {
              var faces = _HaarCascade.Detect (grayframe);

              foreach (var face in faces)
                NextFrame.Draw (face.rect, new Bgr (0, double.MaxValue, 0), 2);
            }
            PointF ReticleCenter = new PointF ((NextFrame.Width / 2) + RETICLE_CENTER_OFFSET.X, (NextFrame.Height / 2) + RETICLE_CENTER_OFFSET.Y);
            CircleF Reticle = new CircleF (ReticleCenter, 15);
            NextFrame.Draw (Reticle, new Bgr (double.MaxValue / 2, double.MaxValue / 2, double.MaxValue / 2), 2);

            using (Bitmap NextBitmapFrame = NextFrame.ToBitmap ()) {
              lock (_ImageCapture) {
                if (_IsFiring) {
                  NextBitmapFrame.Save (string.Format ("image{0}.jpg", _FiringImageCount), _JpegCodecInfo, _EncoderParams);
                  _FiringImageCount++;
                }
                NextBitmapFrame.Save ("image.jpg", _JpegCodecInfo, _EncoderParams);
              }
            }
          }
        }
        if (!_IsFiring)
          System.Threading.Thread.Sleep (200);
      }
    }

    /// <summary>
    /// Captures a new JPEG image from the gun's on-board webcam.
    /// </summary>
    /// <returns>
    /// Memory stream with jpeg contents.
    /// </returns>
    public System.IO.Stream CaptureJpegImage ()
    {
      lock (_ImageCapture)
        return new System.IO.FileStream ("image.jpg", System.IO.FileMode.Open);
    }

    /// <summary>
    /// Gets the encoder info.
    /// </summary>
    /// <returns>
    /// The encoder info.
    /// </returns>
    /// <param name='mimeType'>
    /// MIME type.
    /// </param>
    private static ImageCodecInfo GetEncoderInfo (string mimeType)
    {
      // Get image codecs for all image formats
      ImageCodecInfo[] Codecs = ImageCodecInfo.GetImageEncoders ();

      // Find the correct image codec
      for (int i = 0; i < Codecs.Length; i++)
        if (Codecs [i].MimeType == mimeType)
          return Codecs [i];
      return null;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="NerfItLib.Gun"/> class.
    /// </summary>
    public Gun ()
    {
      // Initialize Pololu
      var ConnectedDevices = Usc.getConnectedDevices ();
      if (ConnectedDevices.Count == 0)
        throw new ApplicationException ("Could not find a connected usb device!");

      _Usc = new Usc (ConnectedDevices [0]);
      using (var ConfigStreamReader = new System.IO.StreamReader("NerfConf.maestro")) {
        _UscSettings = Pololu.Usc.ConfigurationFile.load (ConfigStreamReader, new List<string> ());
      }
      _Usc.setUscSettings (_UscSettings, false);
      _Channel0Pan = _UscSettings.channelSettings [0];
      _Channel1Tilt = _UscSettings.channelSettings [1];
      _Channel2Fire = _UscSettings.channelSettings [2];
      _Usc.clearErrors ();
      _Usc.eraseScript ();
      _Usc.reinitialize ();

      // Jpeg image codec
      _JpegCodecInfo = GetEncoderInfo ("image/jpeg");

      // Initialize Webcam
      _EncoderParams = new EncoderParameters (1);
      _EncoderParams.Param [0] = new EncoderParameter (Encoder.Quality, 100L);

      _HaarCascade = new HaarCascade ("/usr/share/opencv/haarcascades/haarcascade_frontalface_alt2.xml");
      _CameraIndex = int.Parse (ConfigurationManager.AppSettings ["CameraIndex"]);
      _ImageCapture = new Capture (_CameraIndex);

      _CameraThread = new System.Threading.Thread (new System.Threading.ThreadStart (CameraThread));
      _CameraThread.Start ();
    }

    #region IDisposable implementation
    private bool _IsDisposed;

    /// <summary>
    /// Dispose the specified isDisposing.
    /// </summary>
    /// <param name='isDisposing'>
    /// Is disposing.
    /// </param>
    protected virtual void Dispose (bool isDisposing)
    {
      if (!_IsDisposed) {
        if (isDisposing) {
          _Exiting = true;

          if (_CameraThread != null) {
            if (!_CameraThread.Join (2000))
              _CameraThread.Abort ();
          }

          if (_HaarCascade != null)
            _HaarCascade.Dispose ();

          if (_ImageCapture != null)
            _ImageCapture.Dispose ();

          if (_Usc != null) {
            _Usc.disablePWM ();
            _Usc.disconnect ();
          }
        }
        _IsDisposed = true;
      }
    }

    /// <summary>
    /// Releases all resource used by the <see cref="NerfItLib.Gun"/> object.
    /// </summary>
    /// <remarks>
    /// Call Dispose when you are finished using the <see cref="NerfItLib.Gun"/>. The
    /// Dispose method leaves the <see cref="NerfItLib.Gun"/> in an unusable state. After calling
    /// Dispose, you must release all references to the <see cref="NerfItLib.Gun"/> so the garbage
    /// collector can reclaim the memory that the <see cref="NerfItLib.Gun"/> was occupying.
    /// </remarks>
    public void Dispose ()
    {
      Dispose (true);
      GC.SuppressFinalize (this);
    }
    #endregion
  }
}

