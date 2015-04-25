using System;
using System.Collections.Generic;

namespace NerfItWeb.Models
{
  public class PanTiltModel
  {
    private decimal _Pan;
    private decimal _Tilt;
    private List<string> _History;

    public List<string> History {
      get{ return _History;}
      set{ _History = value;}
    }

    public decimal Pan {
      get{ return _Pan;}
      set{ _Pan = value;}
    }

    public decimal Tilt {
      get{ return _Tilt;}
      set{ _Tilt = value;}
    }

    public PanTiltModel ()
    {
    }
  }
}

