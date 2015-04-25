using System;

namespace NerfItCli
{
  class MainCli
  {
    public static void Main (string[] args)
    {
      using (NerfItLib.Gun g = new NerfItLib.Gun ()) {
        char KeyPress = char.MinValue;
        while (KeyPress!='Q') {
          Console.WriteLine ("Pan: {0:N4}; Tilt: {1:N4};", g.Pan, g.Tilt);
          KeyPress = char.ToUpperInvariant (Console.ReadKey (true).KeyChar);
          Console.Clear ();

          switch (KeyPress) {
          case 'H':
            if (g.Pan >= 0.05m)
              g.Pan -= .05m;
            else
              g.Pan = 0m;
            break;
          case 'L':
            if (g.Pan <= 0.95m)
              g.Pan += .05m;
            else
              g.Pan = 1m;
            break;
          case 'J':
            if (g.Tilt >= 0.05m)
              g.Tilt -= .05m;
            else
              g.Tilt = 0m;
            break;
          case 'K':
            if (g.Tilt <= 0.95m)
              g.Tilt += .05m;
            else
              g.Tilt = 1m;
            break;
          case 'G':
            Console.Write ("Enter Pan and Tilt in the form #.###;#.###: ");
            var Coordinates = Console.ReadLine ().Split (';');
            var Pan = decimal.Parse (Coordinates [0]);
            var Tilt = decimal.Parse (Coordinates [1]);
            g.PanTilt (Pan, Tilt);
            break;
          case ' ':
            Console.WriteLine ("Fire!");
            g.Fire ();
            break;
          }
        }
      }
    }
  }
}
