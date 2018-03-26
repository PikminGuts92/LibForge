﻿using System;
using System.Diagnostics;
using System.IO;
using LibForge.Midi;

namespace ForgeTool
{
  class Program
  {
    static void Main(string[] args)
    {
      if(args.Length < 1)
      {
        Usage();
        return;
      }
      switch (args[0])
      {
        case "rbmid2mid":
          {
            var input = args[1];
            var output = args[2];
            using (var fi = File.OpenRead(input))
            using (var fo = File.OpenWrite(output))
            {
              var rbmid = RBMidReader.ReadStream(fi);
              var midi = RBMidConverter.ToMid(rbmid);
              MidiCS.MidiFileWriter.WriteSMF(midi, fo);
            }
          }
          break;
        case "mid2rbmid":
          {
            var input = args[1];
            var output = args[2];
            using (var fi = File.OpenRead(input))
            using (var fo = File.OpenWrite(output))
            {
              var mid = MidiCS.MidiFileReader.FromStream(fi);
              var rbmid = RBMidConverter.ToRBMid(mid);
              RBMidWriter.WriteStream(rbmid, fo);
            }
          }
          break;
        case "reprocess":
          {
            var input = args[1];
            var output = args[2];
            using (var fi = File.OpenRead(input))
            using (var fo = File.OpenWrite(output))
            {
              var rbmid = RBMidReader.ReadStream(fi);
              RBMidWriter.WriteStream(rbmid, fo);
            }
          }
          break;
        case "version":
          var assembly = System.Reflection.Assembly.GetExecutingAssembly();
          var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
          var libAssembly = System.Reflection.Assembly.GetAssembly(typeof(RBMid));
          var libVersion = FileVersionInfo.GetVersionInfo(libAssembly.Location).FileVersion;
          Console.WriteLine($"ForgeTool v{version}");
          Console.WriteLine($"LibForge v{libVersion}");
          break;
        case "test":
          {
            // TODO: Test RBMid -> Mid -> RBMid (currently only testing RBMid -> RBMid)
            var dir = args[1];
            int succ = 0, warn = 0, fail = 0;
            foreach (var f in Directory.EnumerateFiles(dir, "*.rbmid_ps4"))
            {
              var info = new FileInfo(f);
              var name = info.Name;
              using (var fi = File.OpenRead(f))
              {
                try
                {
                  var rbmid = RBMidReader.ReadStream(fi);
                  using (var ms = new MemoryStream((int)fi.Length))
                  {
                    RBMidWriter.WriteStream(rbmid, ms);
                    ms.Position = 0;
                    if (ms.Length == fi.Length)
                    {
                      succ++;
                    }
                    else
                    {
                      Console.WriteLine($"[WARN] {name}:");
                      Console.WriteLine($"  Processed file had different length ({fi.Length} orig, {ms.Length} processed)");
                      warn++;
                    }
                  }
                } catch(Exception e)
                {
                  Console.WriteLine($"[ERROR] {name}:");
                  Console.WriteLine("  " + e.Message);
                  fail++;
                }
              }
            }
            Console.WriteLine($"Summary: {succ} OK, {warn} WARN, {fail} ERROR");
          }
          break;
        default:
          Usage();
          break;
      }
    }

    static void Usage()
    {
      Console.WriteLine("Usage: ForgeTool.exe <verb> [options]");
      Console.WriteLine("Verbs: ");
      Console.WriteLine("  version");
      Console.WriteLine("    - Prints the version number and exits");
      Console.WriteLine("  rbmid2mid <input.rbmid> <output.mid>");
      Console.WriteLine("   - converts a Forge midi to a Standard Midi File");
      Console.WriteLine("  reprocess <input.rbmid> <output.rbmid>");
      Console.WriteLine("   - converts a Forge midi to a Forge midi");
      Console.WriteLine("  mid2rbmid <input.mid> <output.rbmid>");
      Console.WriteLine("   - converts a Standard Midi File to a Forge midi");
    }
  }
}
