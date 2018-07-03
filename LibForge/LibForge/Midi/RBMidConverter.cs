﻿using System;
using System.Collections.Generic;
using MidiCS;
using System.Linq;

namespace LibForge.Midi
{
  public class RBMidConverter
  {
    public static RBMid ToRBMid(MidiFile mf, int hopoThreshold = 170)
    {
      return new MidiConverter(mf, hopoThreshold).ToRBMid();
    }
    public static MidiFile ToMid(RBMid m)
    {
      return new MidiFile(MidiFormat.MultiTrack, new List<MidiTrack>(m.MidiTracks), 480);
    }

    public class MidiConverter
    {
      private MidiFile mf;
      private RBMid rb;
      private List<MidiTrackProcessed> processedTracks;

      private List<RBMid.LYRICS> Lyrics;
      private List<RBMid.DRUMFILLS> DrumFills;
      private List<RBMid.ANIM> Anims;
      private List<RBMid.TOMMARKER> ProMarkers;
      private List<RBMid.LANEMARKER> LaneMarkers;
      private List<RBMid.GTRTRILLS> TrillMarkers;
      private List<RBMid.DRUMMIXES> DrumMixes;
      private List<RBMid.GEMTRACK> GemTracks;
      private List<RBMid.SECTIONS> OverdriveSoloSections;
      private List<RBMid.VOCALTRACK> VocalTracks;
      private List<RBMid.UNKSTRUCT1> Unknown4;
      private List<RBMid.VocalTrackRange> VocalRanges;
      // TODO: multiple ranges? that's probably a thing?
      private RBMid.VocalTrackRange theVocalRange = new RBMid.VocalTrackRange { LowNote = 100, HighNote = 0 };
      private List<RBMid.MAP[]> HandMap;
      private List<RBMid.HANDPOS[]> HandPos;
      private List<RBMid.MAP[]> strumMaps;
      private List<RBMid.MARKUP_SOLO_NOTES> MarkupSoloNotes1, MarkupSoloNotes2, MarkupSoloNotes3;
      private List<RBMid.TWOTICKS> SoloLoops1, SoloLoops2;
      private List<RBMid.MARKUPCHORD> MarkupChords1;
      private List<RBMid.TEMPO> Tempos;
      private List<RBMid.TIMESIG> TimeSigs;
      private List<RBMid.BEAT> Beats;
      private List<string> MidiTrackNames;
      private float PreviewStart;
      private float PreviewEnd;
      private uint LastMarkupTick;
      private uint FinalTick;
      private int hopoThreshold;
      private List<uint> MeasureTicks = new List<uint>() { 0U };

      public MidiConverter(MidiFile mf, int hopoThreshold = 170)
      {
        this.mf = mf;
        this.hopoThreshold = hopoThreshold;
        processedTracks = new MidiHelper().ProcessTracks(mf);
        trackHandlers = new Dictionary<string, Action<MidiTrackProcessed>>
        {
          {"PART DRUMS", HandleDrumTrk },
          {"PART BASS", HandleGuitarBass },
          {"PART GUITAR", HandleGuitarBass },
          {"PART REAL_KEYS_X", HandleRealKeysXTrk },
          {"PART KEYS_ANIM_RH", HandleKeysAnimTrk },
          {"PART KEYS_ANIM_LH", HandleKeysAnimTrk },
          {"PART VOCALS", HandleVocalsTrk },
          {"HARM1", HandleVocalsTrk },
          {"HARM2", HandleVocalsTrk },
          {"HARM3", HandleVocalsTrk },
          {"EVENTS", HandleEventsTrk },
          {"BEAT", HandleBeatTrk },
          {"MARKUP", HandleMarkupTrk },
          {"VENUE", HandleVenueTrk }
        };
      }

      public RBMid ToRBMid()
      {
        Lyrics = new List<RBMid.LYRICS>();
        DrumFills = new List<RBMid.DRUMFILLS>();
        Anims = new List<RBMid.ANIM>();
        ProMarkers = new List<RBMid.TOMMARKER>();
        LaneMarkers = new List<RBMid.LANEMARKER>();
        TrillMarkers = new List<RBMid.GTRTRILLS>();
        DrumMixes = new List<RBMid.DRUMMIXES>();
        GemTracks = new List<RBMid.GEMTRACK>();
        OverdriveSoloSections = new List<RBMid.SECTIONS>();
        VocalTracks = new List<RBMid.VOCALTRACK>();
        Unknown4 = new List<RBMid.UNKSTRUCT1>();
        VocalRanges = new List<RBMid.VocalTrackRange>();
        HandMap = new List<RBMid.MAP[]>();
        HandPos = new List<RBMid.HANDPOS[]>();
        strumMaps = new List<RBMid.MAP[]>();
        MarkupSoloNotes1 = new List<RBMid.MARKUP_SOLO_NOTES>();
        SoloLoops1 = new List<RBMid.TWOTICKS>();
        MarkupChords1 = new List<RBMid.MARKUPCHORD>();
        MarkupSoloNotes2 = new List<RBMid.MARKUP_SOLO_NOTES>();
        MarkupSoloNotes3 = new List<RBMid.MARKUP_SOLO_NOTES>();
        SoloLoops2 = new List<RBMid.TWOTICKS>();
        Tempos = new List<RBMid.TEMPO>();
        TimeSigs = new List<RBMid.TIMESIG>();
        Beats = new List<RBMid.BEAT>();
        MidiTrackNames = new List<string>();
        var lastTimeSig = mf.TempoTimeSigMap[0];
        var measure = 0;
        foreach (var tempo in mf.TempoTimeSigMap)
        {
          if (tempo.NewTempo)
            Tempos.Add(new RBMid.TEMPO
            {
              StartTick = (uint)tempo.Tick,
              StartMillis = (float)(tempo.Time * 1000.0),
              Tempo = (int)(60_000_000 / (float)tempo.BPM)
            });
          if(tempo.NewTimeSig)
          {
            if (tempo.Tick > 0)
            {
              var elapsed = tempo.Tick - lastTimeSig.Tick;
              var ticksPerBeat = (480 * 4) / lastTimeSig.Denominator;
              measure += (int)(elapsed / ticksPerBeat / lastTimeSig.Numerator);
              var lastMeasureTick = MeasureTicks.LastOrDefault();
              for (var i = MeasureTicks.Count; i < measure; i++)
              {
                lastMeasureTick += 480U * lastTimeSig.Numerator * 4 / lastTimeSig.Denominator;
                MeasureTicks.Add(lastMeasureTick);
              }
            }
            TimeSigs.Add(new RBMid.TIMESIG
            {
              Numerator = tempo.Numerator,
              Denominator = tempo.Denominator,
              Tick = (uint)tempo.Tick,
              Measure = measure
            });
            lastTimeSig = tempo;
          }
        }
        uint lastMeasureTick2 = MeasureTicks.LastOrDefault();
        FinalTick = processedTracks.Select(t => t.LastTick).Max();
        for (var i = MeasureTicks.Count; lastMeasureTick2 < FinalTick; i++)
        {
          lastMeasureTick2 += 480U * lastTimeSig.Numerator * 4 / lastTimeSig.Denominator;
          if (lastMeasureTick2 >= FinalTick) break;
          MeasureTicks.Add(lastMeasureTick2);
        }
        var trackNames = new[] {
          processedTracks[0].Name,
          "PART DRUMS",
          "PART BASS",
          "PART REAL_BASS",
          "PART GUITAR",
          "PART REAL_GUITAR",
          // TODO: Allow these in release builds when shit's no longer borked
#if DEBUG
          "PART KEYS",
          "PART REAL_KEYS_X",
          "PART REAL_KEYS_H",
          "PART REAL_KEYS_M",
          "PART REAL_KEYS_E",
          "PART KEYS_ANIM_RH",
          "PART KEYS_ANIM_LH",
#endif
          "PART VOCALS",
          "HARM1",
          "HARM2",
          "HARM3",
          "EVENTS",
          "BEAT",
          "MARKUP",
        };
        foreach(var trackname in trackNames)
        {
          var track = processedTracks.Where(x => x.Name == trackname).FirstOrDefault();
          if(track != null)
          {
            ProcessTrack(track);
          }
        }
        VocalRanges.Add(theVocalRange);
        rb = new RBMid
        {
          Format = 0x10,
          Lyrics = Lyrics.ToArray(),
          DrumFills = DrumFills.ToArray(),
          Anims = Anims.ToArray(),
          ProMarkers = ProMarkers.ToArray(),
          LaneMarkers = LaneMarkers.ToArray(),
          TrillMarkers = TrillMarkers.ToArray(),
          DrumMixes = DrumMixes.ToArray(),
          GemTracks = GemTracks.ToArray(),
          OverdriveSoloSections = OverdriveSoloSections.ToArray(),
          VocalTracks = VocalTracks.ToArray(),
          Unknown4 = Unknown4.ToArray(),
          VocalRange = VocalRanges.ToArray(),
          HandMaps = HandMap.ToArray(),
          GuitarLeftHandPos = HandPos.ToArray(),
          StrumMaps = strumMaps.ToArray(),
          MarkupSoloNotes1 = MarkupSoloNotes1.ToArray(),
          MarkupLoop1 = SoloLoops1.ToArray(),
          MarkupChords1 = MarkupChords1.ToArray(),
          MarkupSoloNotes2 = MarkupSoloNotes2.ToArray(),
          MarkupSoloNotes3 = MarkupSoloNotes3.ToArray(),
          MarkupLoop2 = SoloLoops2.ToArray(),
          MidiTracks = mf.Tracks.ToArray(),
          Tempos = Tempos.ToArray(),
          TimeSigs = TimeSigs.ToArray(),
          Beats = Beats.ToArray(),
          FinalTick = FinalTick + 1,
          Measures = (uint)MeasureTicks.Count(),
          Unknown = new uint[6],
          FinalTickMinusOne = FinalTick,
          UnknownFloats = new float[4] { -1, -1, -1, -1 },
          MidiTrackNames = mf.Tracks.Select(t => t.Name).ToArray(),
          PreviewStartMillis = PreviewStart,
          PreviewEndMillis = PreviewEnd,
          UnknownTwo = 2,
          LastMarkupEventTick = LastMarkupTick,
          NumPlayableTracks = (uint)Lyrics.Count,
          FinalEventTick = processedTracks.Where(t=>t.Name == "EVENTS").Select(t=>t.LastTick).First(),
          UnknownHundred = 100f,
          UnknownNegOne = -1,
          UnknownOne = 1,
          UnknownZeroByte = 0,
          UnknownZero = 0,
          HopoThreshold = hopoThreshold,
        };
        return rb;
      }
      private Dictionary<string, Action<MidiTrackProcessed>> trackHandlers;
      private MidiTrackProcessed currentTrack;

      private void ProcessTrack(MidiTrackProcessed track)
      {
        currentTrack = track;
        if (track.LastTick > FinalTick) FinalTick = track.LastTick;
        MidiTrackNames.Add(track.Name);
        if (MidiTrackNames.Count == 1)
          return;
        else if (trackHandlers.ContainsKey(track.Name))
          trackHandlers[track.Name](track);
      }

      const byte Roll2 = 127;
      const byte Roll1 = 126;
      const byte DrumFillMarkerEnd = 124;
      const byte DrumFillMarkerStart = 120;
      const byte OverdriveMarker = 116;
      const byte ProGreen = 112;
      const byte ProBlue = 111;
      const byte ProYellow = 110;
      const byte SoloMarker = 103;
      const byte ExpertHopoOff = 102;
      const byte ExpertHopoOn = 101;
      const byte ExpertEnd = 100;
      const byte ExpertStart = 96;
      const byte HardHopoOff = 90;
      const byte HardHopoOn = 89;
      const byte HardEnd = 88;
      const byte HardStart = 84;
      const byte MediumEnd = 76;
      const byte MediumStart = 72;
      const byte EasyEnd = 64;
      const byte EasyStart = 60;
      const byte DrumAnimEnd = 51;
      const byte DrumAnimStart = 24;
      private void HandleDrumTrk(MidiTrackProcessed track)
      {
        var drumfills = new List<RBMid.DRUMFILLS.FILL>();
        var fills_unk = new List<RBMid.DRUMFILLS.FILL_LANES>();
        var tom_markers = new SortedDictionary<uint, RBMid.TOMMARKER.MARKER>();
        var overdrive_markers = new List<RBMid.SECTIONS.SECTION>();
        var solo_markers = new List<RBMid.SECTIONS.SECTION>();
        var gem_tracks = new List<RBMid.GEMTRACK.GEM>[4];
        var rolls = new List<RBMid.LANEMARKER.MARKER>();

        tom_markers[0] = new RBMid.TOMMARKER.MARKER
        {
          Tick = 0, Flags = 0
        };
        var marker_ends = new uint[3];
        var mixes = new List<RBMid.TICKTEXT>[4];
        for (var i = 0; i < 4; i++)
        {
          mixes[i] = new List<RBMid.TICKTEXT>();
        }
        void SetMarkerOn(uint tick, RBMid.TOMMARKER.MARKER.FLAGS flag)
        {
          if (tom_markers.ContainsKey(tick))
          {
            tom_markers[tick].Flags |= flag;
          }
          else
          {
            var active_flag = 4;
            foreach(var end in marker_ends)
            {
              if (end > tick)
              {
                flag |= (RBMid.TOMMARKER.MARKER.FLAGS)active_flag;
              }
              active_flag <<= 1;
            }
            tom_markers[tick] = new RBMid.TOMMARKER.MARKER
            {
              Tick = tick,
              Flags = flag
            };
          }
          // HACK for superunknownrb4 which has a badly quantized PRO marker
          for(var diff = 2; diff < 4; diff++)
          {
            var count = gem_tracks[diff]?.Count ?? 0;
            if (count > 0 
              && tick - gem_tracks[diff][count - 1].StartTicks < 5
              && (gem_tracks[diff][count - 1].Lanes & (int)flag) != 0)
            {
              gem_tracks[diff][count - 1].ProCymbal = 0;
            }
          }
        }
        void SetMarkerOff(uint tick, RBMid.TOMMARKER.MARKER.FLAGS flag)
        {
          if (tom_markers.ContainsKey(tick))
          {
            tom_markers[tick].Flags &= ~flag;
          }
          else
          {
            RBMid.TOMMARKER.MARKER.FLAGS new_flag = 0;
            var active_flag = 4;
            foreach (var end in marker_ends)
            {
              if (end > tick)
              {
                new_flag |= (RBMid.TOMMARKER.MARKER.FLAGS)active_flag;
              }
              active_flag <<= 1;
            }
            tom_markers[tick] = new RBMid.TOMMARKER.MARKER
            {
              Tick = tick,
              Flags = new_flag
            };
          }
        }
        RBMid.TOMMARKER.MARKER.FLAGS GetFlag(byte key)
        {
          switch (key)
          {
            case ProYellow:
              return RBMid.TOMMARKER.MARKER.FLAGS.ProYellow;
            case ProBlue:
              return RBMid.TOMMARKER.MARKER.FLAGS.ProBlue;
            case ProGreen:
              return RBMid.TOMMARKER.MARKER.FLAGS.ProGreen;
          }
          return 0;
        }
        bool AddGem(MidiNote e)
        {
          var key = e.Key;
          var lane = 0;
          var diff = 0;
          if (key >= EasyStart && key <= EasyEnd)
          {
            lane = key - EasyStart;
            diff = 0;
          }
          else if (key >= MediumStart && key <= MediumEnd)
          {
            lane = key - MediumStart;
            diff = 1;
          }
          else if (key >= HardStart && key <= HardEnd)
          {
            lane = key - HardStart;
            diff = 2;
          }
          else if (key >= ExpertStart && key <= ExpertEnd)
          {
            lane = key - ExpertStart;
            diff = 3;
          }
          else
          {
            return false;
          }

          if (diff == 3 && rolls.Count > 0 && rolls[rolls.Count - 1].EndTick > e.StartTicks)
          {
            var tmp = rolls[rolls.Count - 1];
            tmp.Lanes |= 1 << lane;
            rolls[rolls.Count - 1] = tmp;
          }
          if (gem_tracks[diff] == null) gem_tracks[diff] = new List<RBMid.GEMTRACK.GEM>();
          var lastOverdrive = overdrive_markers.LastOrDefault();
          var proCymbal = (lane > 1 && marker_ends[lane - 2] <= e.StartTicks) ? 1 : 0;
          gem_tracks[diff].Add(new RBMid.GEMTRACK.GEM
          {
            StartMillis = (float)e.StartTime * 1000,
            StartTicks = e.StartTicks,
            LengthMillis = (ushort)(e.Length * 1000),
            LengthTicks = (ushort)e.LengthTicks,
            Lanes = 1 << lane,
            IsHopo = false,
            NoTail = true,
            // TODO: Sometimes this is not zero
            ProCymbal = proCymbal
          });
          return true;
        }
        // 
        var itemsOrdered = track.Items
          // If shorter notes come first we get better output for arabella (seems to be breaking things)
          .OrderBy(x => (x as MidiNote)?.LengthTicks ?? 0)
          .OrderBy(x => {
            // Sort modifiers to come before gems
            var key = (x as MidiNote)?.Key ?? 0;
            if (key <= ExpertEnd) key = 127;
            return key;
          })
          .OrderBy(x => x.StartTicks);
        foreach (var item in itemsOrdered)
        {
          var ticks = item.StartTicks;
          var time = item.StartTime;
          switch (item)
          {
            case MidiNote e:
              if (e.Key == DrumFillMarkerStart)
              {
                fills_unk.Add(new RBMid.DRUMFILLS.FILL_LANES
                {
                  Tick = ticks,
                  Lanes = 0b11111 // TODO: parse each lane
                });
                drumfills.Add(new RBMid.DRUMFILLS.FILL
                {
                  StartTick = ticks,
                  EndTick = ticks + e.LengthTicks, // TODO: this seems to be rounded up to the next note
                  IsBRE = 0
                });
              }
              else if (e.Key >= DrumFillMarkerStart && e.Key <= DrumFillMarkerEnd) { }
              else if (e.Key == OverdriveMarker)
              {
                overdrive_markers.Add(new RBMid.SECTIONS.SECTION
                {
                  StartTicks = ticks,
                  LengthTicks = e.LengthTicks
                });
              }
              else if (e.Key == SoloMarker)
              {
                solo_markers.Add(new RBMid.SECTIONS.SECTION
                {
                  StartTicks = ticks,
                  LengthTicks = e.LengthTicks
                });
              }
              else if (e.Key == ProYellow || e.Key == ProBlue || e.Key == ProGreen)
              {
                // Pro Tom Markers
                var flag = GetFlag(e.Key);
                SetMarkerOn(e.StartTicks, flag);
                SetMarkerOff(e.StartTicks + e.LengthTicks, flag);
                marker_ends[e.Key - ProYellow] = e.StartTicks + e.LengthTicks;
                foreach(var x in tom_markers.Values.Where(k => k.Tick >= e.StartTicks && k.Tick < e.StartTicks + e.LengthTicks))
                {
                  x.Flags |= flag;
                }
              }
              else if (AddGem(e)) { }  // everything is handled in AddGem
              else if (e.Key >= DrumAnimStart && e.Key <= DrumAnimEnd)
              {
                // TODO: Anims?
              }
              else if (e.Key == Roll1 || e.Key == Roll2)
              {
                rolls.Add(new RBMid.LANEMARKER.MARKER
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  Lanes = 0
                });
              }
              else if (e.Key == 105 || e.Key == 106 || e.Key == 12 || e.Key == 13 || e.Key == 14 || e.Key == 15)
              {
                // TODO: What are these note?
              }
              else
              {
                throw new Exception($"Unhandled midi note {e.Key} in drum track at time {e.StartTime}");
              }
              break;
            case MidiText e:
              switch (e.Text)
              {
                default:
                  var regex = new System.Text.RegularExpressions.Regex("\\[mix ([0-9]) (\\S+)\\]");
                  var match = regex.Match(e.Text);
                  if (match.Success)
                  {
                    var difficulty = Int32.Parse(match.Groups[1].Value);
                    var mix = match.Groups[2].Value;
                    mixes[difficulty].Add(new RBMid.TICKTEXT
                    {
                      Text = mix,
                      Tick = e.StartTicks
                    });
                  }
                  break;
              }
              break;
          }
        }
        Lyrics.Add(new RBMid.LYRICS
        {
          TrackName = track.Name,
          Lyrics = new RBMid.TICKTEXT[0],
          Unknown1 = 0,
          Unknown2 = 0,
          Unknown3 = 1
        });
        DrumFills.Add(new RBMid.DRUMFILLS
        {
          Fills = drumfills.ToArray(),
          Lanes = fills_unk.ToArray()
        });
        ProMarkers.Add(new RBMid.TOMMARKER
        {
          Markers = tom_markers.Values.ToArray()
        });
        LaneMarkers.Add(new RBMid.LANEMARKER
        {
          Markers = rolls.Count == 0 ? new RBMid.LANEMARKER.MARKER[0][] : new RBMid.LANEMARKER.MARKER[4][]
          {
            new RBMid.LANEMARKER.MARKER[0],
            new RBMid.LANEMARKER.MARKER[0],
            new RBMid.LANEMARKER.MARKER[0],
            rolls.ToArray()
          }
        });
        TrillMarkers.Add(new RBMid.GTRTRILLS { Trills = new RBMid.GTRTRILLS.TRILL[0][] });
        DrumMixes.Add(new RBMid.DRUMMIXES
        {
          Mixes = mixes.Select(m => m.ToArray()).ToArray()
        });
        GemTracks.Add(new RBMid.GEMTRACK
        {
          Gems = gem_tracks.Select(g => g.ToArray()).ToArray(),
          HopoThreshold = hopoThreshold
        });
        var sections = new RBMid.SECTIONS.SECTION[6][] {
          overdrive_markers.ToArray(),
          solo_markers.ToArray(),
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0]
        };
        OverdriveSoloSections.Add(new RBMid.SECTIONS
        {
          Sections = new RBMid.SECTIONS.SECTION[4][][]
          {
            sections, sections, sections, sections
          }
        });
        HandMap.Add(new RBMid.MAP[0]);
        HandPos.Add(new RBMid.HANDPOS[0]);
        strumMaps.Add(new RBMid.MAP[0]);
      }

      private static Dictionary<string, int> HandMaps = new Dictionary<string, int>
      {
        {"HandMap_Default", 0 },
        {"HandMap_AllBend", 1 },
        {"HandMap_AllChords", 2 },
        {"HandMap_Chord_A", 3 },
        {"HandMap_Chord_C", 4 },
        {"HandMap_Chord_D", 5 },
        {"HandMap_DropD", 6 },
        {"HandMap_DropD2", 7 },
        {"HandMap_NoChords", 8 },
        {"HandMap_Solo", 9 },
      };

      private static Dictionary<string, int> StrumMaps = new Dictionary<string, int>
      {
        {"StrumMap_Default", 0 },
        {"StrumMap_Pick", 1 },
        {"StrumMap_SlapBass", 2 },
      };

      const byte TrillMarker = 127;
      const byte TremoloMarker = 126;
      const byte LeftHandEnd = 59;
      const byte LeftHandStart = 40;
      struct Hopo {
        public uint EndTick;
        public enum State { NormalOff, NormalOn, ForcedOn, ForcedOff }
        public State state;
      };
      private void HandleGuitarBass(MidiTrackProcessed track)
      {
        var drumfills = new List<RBMid.DRUMFILLS.FILL>();
        var fills_unk = new List<RBMid.DRUMFILLS.FILL_LANES>();
        var gem_tracks = new List<RBMid.GEMTRACK.GEM>[4];
        RBMid.GEMTRACK.GEM[] chords = new RBMid.GEMTRACK.GEM[4];
        var trills = new List<RBMid.GTRTRILLS.TRILL>();
        var trill = new RBMid.GTRTRILLS.TRILL();
        var maps = new List<RBMid.MAP>();
        var left_hand = new List<RBMid.HANDPOS>();
        var overdrive_markers = new List<RBMid.SECTIONS.SECTION>();
        var solo_markers = new List<RBMid.SECTIONS.SECTION>();
        var tremolos = new List<RBMid.LANEMARKER.MARKER>();
        var strummaps = new List<RBMid.MAP>();
        var hopoState = new Hopo[]{
          new Hopo { EndTick = uint.MaxValue, state = Hopo.State.NormalOff },
          new Hopo { EndTick = uint.MaxValue, state = Hopo.State.NormalOff },
          new Hopo { EndTick = uint.MaxValue, state = Hopo.State.NormalOff },
          new Hopo { EndTick = uint.MaxValue, state = Hopo.State.NormalOff },
        };

        bool AddGem(MidiNote e)
        {
          var key = e.Key;
          var lane = 0;
          var diff = 0;
          if (key >= EasyStart && key <= EasyEnd)
          {
            lane = key - EasyStart;
            diff = 0;
          }
          else if (key >= MediumStart && key <= MediumEnd)
          {
            lane = key - MediumStart;
            diff = 1;
          }
          else if (key >= HardStart && key <= HardEnd)
          {
            lane = key - HardStart;
            diff = 2;
          }
          else if (key >= ExpertStart && key <= ExpertEnd)
          {
            lane = key - ExpertStart;
            diff = 3;
            if (trill.EndTick > e.StartTicks)
            {
              if (trill.FirstFret == -1)
              {
                trill.FirstFret = lane;
              }
              else if (trill.SecondFret == -1)
              {
                trill.SecondFret = lane;
              }
            }
          }
          else
          {
            return false;
          }
          if(diff == 3 && tremolos.Count > 0 && tremolos[tremolos.Count - 1].EndTick > e.StartTicks)
          {
            var tmp = tremolos[tremolos.Count - 1];
            tmp.Lanes |= 1 << lane;
            tremolos[tremolos.Count - 1] = tmp;
          }
          if (gem_tracks[diff] == null) gem_tracks[diff] = new List<RBMid.GEMTRACK.GEM>();
          if (chords[diff] != null && e.StartTicks - chords[diff].StartTicks < 5)
          { // additional gem in a chord
            if (chords[diff].Lanes != 0 && hopoState[diff].state != Hopo.State.ForcedOn || hopoState[diff].EndTick <= e.StartTicks)
            {
              // chords are not automatically HOPO'd
              chords[diff].IsHopo = false;
            }
            chords[diff].Lanes |= (1 << lane);

            if (gem_tracks[diff].Count > 0
              && 0 != (gem_tracks[diff].Last().Lanes & chords[diff].Lanes)
              && (Hopo.State.ForcedOn != hopoState[diff].state || hopoState[diff].EndTick < e.StartTicks))
            {
              chords[diff].IsHopo = false;
            }
            chords[diff].ProCymbal = (chords[diff].Lanes & 3) != 0 ? 0 : 1;
          }
          else
          { // new chord

            bool hopo = false;
            if(chords[diff] != null)
            {
              if(e.StartTicks - chords[diff].StartTicks <= hopoThreshold && ((1 << lane) & chords[diff].Lanes) == 0)
              {
                if(hopoState[diff].state != Hopo.State.ForcedOff || hopoState[diff].EndTick <= e.StartTicks)
                  hopo = true;
              }
            }
            if (hopoState[diff].state == Hopo.State.ForcedOn && hopoState[diff].EndTick > e.StartTicks)
              hopo = true;
            var chord = new RBMid.GEMTRACK.GEM
            {
              StartMillis = (float)e.StartTime * 1000,
              StartTicks = e.StartTicks,
              LengthMillis = (ushort)(e.Length * 1000),
              LengthTicks = (ushort)e.LengthTicks,
              Lanes = 1 << lane,
              IsHopo = diff > 1 ? hopo : false,
              NoTail = e.LengthTicks <= 120 || (hopo && e.LengthTicks <= 160) || (diff <= 2 && e.LengthTicks <= 160),
              ProCymbal = lane > 1 ? 1 : 0
            };
            chords[diff] = chord;
            gem_tracks[diff].Add(chord);
          }
          return true;
        }
        bool AddHopo(MidiNote e)
        {
          var key = e.Key;
          var diff = 0;
          bool force;
          switch (e.Key)
          {
            case ExpertHopoOff:
              diff = 3;
              hopoState[diff].state = Hopo.State.ForcedOff;
              force = false;
              break;
            case ExpertHopoOn:
              diff = 3;
              force = true;
              hopoState[diff].state = Hopo.State.ForcedOn;
              break;
            case HardHopoOff:
              diff = 2;
              force = false;
              hopoState[diff].state = Hopo.State.ForcedOff;
              break;
            case HardHopoOn:
              diff = 2;
              force = true;
              hopoState[diff].state = Hopo.State.ForcedOn;
              break;
            default:
              return false;
          }
          hopoState[diff].EndTick = e.StartTicks + e.LengthTicks;
          if(chords[diff] != null && chords[diff].StartTicks == e.StartTicks)
          {
            chords[diff].IsHopo = force;
          }
          return true;
        }
        foreach (var item in track.Items
          .OrderBy(e => 127 - (e as MidiNote)?.Key ?? 0)
          .OrderBy(e => e.StartTicks))
        {
          switch (item)
          {
            case MidiNote e:
              if (e.Key == DrumFillMarkerStart)
              {
                fills_unk.Add(new RBMid.DRUMFILLS.FILL_LANES
                {
                  Tick = e.StartTicks,
                  Lanes = 31
                });
                drumfills.Add(new RBMid.DRUMFILLS.FILL
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  IsBRE = 1
                });
              }
              else if (AddGem(e)) { }
              else if (AddHopo(e)) { }
              else if (e.Key == TrillMarker)
              {
                // Remove invalid trill (one with no notes)
                if (trills.Count > 0)
                {
                  var lastTrill = trills.Last();
                  if (lastTrill.FirstFret == -1)
                    trills.RemoveAt(trills.Count - 1);
                  else if (lastTrill.SecondFret == -1)
                    lastTrill.SecondFret = lastTrill.FirstFret;
                }
                trill = new RBMid.GTRTRILLS.TRILL
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  FirstFret = -1,
                  SecondFret = -1
                };
                trills.Add(trill);
              }
              else if (e.Key == TremoloMarker)
              {
                tremolos.Add(new RBMid.LANEMARKER.MARKER
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  Lanes = 0
                });
              }
              else if(e.Key >= LeftHandStart && e.Key <= LeftHandEnd)
              {
                left_hand.Add(new RBMid.HANDPOS
                {
                  StartTime = (float)e.StartTime,
                  Length = (float)e.Length,
                  Position = e.Key - LeftHandStart,
                  // TODO
                  Unknown = 0
                });
              }
              else if (e.Key == OverdriveMarker)
              {
                overdrive_markers.Add(new RBMid.SECTIONS.SECTION
                {
                  StartTicks = e.StartTicks,
                  LengthTicks = e.LengthTicks
                });
              }
              else if (e.Key == SoloMarker)
              {
                solo_markers.Add(new RBMid.SECTIONS.SECTION
                {
                  StartTicks = e.StartTicks,
                  LengthTicks = e.LengthTicks
                });
              }
              break;
            case MidiText e:
              var regex = new System.Text.RegularExpressions.Regex("\\[map (HandMap_[A-Za-z_2]+)\\]");
              var match = regex.Match(e.Text);
              if (match.Success)
              {
                var mapType = HandMaps[match.Groups[1].Value];
                maps.Add(new RBMid.MAP
                {
                  Map = mapType,
                  StartTime = (float)e.StartTime
                });
                break;
              }
              regex = new System.Text.RegularExpressions.Regex("\\[map (StrumMap_[A-Za-z]+)\\]");
              match = regex.Match(e.Text);
              if(match.Success)
              {
                var mapType = StrumMaps[match.Groups[1].Value];
                strummaps.Add(new RBMid.MAP
                {
                  StartTime = (float)e.StartTime,
                  Map = mapType
                });
                break;
              }
              break;
          }
        }

        int Unk = track.Name == "PART BASS" ? 2 : 1;
        Lyrics.Add(new RBMid.LYRICS
        {
          TrackName = track.Name,
          Lyrics = new RBMid.TICKTEXT[0],
          Unknown1 = Unk,
          Unknown2 = Unk,
          Unknown3 = 0
        });
        DrumFills.Add(new RBMid.DRUMFILLS
        {
          Fills = drumfills.ToArray(),
          Lanes = fills_unk.ToArray()
        });
        ProMarkers.Add(new RBMid.TOMMARKER
        {
          Markers = new RBMid.TOMMARKER.MARKER[]
          {
            new RBMid.TOMMARKER.MARKER
            {
              Tick = 0,
              Flags = 0
            }
          }
        });
        LaneMarkers.Add(new RBMid.LANEMARKER
        {
          Markers = tremolos.Count == 0 ? new RBMid.LANEMARKER.MARKER[0][] : new RBMid.LANEMARKER.MARKER[4][]
          {
            new RBMid.LANEMARKER.MARKER[0],
            new RBMid.LANEMARKER.MARKER[0],
            new RBMid.LANEMARKER.MARKER[0],
            tremolos.ToArray()
          }
        });

        TrillMarkers.Add(new RBMid.GTRTRILLS
        {
          Trills = trills.Count == 0 ? new RBMid.GTRTRILLS.TRILL[0][] : new RBMid.GTRTRILLS.TRILL[4][]
          {
            new RBMid.GTRTRILLS.TRILL[0],
            new RBMid.GTRTRILLS.TRILL[0],
            new RBMid.GTRTRILLS.TRILL[0],
            trills.ToArray()
          }
        });
        DrumMixes.Add(new RBMid.DRUMMIXES
        {
          Mixes = new RBMid.TICKTEXT[4][]
        });
        GemTracks.Add(new RBMid.GEMTRACK
        {
          Gems = gem_tracks.Select(g => g.ToArray()).ToArray(),
          HopoThreshold = hopoThreshold
        });
        var sections = new RBMid.SECTIONS.SECTION[6][] {
          overdrive_markers.ToArray(),
          solo_markers.ToArray(),
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0]
        };
        OverdriveSoloSections.Add(new RBMid.SECTIONS
        {
          Sections = new RBMid.SECTIONS.SECTION[4][][]
          {
            sections, sections, sections, sections
          }
        });
        HandMap.Add(maps.ToArray());
        HandPos.Add(left_hand.ToArray());
        strumMaps.Add(strummaps.ToArray());
      }

      private void HandleRealKeysXTrk(MidiTrackProcessed track)
      {
        foreach(var item in track.Items)
        {
          switch (item)
          {

          }
        }

        Lyrics.Add(new RBMid.LYRICS
        {
          TrackName = track.Name,
          Lyrics = new RBMid.TICKTEXT[0],
          Unknown1 = 4,
          Unknown2 = 5,
          Unknown3 = 0
        });
        DrumFills.Add(new RBMid.DRUMFILLS
        {
          Fills = new RBMid.DRUMFILLS.FILL[0],
          Lanes = new RBMid.DRUMFILLS.FILL_LANES[0]
        });
        ProMarkers.Add(new RBMid.TOMMARKER
        {
          Markers = new RBMid.TOMMARKER.MARKER[]
          {
            new RBMid.TOMMARKER.MARKER
            {
              Tick = 0,
              Flags = 0
            }
          }
        });
        LaneMarkers.Add(new RBMid.LANEMARKER
        {
          Markers = new RBMid.LANEMARKER.MARKER[0][]
        });
        TrillMarkers.Add(new RBMid.GTRTRILLS
        {
          Trills = new RBMid.GTRTRILLS.TRILL[0][]
        });
        DrumMixes.Add(new RBMid.DRUMMIXES
        {
          Mixes = new RBMid.TICKTEXT[4][]
        });
        GemTracks.Add(new RBMid.GEMTRACK
        {
          Gems = new RBMid.GEMTRACK.GEM[0][],
          HopoThreshold = hopoThreshold
        });
        OverdriveSoloSections.Add(new RBMid.SECTIONS
        {
          Sections = new RBMid.SECTIONS.SECTION[0][][]
        });
        HandMap.Add(new RBMid.MAP[0]);
        HandPos.Add(new RBMid.HANDPOS[0]);
        strumMaps.Add(new RBMid.MAP[0]);
      }

      private void HandleKeysAnimTrk(MidiTrackProcessed track)
      {
        var anims = new List<RBMid.ANIM.EVENT>();
        foreach(var item in track.Items)
        {
          switch (item)
          {
            case MidiNote e:
              if(e.Key >= 48 && e.Key <= 72)
              {
                anims.Add(new RBMid.ANIM.EVENT
                {
                  StartMillis = (float)e.StartTime * 1000,
                  StartTick = e.StartTicks,
                  KeyBitfield = 1 << (e.Key - 48),
                  LengthTicks = (ushort)(e.LengthTicks),
                  LengthMillis = (ushort)(e.Length * 1000),
                  // TODO: Usually this is 256, or 0, or 65536 (so maybe it is actually 4 bools?)
                  Unknown2 = 256,
                  // TODO
                  Unknown3 = 0
                });
              }
              break;
          }
        }
        Anims.Add(new RBMid.ANIM
        {
          TrackName = track.Name,
          Unknown1 = 1,
          Unknown2 = 120,
          Events = anims.ToArray(),
          Unknown3 = 120
        });
      }

      const byte PhraseMarker = 105;
      const byte Percussion = 96;
      const byte VocalsEnd = 84;
      const byte VocalsStart = 36;
      private void HandleVocalsTrk(MidiTrackProcessed track)
      {
        var lyrics = new List<RBMid.TICKTEXT>();
        var overdrive_markers = new List<RBMid.SECTIONS.SECTION>();
        var percussions = new List<uint>();
        var notes = new List<RBMid.VOCALTRACK.VOCAL_NOTE>();
        var phrase_markers_1 = new List<RBMid.VOCALTRACK.PHRASE_MARKER>();
        var phrase_markers_2 = new List<RBMid.VOCALTRACK.PHRASE_MARKER>();
        var tacets = new List<RBMid.VOCALTRACK.VOCAL_TACET>();
        int phrase_index = 0;
        RBMid.VOCALTRACK.PHRASE_MARKER last_phrase_1 = null;
        RBMid.VOCALTRACK.PHRASE_MARKER last_phrase_2 = null;
        
        bool copyPreviousPhrases = track.Name == "HARM3"
          || (track.Name == "HARM2" && mf.GetTrackByName("HARM3") == null);
        RBMid.VOCALTRACK.PHRASE_MARKER[] last_track_markers = null;
        if (copyPreviousPhrases)
        {
          last_track_markers = VocalTracks.Last().FakePhraseMarkers;
          phrase_markers_1.Add(last_track_markers[0]);
          last_phrase_1 = phrase_markers_1[0];
        }

        // Order the notes by descending key so that bad phrase markers (that start at the same time as a note) are counted
        foreach (var item in track.Items.OrderBy(x => -(x as MidiNote)?.Key ?? 0).OrderBy(x => x.StartTicks))
        {
          if (copyPreviousPhrases)
          {
            while (last_phrase_1.StartTicks + last_phrase_1.LengthTicks < item.StartTicks)
            {
              if (last_track_markers.Length - 1 < phrase_markers_1.Count) break;
              phrase_markers_1.Add(last_track_markers[phrase_markers_1.Count]);
              last_phrase_1 = phrase_markers_1.Last();
              if (notes.Count > 0)
              {
                var lastNoteInPhrase = notes.Last();
                lastNoteInPhrase.LastNoteInPhrase = true;
                notes[notes.Count - 1] = lastNoteInPhrase;
              }
            }
          }
          switch (item)
          {
            case MidiNote e:
              if (e.Key == OverdriveMarker)
              {
                overdrive_markers.Add(new RBMid.SECTIONS.SECTION
                {
                  StartTicks = e.StartTicks,
                  LengthTicks = e.LengthTicks
                });
                if(last_phrase_1?.StartTicks == e.StartTicks)
                {
                  last_phrase_1.IsOverdrive = 1;
                }
              }
              else if (e.Key == Percussion)
              {
                percussions.Add(e.StartTicks);
              }
              else if (e.Key == PhraseMarker)
              {
                // TODO This is mostly wrong.
                if(phrase_markers_1.Count == 0)
                {
                  phrase_markers_1.Add(new RBMid.VOCALTRACK.PHRASE_MARKER
                  {
                    StartMillis = 0,
                    Length = (float)e.StartTime * 1000,
                    StartTicks = 0U,
                    LengthTicks = e.StartTicks,
                    StartNoteIdx = -1,
                    EndNoteIdx = -1,
                    IsPhrase = 0,
                    IsOverdrive = 0,
                    LowNote = theVocalRange.LowNote,
                    HighNote = theVocalRange.HighNote
                  });
                } else if(last_phrase_1 != null)
                {
                  last_phrase_1.EndNoteIdx = notes.Count;
                  if (notes.Count > 0)
                  {
                    var lastNoteInPhrase = notes.Last();
                    lastNoteInPhrase.LastNoteInPhrase = true;
                    notes[notes.Count - 1] = lastNoteInPhrase;
                  }
                }
                last_phrase_1 = new RBMid.VOCALTRACK.PHRASE_MARKER
                {
                  StartMillis = (float)e.StartTime * 1000,
                  StartTicks = e.StartTicks,
                  Length = (float)e.Length * 1000,
                  LengthTicks = e.LengthTicks,
                  StartNoteIdx = notes.Count,
                  IsPhrase = 1,
                  IsOverdrive = 0,
                  LowNote = float.MaxValue,
                  HighNote = float.MinValue
                };
                if(overdrive_markers.LastOrDefault().StartTicks == e.StartTicks)
                {
                  last_phrase_1.IsOverdrive = 1;
                }
                phrase_markers_1.Add(last_phrase_1);
                last_phrase_2 = new RBMid.VOCALTRACK.PHRASE_MARKER
                {
                  StartMillis = 0f,
                  Length = 0f,
                  StartTicks = e.StartTicks,
                  LengthTicks = e.LengthTicks,
                  StartNoteIdx = notes.Count,
                  IsPhrase = 0,
                  IsOverdrive = 0,
                  LowNote = float.MaxValue,
                  HighNote = float.MinValue
                };
                phrase_markers_2.Add(last_phrase_2);
              }
              else if (e.Key >= VocalsStart && e.Key <= VocalsEnd)
              {
                var lyric = (track.Items.Find(s => s is MidiText && s.StartTicks == e.StartTicks) as MidiText).Text;
                var lyricCleaned = lyric.Replace("$", "").Replace("#", "").Replace("^", "");
                if(lyricCleaned == "+")
                {
                  var previous = notes.LastOrDefault();
                  notes.Add(new RBMid.VOCALTRACK.VOCAL_NOTE
                  {
                    PhraseIndex = phrase_markers_1.Count - 1,
                    MidiNote = previous.MidiNote2,
                    MidiNote2 = e.Key,
                    StartMillis = previous.StartMillis + previous.LengthMillis,
                    StartTick = previous.StartTick + previous.LengthTicks,
                    LengthMillis = ((float)e.StartTime * 1000) - (previous.StartMillis + previous.LengthMillis),
                    LengthTicks = (ushort)(e.StartTicks - (previous.StartTick + previous.LengthTicks)),
                    Lyric = "",
                    LastNoteInPhrase = false,
                    // TODO: What is this?
                    Unknown = 1,
                    Portamento = true,
                    Flag9 = true
                  }); 
                }
                var last = notes.LastOrDefault();
                var lastNoteEnd = last.StartMillis + last.LengthMillis;
                var tacet = ((float)e.StartTime * 1000) - lastNoteEnd;
                if(tacet > 600f)
                {
                  float transition = (tacet > 800f ? 100f : 50f);
                  tacets.Add(new RBMid.VOCALTRACK.VOCAL_TACET
                  {
                    StartMillis = lastNoteEnd + transition,
                    EndMillis = (float)e.StartTime * 1000 - transition
                  });
                }
                notes.Add(new RBMid.VOCALTRACK.VOCAL_NOTE
                {
                  PhraseIndex = phrase_markers_1.Count - 1,
                  MidiNote = e.Key,
                  MidiNote2 = e.Key,
                  StartMillis = (float)e.StartTime * 1000,
                  StartTick = e.StartTicks,
                  LengthMillis = (float)e.Length * 1000,
                  LengthTicks = (ushort)e.LengthTicks,
                  Lyric = lyricCleaned == "+" ? "" : lyricCleaned,
                  // TODO
                  LastNoteInPhrase = false,
                  UnknownFalse = false,
                  Unpitched = lyric.Contains("#") || lyric.Contains("^"),
                  UnknownFalse2 = false,
                  UnkFlag1 = false,
                  // TODO: What is this?
                  Unknown = 1,
                  Portamento = lyricCleaned == "+",
                  Flag8 = false,
                  // TODO: What is this?
                  Flag9 = true
                });
                if (theVocalRange.HighNote < e.Key) theVocalRange.HighNote = e.Key;
                if (theVocalRange.LowNote > e.Key) theVocalRange.LowNote = e.Key;
              }
              break;
            case MidiText e:
              if (e.Text[0] != '[')
              {
                lyrics.Add(new RBMid.TICKTEXT
                {
                  Text = e.Text.Trim(' '),
                  Tick = e.StartTicks,
                });
              }
              break;
          }
        }
        var lastNote = notes.Last();
        var lastTempo = mf.TempoTimeSigMap.Last();
        var lastMeasure = MeasureTicks.Last() + (480U * lastTempo.Numerator * 4 / lastTempo.Denominator);
        var lastTime = lastTempo.Time + ((lastMeasure - lastTempo.Tick) / 480.0) * (60 / lastTempo.BPM);
        tacets.Add(new RBMid.VOCALTRACK.VOCAL_TACET
        {
          StartMillis = lastNote.StartMillis + lastNote.LengthMillis + 100f,
          EndMillis = (float)lastTime * 1000,
        });
        lastNote.LastNoteInPhrase = true;
        notes[notes.Count - 1] = lastNote;


        Lyrics.Add(new RBMid.LYRICS
        {
          TrackName = track.Name,
          Lyrics = lyrics.ToArray(),
          Unknown1 = 3,
          Unknown2 = 3,
          Unknown3 = 0
        });
        DrumFills.Add(new RBMid.DRUMFILLS());
        ProMarkers.Add(new RBMid.TOMMARKER
        {
          Markers = new RBMid.TOMMARKER.MARKER[]
          {
            new RBMid.TOMMARKER.MARKER
            {
              Tick = 0,
              Flags = 0
            }
          }
        });
        LaneMarkers.Add(new RBMid.LANEMARKER());
        TrillMarkers.Add(new RBMid.GTRTRILLS());
        var emptyMixes = new RBMid.TICKTEXT[0];
        DrumMixes.Add(new RBMid.DRUMMIXES
        {
          Mixes = new RBMid.TICKTEXT[4][] { emptyMixes, emptyMixes, emptyMixes, emptyMixes }
        });
        var emptyGems = new RBMid.GEMTRACK.GEM[0];
        GemTracks.Add(new RBMid.GEMTRACK
        {
          Gems = new RBMid.GEMTRACK.GEM[4][] { emptyGems, emptyGems, emptyGems, emptyGems },
          HopoThreshold = hopoThreshold
        });
        var overdriveSections = new RBMid.SECTIONS.SECTION[6][]
        {
          overdrive_markers.ToArray(),
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0],
          new RBMid.SECTIONS.SECTION[0]
        };
        OverdriveSoloSections.Add(new RBMid.SECTIONS
        {
          Sections = new RBMid.SECTIONS.SECTION[4][][]
          {
            overdriveSections, overdriveSections, overdriveSections, overdriveSections
          }
        });
        // hack: copy data from HARM2 into HARM3
        if(copyPreviousPhrases)
        {
          VocalTracks.Add(new RBMid.VOCALTRACK
          {
            FakePhraseMarkers = VocalTracks.Last().FakePhraseMarkers,
            AuthoredPhraseMarkers = new RBMid.VOCALTRACK.PHRASE_MARKER[0],
            Notes = notes.ToArray(),
            Percussion = percussions.ToArray(),
            Tacets = VocalTracks.Last().Tacets
          });
        }
        else
        {
          VocalTracks.Add(new RBMid.VOCALTRACK
          {
            FakePhraseMarkers = phrase_markers_1.ToArray(),
            AuthoredPhraseMarkers = phrase_markers_2.ToArray(),
            Notes = notes.ToArray(),
            Percussion = percussions.ToArray(),
            Tacets = tacets.ToArray()
          });
        }
        HandMap.Add(new RBMid.MAP[0]);
        HandPos.Add(new RBMid.HANDPOS[0]);
        strumMaps.Add(new RBMid.MAP[0]);
      }

      private void HandleEventsTrk(MidiTrackProcessed track)
      {
        foreach(var item in track.Items)
        {
          var timeMillis = (float)(item.StartTime * 1000);
          switch (item)
          {
            case MidiText e:
              switch (e.Text)
              {
                case "[preview_start]":
                  PreviewStart = timeMillis;
                  break;
                case "[preview_end]":
                  PreviewEnd = timeMillis;
                  break;
                case "[preview]":
                  PreviewStart = timeMillis;
                  PreviewEnd = PreviewStart + 30_000;
                  break;
                case "[coda]":
                  // TODO: This would be better in the Drum track code,
                  // but we don't know if it's a BRE there because the lanes are the same for normal fills
                  var idx = DrumFills[0].Fills.Length - 1;
                  var lastDrumFill = DrumFills[0].Fills[idx];
                  lastDrumFill.IsBRE = 1;
                  DrumFills[0].Fills[idx] = lastDrumFill;
                  break;
              }
              break;
          }
        }
      }

      private void HandleBeatTrk(MidiTrackProcessed track)
      {
        foreach(var item in track.Items)
        {
          switch (item)
          {
            case MidiNote e:
              switch (e.Key)
              {
                case 12:
                case 13:
                  Beats.Add(new RBMid.BEAT
                  {
                    Tick = e.StartTicks,
                    Downbeat = e.Key == 12
                  });
                  break;
              }
              break;
          }
        }
      }

      const byte MarkupNotes2 = 127;
      const byte MarkupChordsEnd = 64;
      const byte MarkupChordsStart = 36;
      const byte MarkupNotes3End = 23;
      const byte MarkupNotes3Start = 12;
      const byte MarkupNotes1End = 11;
      const byte MarkupNotes1Start = 0;
      private void HandleMarkupTrk(MidiTrackProcessed track)
      {
        LastMarkupTick = track.LastTick;
        RBMid.MARKUPCHORD last_chord = null;
        var pitches = new SortedSet<int>();
        foreach(var item in track.Items)
        {
          switch (item)
          {
            case MidiNote e:
              if(e.Key >= MarkupChordsStart && e.Key <= MarkupChordsEnd)
              {
                if(last_chord?.StartTick == e.StartTicks)
                {
                  pitches.Add(e.Key % 12);
                  last_chord.Pitches = pitches.ToArray();
                }
                else
                {
                  if(last_chord != null)
                  {
                    last_chord.EndTick = e.StartTicks;
                  }
                  last_chord = new RBMid.MARKUPCHORD
                  {
                    StartTick = e.StartTicks,
                    EndTick = 2147483647,
                    Pitches = new[] { e.Key % 12 }
                  };
                  MarkupChords1.Add(last_chord);
                  pitches.Clear();
                  pitches.Add(e.Key % 12);
                }
              }
              else if(e.Key >= MarkupNotes1Start && e.Key <= MarkupNotes1End)
              {
                MarkupSoloNotes1.Add(new RBMid.MARKUP_SOLO_NOTES
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  NoteOffset = e.Key - MarkupNotes1Start
                });
              }
              else if(e.Key >= MarkupNotes3Start && e.Key <= MarkupNotes3End)
              {
                MarkupSoloNotes3.Add(new RBMid.MARKUP_SOLO_NOTES
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  NoteOffset = e.Key - MarkupNotes3Start
                });
              }
              else if(e.Key == MarkupNotes2)
              {
                MarkupSoloNotes2.Add(new RBMid.MARKUP_SOLO_NOTES
                {
                  StartTick = e.StartTicks,
                  EndTick = e.StartTicks + e.LengthTicks,
                  NoteOffset = e.Velocity
                });
              }
              break;
            case MidiText e:
              var regex = new System.Text.RegularExpressions.Regex("\\[sololoop ([0-9]+)\\]");
              var match = regex.Match(e.Text);
              if (match.Success)
              {
                var loop_measures =int.Parse(match.Groups[1].Value);
                var loop_end = MeasureTicks[loop_measures - 1];
                SoloLoops1.Add(new RBMid.TWOTICKS
                {
                  StartTick = e.StartTicks,
                  EndTick = loop_end
                });
                SoloLoops2.Add(new RBMid.TWOTICKS
                {
                  StartTick = e.StartTicks,
                  EndTick = loop_end
                });
              }
              break;
          }
        }
      }

      private void HandleVenueTrk(MidiTrackProcessed track)
      {
        foreach (var item in track.Items)
        {
          switch (item)
          {

          }
        }
      }
    }
  }
}
