using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.MusicTheory;

namespace Glisztering
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            WinformsInit();

            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "MIDI files (*.mid)|*.mid",
                Multiselect = false,
            };
            DialogResult res = ofd.ShowDialog();
            if (res != DialogResult.OK)
                return;

            MidiFile midi = MidiFile.Read(ofd.FileName);

            NAudioWaveOutput output = new NAudioWaveOutput();
            using (var playback = midi.GetPlayback(output))
            {
                output.midiFile = midi;
                output.pb = playback;
                playback.Play();
                System.Threading.Thread.Sleep(10000);
            }
        }

        static void WinformsInit()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }

    public class NAudioWaveOutput : IOutputDevice
    {
        public MidiFile midiFile;
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
                    waves[ev.NoteNumber].Stop();
            }

            EventSent?.Invoke(this, new MidiEventSentEventArgs(midiEvent));

            if (midiEvent.DeltaTime != 0)
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(duration));
        }
    }
}
