using System;
using System.Collections.Generic;
using System.Text;
using Melanchall.DryWetMidi.MusicTheory;

namespace Glisztering
{
    public class GAudioUtils
    {
        public void n()
        {
            Melanchall.DryWetMidi.MusicTheory.Note n = Melanchall.DryWetMidi.MusicTheory.Note.Get(Melanchall.DryWetMidi.MusicTheory.NoteName.ASharp, 4);
        }


    }

    public static class GDWMUtils
    {
        public static double CalcFrequency(this Note n, double A4freq = 440)
        {
            // https://pages.mtu.edu/~suits/NoteFreqCalcs.html

            int diff = (n.NoteNumber - 69);
            double a = Math.Pow(2, 1.0 / 12.0);

            double freq = A4freq * Math.Pow(a, diff);

            return freq;
        }
    }
}
