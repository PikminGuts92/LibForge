﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DtxCS;
using DtxCS.DataTypes;
using GameArchives.STFS;
using LibForge.Lipsync;
using LibForge.Midi;
using LibForge.Milo;
using LibForge.RBSong;
using LibForge.SongData;

namespace LibForge.Util
{
  public static class PkgCreator
  {
    static byte[] paramSFO = {
      0x00, 0x50, 0x53, 0x46, 0x01, 0x01, 0x00, 0x00, 0x84, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00,
      0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x04, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x04, 0x02, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
      0x04, 0x00, 0x00, 0x00, 0x13, 0x00, 0x04, 0x02, 0x25, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00,
      0x08, 0x00, 0x00, 0x00, 0x1E, 0x00, 0x04, 0x02, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
      0x38, 0x00, 0x00, 0x00, 0x25, 0x00, 0x04, 0x02, 0x1D, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
      0x3C, 0x00, 0x00, 0x00, 0x2B, 0x00, 0x04, 0x02, 0x0A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00,
      0xBC, 0x00, 0x00, 0x00, 0x34, 0x00, 0x04, 0x02, 0x06, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00,
      0xC8, 0x00, 0x00, 0x00, 0x41, 0x54, 0x54, 0x52, 0x49, 0x42, 0x55, 0x54, 0x45, 0x00, 0x43, 0x41,
      0x54, 0x45, 0x47, 0x4F, 0x52, 0x59, 0x00, 0x43, 0x4F, 0x4E, 0x54, 0x45, 0x4E, 0x54, 0x5F, 0x49,
      0x44, 0x00, 0x46, 0x4F, 0x52, 0x4D, 0x41, 0x54, 0x00, 0x54, 0x49, 0x54, 0x4C, 0x45, 0x00, 0x54,
      0x49, 0x54, 0x4C, 0x45, 0x5F, 0x49, 0x44, 0x00, 0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x61, 0x63, 0x00, 0x00, 0x55, 0x50, 0x38, 0x38, 0x30, 0x32, 0x2D, 0x43,
      0x55, 0x53, 0x41, 0x30, 0x32, 0x30, 0x38, 0x34, 0x5F, 0x30, 0x30, 0x2D, 0x52, 0x42, 0x43, 0x55,
      0x53, 0x54, 0x4F, 0x4D, 0x58, 0x58, 0x58, 0x58, 0x35, 0x30, 0x30, 0x30, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6F, 0x62, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x43, 0x55, 0x53, 0x41,
      0x30, 0x32, 0x30, 0x38, 0x34, 0x00, 0x00, 0x00, 0x30, 0x31, 0x2E, 0x30, 0x30, 0x00, 0x00, 0x00
    };
    static string gp4 = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<psproject fmt=""gp4"" version=""1000"">
  <volume>
    <volume_type>pkg_ps4_ac_data</volume_type>
    <volume_id></volume_id>
    <volume_ts>2018-01-01 00:00:00</volume_ts>
    <package content_id=""UP8802-CUSA02084_00-RBCUSTOMXXXX5000"" passcode=""00000000000000000000000000000000""/>
  </volume>
  <files img_no=""0"">
    <file targ_path=""sce_sys/param.sfo"" orig_path=""param.sfo""/>
FILES  </files>
  <rootdir>
    <dir targ_name=""sce_sys""/>
    <dir targ_name=""songs"">
SHORTNAMES
    </dir>
  </rootdir>
</psproject>";

    public static byte[] MakeParamSfo(string pkgId, string description)
    {
      var idBytes = Encoding.UTF8.GetBytes(pkgId);
      if (idBytes.Length != 36) throw new Exception("Content ID is not formatted correctly. It should be 36 characters");
      var descBytes = Encoding.UTF8.GetBytes(description);
      var param = paramSFO.ToArray();
      Array.Copy(idBytes, 0, param, 200, 36);
      Array.Copy(descBytes, 0, param, 252, Math.Min(descBytes.Length, 128));
      Array.Copy(idBytes, 7, param, 380, 9); // overwrite SKU
      return param;
    }

    public static byte[] MakeGp4(string pkgId, IList<string> shortnames, List<string> files)
    {
      var fileSb = new StringBuilder();
      foreach(var f in files)
      {
        fileSb.AppendLine($"    <file targ_path=\"{f}\" orig_path=\"{f.Replace('/', '\\')}\"/>");
      }
      var shortname_dirs = new StringBuilder();
      foreach(var shortname in shortnames)
      {
        shortname_dirs.AppendLine($"      <dir targ_name=\"{shortname}\"/>");
      }
      var project = gp4.Replace("UP8802-CUSA02084_00-RBCUSTOMXXXX5000", pkgId)
                       .Replace("SHORTNAMES", shortname_dirs.ToString())
                       .Replace("FILES", fileSb.ToString())
                       .Replace("2018-01-01 00:00:00", DateTime.UtcNow.ToString("s").Replace('T', ' '));
      return Encoding.UTF8.GetBytes(project);
    }

    public static DataArray MakeMoggDta(DataArray array)
    {
      var moggDta = new DataArray();
      var trackArray = new DataArray();
      trackArray.AddNode(DataSymbol.Symbol("tracks"));
      var trackSubArray = trackArray.AddNode(new DataArray());
      foreach (var child in array.Array("song").Array("tracks").Array(1).Children)
      {
        trackSubArray.AddNode(child);
      }
      var totalTracks = array.Array("song").Array("pans").Array(1).Children.Count;
      var lastTrack = ((trackSubArray.Children.Last() as DataArray)
        .Array(1).Children.Last() as DataAtom).Int;
      var crowdChannel = array.Array("song").Array("crowd_channels")?.Int(1);
      if (crowdChannel != null)
      {
        if (crowdChannel == lastTrack + 2)
          trackSubArray.AddNode(DTX.FromDtaString($"fake ({lastTrack + 1})"));
        else if (crowdChannel == lastTrack + 3)
          trackSubArray.AddNode(DTX.FromDtaString($"fake ({lastTrack + 1} {lastTrack + 2})"));
        trackSubArray.AddNode(DTX.FromDtaString($"crowd ({crowdChannel} {crowdChannel + 1})"));
      }
      else
      {
        if (totalTracks == lastTrack + 2)
          trackSubArray.AddNode(DTX.FromDtaString($"fake ({lastTrack + 1})"));
        else if (totalTracks == lastTrack + 3)
          trackSubArray.AddNode(DTX.FromDtaString($"fake ({lastTrack + 1} {lastTrack + 2})"));
      }
      moggDta.AddNode(trackArray);
      moggDta.AddNode(array.Array("song").Array("pans"));
      moggDta.AddNode(array.Array("song").Array("vols"));
      return moggDta;
    }
    
    // TODO: RBSONG
    public static RBSong.RBSong MakeRBSong(DataArray array)
    {
      var drumBank = array.Array("drum_bank")?.Any(1)
        .Replace("sfx", "fusion/patches")
        .Replace("_bank.milo", ".fusion") 
        ?? "fusion/patches/kit01.fusion";
      var editorComponent = new Component
      {
        ClassName = "Editor",
        Name = "Editor",
        Unknown1 = 3,
        Unknown2 = 2,
        Props = new[]
        {
          new Property("capabilities", new FlagValue(50))
        }
      };
      var vec3 = StructType.FromData(DTX.FromDtaString("define vec3 (props (x float) (y float) (z float))"));
      var xfm_type = StructType.FromData(DTX.FromDtaString(
        @"define xfm
            (props
              (basis_x vec3)
              (basis_y vec3)
              (basis_z vec3)
              (translate vec3))"));
      var entityHeaderComponent = new Component
      {
        ClassName = "EntityHeader",
        Name = "EntityHeader",
        Unknown1 = 3,
        Unknown2 = 1,
        Props = new[]
        {
          new Property("copy_on_instance", new BoolValue(true)),
          new Property("drives_parent", new BoolValue(false)),
          new Property("static", new BoolValue(false)),
          new Property("instance_polling_mode", new IntValue(0)),
          new Property("num_instances", new IntValue(0)),
          new Property("num_meshes", new IntValue(0)),
          new Property("num_particles", new IntValue(0)),
          new Property("num_propanims", new IntValue(0)),
          new Property("num_lights", new IntValue(0)),
          new Property("num_verts", new IntValue(0)),
          new Property("num_faces", new IntValue(0)),
          new Property("changelist", new IntValue(0)),
          new Property("icon_cam_initialized", new BoolValue(false)),
          new Property("icon_cam_near", new FloatValue(0.1f)),
          new Property("icon_cam_far", new FloatValue(1000f)),
          new Property("icon_cam_xfm", StructValue.FromData(xfm_type, DTX.FromDtaString(
            @"(basis_x ((x 1.0) (y 0.0) (z 0.0)))
              (basis_y ((x 0.0) (y 1.0) (z 0.0)))
              (basis_z ((x 0.0) (y 0.0) (z 1.0)))
              (translate ((x 0.0) (y 0.0) (z 0.0)))"))),
          new Property("icon_data", 
            new ArrayValue(new ArrayType{ElementType = PrimitiveType.Byte, InternalType = RBSong.DataType.Uint8 | RBSong.DataType.Array }, new Value[] { }))
        }
      };
      return new RBSong.RBSong
      {
        Version = 0xE,
        Object1 = new ObjectContainer
        {
          Unknown1 = 20,
          Unknown2 = 1,
          Unknown3 = 20,
          Unknown4 = 0,
          Unknown5 = 1,
          Entities = new[] {
            new Entity
            {
              Index0 = 0,
              Index1 = 0,
              Name = "root",
              Coms = new Component[] {
                editorComponent,
                entityHeaderComponent,
                new Component
                {
                  ClassName = "RBSongMetadata",
                  Name = "RBSongMetadata",
                  Unknown1 = 3,
                  Unknown2 = 4,
                  Props = new[]
                  {
                    new Property("tempo", new SymbolValue("medium")),
                    new Property("vocal_tonic_note", new LongValue(array.Array("vocal_tonic_note")?.Int(1) ?? 0)),
                    new Property("vocal_track_scroll_duration_ms", new LongValue(array.Array("song_scroll_speed")?.Int(1) ?? 2300)),
                    new Property("global_tuning_offset", new FloatValue(array.Array("tuning_offset_cents")?.Int(1) ?? 0)),
                    new Property("band_fail_sound_event", new SymbolValue("", true)),
                    new Property("vocal_percussion_patch", new ResourcePathValue("fusion/patches/vox_perc_tambourine.fusion", true)),
                    new Property("drum_kit_patch", new ResourcePathValue(drumBank)),
                    new Property("improv_solo_patch", new SymbolValue("gtrsolo_amer_03")),
                    new Property("dynamic_drum_fill_override", new IntValue(10)),
                    new Property("improv_solo_volume_db", new FloatValue(-9))
                  }
                },
                new Component
                {
                  ClassName = "RBVenueAuthoring",
                  Name = "RBVenueAuthoring",
                  Unknown1 = 3,
                  Unknown2 = 0,
                  Props = new[]
                  {
                    new Property("part2_instrument", new IntValue(2)),
                    new Property("part3_instrument", new IntValue(0)),
                    new Property("part4_instrument", new IntValue(1))
                  }
                }
              }
            }
          }
        },
        KV = new KeyValue
        {
          Str1 = "PropAnimResource",
          Str2 = "venue_authoring_data"
        },
        Object2 = new ObjectContainer
        {
          Unknown1 = 20,
          Unknown2 = 1,
          Unknown3 = 20,
          Unknown4 = 0,
          Unknown5 = 1,
          Entities = new[] {
            new Entity
            {
              Index0 = 0,
              Index1 = 0,
              Name = "root",
              Coms = new[]
              {
                editorComponent,
                entityHeaderComponent,
                new Component
                {
                  ClassName = "PropAnim",
                  Name = "PropAnim",
                  Unknown1 = 3,
                  Unknown2 = 0,
                  Props = StructValue.FromData(
                            StructType.FromData(DTX.FromDtaString(
                              @"(props 
                                  (frame_range_start float)
                                  (frame_range_end float)
                                  (time_units int)
                                  (is_looping bool))")),
                            DTX.FromDtaString(
                              @"(frame_range_start 3.402823E+38)
                                (frame_range_end -3.402823E+38)
                                (time_units 0)
                                (is_looping 0)")).Props

                }
              }
            }
          }
        }
      };
    }

    public static DLCSong ConvertDLCSong(DataArray songDta, GameArchives.IDirectory songRoot)
    {
      var path = songDta.Array("song").Array("name").String(1);
      var hopoThreshold = songDta.Array("song").Array("hopo_threshold")?.Int(1) ?? 170;
      var shortname = path.Split('/').Last();
      var midPath = shortname + ".mid";
      var artPath = $"gen/{shortname}_keep.png_xbox";
      var miloPath = $"gen/{shortname}.milo_xbox";
      var songId = songDta.Array("song_id").Node(1);
      var name = songDta.Array("name").String(1);
      var artist = songDta.Array("artist").String(1);
      var mid = MidiCS.MidiFileReader.FromBytes(songRoot.GetFileAtPath(midPath).GetBytes());

      // TODO: Catch possible conversion exceptions? i.e. Unsupported milo version
      var milo = MiloFile.ReadFromStream(songRoot.GetFileAtPath(miloPath).GetStream());
      var songData = SongDataConverter.ToSongData(songDta);

      Texture.Texture artwork = null;
      if (songData.AlbumArt)
      {
        artwork = Texture.TextureConverter.MiloPngToTexture(songRoot.GetFileAtPath(artPath).GetStream());
      }
      return new DLCSong
      {
        SongData = songData,
        Lipsync = LipsyncConverter.FromMilo(milo),
        Mogg = songRoot.GetFile(shortname + ".mogg"),
        MoggDta = MakeMoggDta(songDta),
        MoggSong = DTX.FromDtaString($"(mogg_path \"{shortname}.mogg\")\r\n(midi_path \"{shortname}.rbmid\")\r\n"),
        RBMidi = RBMidConverter.ToRBMid(mid, hopoThreshold),
        Artwork = artwork,
        RBSong = MakeRBSong(songDta)
      };
    }

    /// <summary>
    /// Converts an RB3 DLC songs folder into RB4 DLC songs
    /// </summary>
    /// <param name="dlcRoot"></param>
    /// <returns></returns>
    public static List<DLCSong> ConvertDLCPackage(GameArchives.IDirectory dlcRoot)
    {
      var dlcSongs = new List<DLCSong>();
      var dta = DTX.FromPlainTextBytes(dlcRoot.GetFile("songs.dta").GetBytes());
      DataArray arr;
      for(int i = 0; i < dta.Count; i++)
      {
        arr = dta.Array(i);
        dlcSongs.Add(ConvertDLCSong(arr, dlcRoot.GetDirectory(arr.Array("song").Array("name").String(1).Split('/').Last())));
      }
      return dlcSongs;
    }

    /// <summary>
    /// Writes the DLCSong to disk within the given directory.
    /// For example given a song called "custom" and a directory called J:\customs,
    /// you'll end up with J:\customs\custom\custom.mogg, J:\customs\custom\custom.rbsong, etc
    /// </summary>
    /// <param name="song">The song to write</param>
    /// <param name="dir">The parent directory of the song directory</param>
    public static void WriteDLCSong(DLCSong song, string dir)
    {
      var shortname = song.SongData.Shortname;
      var songPath = Path.Combine(dir, "songs", shortname);
      Directory.CreateDirectory(songPath);
      using (var lipsyncFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.lipsync_ps4")))
      {
        new LipsyncWriter(lipsyncFile).WriteStream(song.Lipsync);
      }
      using (var mogg = File.OpenWrite(Path.Combine(songPath, $"{shortname}.mogg")))
      using (var conMogg = song.Mogg.GetStream())
      {
        conMogg.CopyTo(mogg);
      }
      File.WriteAllText(Path.Combine(songPath, $"{shortname}.mogg.dta"), song.MoggDta.ToFileString());
      File.WriteAllText(Path.Combine(songPath, shortname + ".moggsong"), song.MoggSong.ToFileString());
      using (var rbmid = File.OpenWrite(Path.Combine(songPath, $"{shortname}.rbmid_ps4")))
        RBMidWriter.WriteStream(song.RBMidi, rbmid);
      using (var rbsongFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.rbsong")))
        new RBSongWriter(rbsongFile).WriteStream(song.RBSong);
      using (var songdtaFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.songdta_ps4")))
        SongDataWriter.WriteStream(song.SongData, songdtaFile);
      if (song.SongData.AlbumArt)
      {
        using (var artFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.png_ps4")))
          Texture.TextureWriter.WriteStream(song.Artwork, artFile);
      }
    }

    /// <summary>
    /// Writes all the songs and creates a Publishing Tools .gp4 project in the given directory
    /// </summary>
    /// <param name="songs">Songs to include</param>
    /// <param name="pkgId">36-character package ID</param>
    /// <param name="pkgDesc">User-visible name of the package</param>
    /// <param name="buildDir">Directory in which to put the project and files</param>
    public static void DLCSongsToGP4(IList<DLCSong> songs, string pkgId, string pkgDesc, string buildDir)
    {
      var shortnames = new List<string>(songs.Count);
      var files = new List<string>(songs.Count * 8);
      foreach (var song in songs)
      {
        var shortname = song.SongData.Shortname;
        files.AddRange(new[] {
          $"songs/{shortname}/{shortname}.lipsync_ps4",
          $"songs/{shortname}/{shortname}.mogg",
          $"songs/{shortname}/{shortname}.mogg.dta",
          $"songs/{shortname}/{shortname}.moggsong",
          $"songs/{shortname}/{shortname}.rbmid_ps4",
          $"songs/{shortname}/{shortname}.rbsong",
          $"songs/{shortname}/{shortname}.songdta_ps4",
        });
        if (song.Artwork != null)
            files.Add($"songs/{shortname}/{shortname}.png_ps4");
        shortnames.Add(shortname);
      }
      
      var paramSfo = MakeParamSfo(pkgId, pkgDesc);

      // Write all the files
      foreach(var song in songs)
      {
        WriteDLCSong(song, buildDir);
      }
      File.WriteAllBytes(Path.Combine(buildDir, "param.sfo"), paramSfo);
      File.WriteAllBytes(Path.Combine(buildDir, "project.gp4"), MakeGp4(pkgId, shortnames, files));
    }

    /// <summary>
    /// Does the whole process of converting a CON to a GP4 project
    /// </summary>
    /// <param name="conPath">Path to CON file</param>
    /// <param name="buildDir">Output directory for project and files</param>
    /// <param name="eu">If true then an SCEE project is made (otherwise, SCEA)</param>
    public static void ConToGp4(string conPath, string buildDir, bool eu = false, string id = null, string desc = null)
    {
      // Phase 1: Reading from CON
      var con = STFSPackage.OpenFile(GameArchives.Util.LocalFile(conPath));
      if(con.Type != STFSType.CON)
      {
        Console.WriteLine("Error: given file was not a CON file");
        return;
      }
      var songs = ConvertDLCPackage(con.RootDirectory.GetDirectory("songs"));
      if(songs.Count > 1)
      {
        if ((id?.Length ?? 0) < 16)
        {
          throw new Exception("You must provide a 16 char ID if you are building a custom package with multiple songs");
        }
      }
      var shortname = songs[0].SongData.Shortname;
      var pkgName = new Regex("[^a-zA-Z0-9]").Replace(shortname, "")
        .ToUpper().Substring(0, Math.Min(shortname.Length, 10)).PadRight(10, 'X');
      string pkgNum = (songs[0].SongData.SongId % 10000).ToString().PadLeft(4, '0');
      var identifier = id ?? ("RB" + pkgName + pkgNum);
      var pkgId = eu ? $"EP8802-CUSA02901_00-{identifier}" : $"UP8802-CUSA02084_00-{identifier}";
      var pkgDesc = $"Custom: \"{songs[0].SongData.Name} - {songs[0].SongData.Artist}\"";
      DLCSongsToGP4(songs, pkgId, desc ?? pkgDesc, buildDir);
    }

    public static void ConsToGp4(string conPath, string buildDir, bool eu, string id, string desc)
    {
      var songs = new List<DLCSong>();
      foreach (var conFilename in Directory.EnumerateFiles(conPath))
      {
        var file = GameArchives.Util.LocalFile(conFilename);
        var stfs = STFSPackage.IsSTFS(file);
        STFSPackage conFile;
        if (stfs != GameArchives.PackageTestResult.YES
          || null == (conFile = STFSPackage.OpenFile(file))
          || conFile.Type != STFSType.CON)
        {
          Console.WriteLine($"Skipping \"{conFilename}\": not a CON file");
          continue;
        }
        songs.AddRange(ConvertDLCPackage(conFile.RootDirectory.GetDirectory("songs")));
      }

      if (songs.Count > 1)
      {
        if ((id?.Length ?? 0) < 16)
        {
          throw new Exception("You must provide a 16 char ID if you are building a custom package with multiple songs");
        }
      }
      var pkgId = eu ? $"EP8802-CUSA02901_00-{id}" : $"UP8802-CUSA02084_00-{id}";
      DLCSongsToGP4(songs, pkgId, desc ?? "", buildDir);
    }

    public static void BuildPkg(string cmdExe, string proj, string outPath)
    {
      outPath = outPath.Replace('\\', '/');
      if (outPath[outPath.Length - 1] == '/')
      {
        outPath = outPath.Substring(0, outPath.Length - 1);
      }
      var p = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          UseShellExecute = false,
          FileName = cmdExe,
          Arguments = $"img_create --oformat pkg \"{proj}\" \"{outPath}\"",
          RedirectStandardOutput = true
        },
      };
      p.Start();
      p.WaitForExit();
      Console.Write(p.StandardOutput.ReadToEnd());
    }
  }

  public static class DataArrayExtension
  {
    /// <summary>
    /// Renders a DataArray that represents a DTA file.
    /// Basically, DataArray to string without parens.
    /// </summary>
    public static string ToFileString(this DataArray d)
    {
      var ret = d.ToString();
      return ret.Substring(1, ret.Length - 2);
    }
  }
}
