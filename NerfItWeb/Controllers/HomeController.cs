using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace Controllers
{
  /// <summary>
  /// Home controller.
  /// </summary>
 [HandleError]
  public class HomeController : Controller
  {
    private static NerfItLib.Gun _Gun = new NerfItLib.Gun ();
    private static LinkedList<string> _RecentActivity = new LinkedList<string> ();
    private static System.Timers.Timer _GoHomeTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Controllers.HomeController"/> class.
    /// </summary>
    public HomeController ()
    {
      lock (_Gun) {
        if (_GoHomeTimer == null) {
          _GoHomeTimer = new System.Timers.Timer (40000) { AutoReset=true, Enabled=true};
          _GoHomeTimer.Elapsed += Handle_GoHomeTimerElapsed;
        }
      }
    }

    private static void Handle_GoHomeTimerElapsed (object sender, System.Timers.ElapsedEventArgs e)
    {
      if (_Gun.Pan != .5m || _Gun.Tilt != .5m) {
        lock (_Gun) {
          UpdateActivity ("Timeout, going home...", false);
          _Gun.PanTilt (.5m, .5m);
        }
      }
    }

    private static void UpdateActivity (string message)
    {
      UpdateActivity (message, true);
    }

    private static void UpdateActivity (string message, bool includeDetails)
    {
      string NewMessage;

      if (includeDetails)
        NewMessage = string.Format ("{0:d} {0:t} CST - {1} {2}", DateTime.Now, System.Web.HttpContext.Current.Request.UserHostAddress, message);
      else
        NewMessage = message;

      if (_RecentActivity.Count == 0 || _RecentActivity.First.Value != NewMessage)
        _RecentActivity.AddFirst (NewMessage);

      if (_RecentActivity.Count > 20)
        _RecentActivity.RemoveLast ();

      _GoHomeTimer.Interval = _GoHomeTimer.Interval;
    }
    /// <summary>
    /// Index this instance.
    /// </summary>
    [AcceptVerbs(HttpVerbs.Get)]
    public ActionResult Index ()
    {
      NerfItWeb.Models.PanTiltModel Model;
      lock (_Gun) {
        UpdateActivity ("Page load...");
        Model = new NerfItWeb.Models.PanTiltModel () { Pan=_Gun.Pan, Tilt=_Gun.Tilt, History = _RecentActivity.ToList()};
      }
      return View (Model);
    }

    /// <summary>
    /// Fire the gun.
    /// </summary>
    /// <param name='formValues'>
    /// Form values (Pan / Tilt)
    /// </param>
    [AcceptVerbs(HttpVerbs.Post)]
    public ActionResult Index (FormCollection formValues)
    {
      decimal Pan;
      decimal Tilt;
      if (!decimal.TryParse (formValues ["PanTextbox"], out Pan))
        Pan = _Gun.Pan;
      if (!decimal.TryParse (formValues ["TiltTextbox"], out Tilt))
        Tilt = _Gun.Tilt;

      lock (_Gun) {
        _Gun.PanTilt (Pan, Tilt);

        if (formValues ["submit"] == "Fire") {
          UpdateActivity (string.Format ("Fire at ({0:N3},{1:N3})!!", _Gun.Pan, _Gun.Tilt));
          _Gun.Fire ();
        } else
          UpdateActivity (string.Format ("Goto position ({0:N3}, {1:N3})...", _Gun.Pan, _Gun.Tilt));
      }

      return RedirectToAction ("Index");
    }

    /// <summary>
    /// Image from the webcam
    /// </summary>
    public ActionResult Image ()
    {
      System.IO.Stream ImageStream;

      lock (_Gun) {
//        UpdateActivity ("Loading image...");
        ImageStream = _Gun.CaptureJpegImage ();
      }

      return File (ImageStream, "image/jpeg", "image.jpg");
    }

    /// <summary>
    /// Returns a single image by filename.
    /// </summary>
    /// <returns>
    /// The image.
    /// </returns>
    /// <param name='filename'>
    /// Filename.
    /// </param>
    public ActionResult ActionImage (string filename)
    {
      var ImageStream = new System.IO.FileStream (filename, System.IO.FileMode.Open);
      return File (ImageStream, "image/jpeg", filename);
    }

    /// <summary>
    /// Retrieves a page with the last set of images taken while firing the gun.
    /// </summary>
    /// <returns>
    /// The images.
    /// </returns>
    public ActionResult ActionImages ()
    {
      var ActionHtml = "<html><body>";
      var ImageFiles = System.IO.Directory.EnumerateFiles (".", "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
      foreach (var ImageFile in ImageFiles)
        ActionHtml += string.Format ("<img src='/Home/ActionImage?filename={0}' /><br/>", Server.UrlEncode (System.IO.Path.GetFileName (ImageFile)));
      ActionHtml += "</body></html";
      return this.Content (ActionHtml, "text/html");
    }

    /// <summary>
    /// History this instance.
    /// </summary>
    public ActionResult History ()
    {
      string HistoryHtml = "";

      lock (_Gun) {
        foreach (var Message in _RecentActivity)
          HistoryHtml += Message + "<br/>";
      }

      return this.Content (HistoryHtml);
    }
  }
}

