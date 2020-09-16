using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.MusicTheory;
using Accessibility;

namespace Glisztering
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            WinformsInit();

            //OpenFileDialog ofd = new OpenFileDialog()
            //{
            //    Filter = "MIDI files (*.mid)|*.mid",
            //    Multiselect = false,
            //};
            //DialogResult res = ofd.ShowDialog();
            //if (res != DialogResult.OK)
            //    return;

            //MidiFile midi = MidiFile.Read(ofd.FileName);

            //NAudioWaveOutput output = new NAudioWaveOutput();
            //using (var playback = midi.GetPlayback(output))
            //{
            //    output.pb = playback;
            //    playback.Play();
            //    while (playback.IsRunning)
            //        System.Threading.Thread.Sleep(1000);
            //}

            byte min = 36;
            byte max = 82;

            RandoNotePlayer rnp = new RandoNotePlayer();
            Scale chosenScale = new Scale(ScaleIntervals.MixolydianB6M, NoteName.B);

            for (int i = 0; i < 50; i++)
            {
                //rnp.PlayRandomNote(min, max);
                rnp.PlayNoteInScale(chosenScale);
                System.Threading.Thread.Sleep(250);
            }

            return;
        }

        static void WinformsInit()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }

    public class RandoNotePlayer
    {
        Random r = new Random();
        WaveOutEvent wo = new WaveOutEvent();
        int counter = 0;

        Dictionary<int, SignalGenerator> gens = new Dictionary<int, SignalGenerator>();

        private void PlayNote(Note n)
        {
            if (wo != null)
                KillCurrNote();

            double freq = n.CalcFrequency();

            SignalGenerator gen;

            if (gens.ContainsKey(n.NoteNumber))
                gen = gens[n.NoteNumber];
            else
                gen = new SignalGenerator()
                {
                    Gain = 0.2,
                    Frequency = freq,
                    Type = SignalGeneratorType.Sin
                };

            wo.Init(gen);
            wo.Play();
        }

        private void KillCurrNote()
        {
            wo.Stop();
        }

        public void PlayRandomNote(byte min, byte max)
        {
            byte rand = (byte)r.Next(min, max);

            Note n = Note.Get((SevenBitNumber)rand);

            PlayNote(n);
        }

        public void PlayNoteInScale(Scale scale)
        {
            byte rand = (byte)r.Next(0, 8);

            Note n;
            if (counter % 5 != 0)
                n = scale.GetAscendingNotes(Note.Get(scale.RootNote, 4)).ElementAt(rand);
            else
            {
                counter = 0;
                n = Note.Get(scale.RootNote, 4);
            }

            counter++;

            PlayNote(n);
        }
    }

    public class NAudioWaveOutput : IOutputDevice
    {
        public Playback pb;

        public event EventHandler<MidiEventSentEventArgs> EventSent;

        private Dictionary<int, WaveOutEvent> waves = new Dictionary<int, WaveOutEvent>();
        private long tempo = 5000000;

        public void PrepareForEventsSending() { }

        public void SendEvent(MidiEvent midiEvent)
        {
            int tpqn = (pb.TempoMap.TimeDivision as TicksPerQuarterNoteTimeDivision).TicksPerQuarterNote;
            double duration = ((tempo / tpqn) / 100000) * midiEvent.DeltaTime;

            if (midiEvent is SetTempoEvent)
            {
                SetTempoEvent ev = midiEvent as SetTempoEvent;
                tempo = ev.MicrosecondsPerQuarterNote;
            }

            if (midiEvent is NoteOnEvent)
            {
                NoteOnEvent ev = midiEvent as NoteOnEvent;
                Note n = Note.Get(ev.NoteNumber);
                double freq = n.CalcFrequency();

                var gen = new SignalGenerator()
                {
                    Gain = 0.2,
                    Frequency = freq,
                    Type = SignalGeneratorType.Sin
                };

                var wo = new WaveOutEvent();
                wo.Init(gen);

                if (waves.ContainsKey(ev.NoteNumber))
                {
                    waves[ev.NoteNumber].Stop();
                    waves[ev.NoteNumber].Dispose();
                    waves[ev.NoteNumber] = wo;
                }
                else
                {
                    waves.Add(ev.NoteNumber, wo);
                }

                wo.Play();
            }

            if (midiEvent is NoteOffEvent)
            {
                NoteOffEvent ev = midiEvent as NoteOffEvent;

                if (waves.ContainsKey(ev.NoteNumber))
                {
                    waves[ev.NoteNumber].Stop();
                    waves[ev.NoteNumber].Dispose();
                }
            }

            EventSent?.Invoke(this, new MidiEventSentEventArgs(midiEvent));

            if (midiEvent.DeltaTime != 0)
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(duration));
        }
    }
}
