//#define NOZFS

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace zsnapmgr
{
    class Zfs
    {
        public static string ListSnapshots()
        {
#if !NOZFS
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "sudo";
            zfs.StartInfo.Arguments = "zfs list -t snap -H -p -o name,used,zsnapmgr:noautosnap";
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output.Trim();
#elif true
            //return System.IO.File.ReadAllText(@"\\odin\mnt\snaps.txt", Encoding.UTF8).Trim();
            return System.IO.File.ReadAllText("/mnt/snaps.txt").Trim();
#else
            var sb = new StringBuilder();
            sb.Append("foo@0-0-0\t31337\n");
            sb.Append("foo@shazam-what\t31337\n");
            sb.Append("foo@lol\t31337\n");
            for (int i = 1; i < 1000; i++)
            {
                DateTime t = DateTime.Now.AddDays(-1 * i);
                if (t.Month != 1) // skip a month for a test
                {
                    sb.AppendFormat("foo@{0}-{1}-{2}\t89371408\n", t.Year, t.Month, t.Day);
                }
            }
            return sb.ToString().Trim();
#endif
        }

        public static string Snapshot(string snapshotName)
        {
            if (!snapshotName.Contains("@"))
            {
                throw new ArgumentException("snapshot name needs to contain '@' character");
            }

#if !NOZFS
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "sudo";
            zfs.StartInfo.Arguments = string.Format("zfs snapshot {0}", snapshotName);
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output.Trim();
#else
            return "Snapshot not implemented.";
#endif
        }

        public static string DestroySnapshot(string snapshotName)
        {
            if (!snapshotName.Contains("@"))
            {
                throw new ArgumentException("snapshot name needs to contain '@' character");
            }

#if !NOZFS
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "sudo";
            zfs.StartInfo.Arguments = string.Format("zfs destroy {0} -v", snapshotName);
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output.Trim();
#else
            return "Snapshot destroy not implemented.";
#endif
        }

        public static void Send(string snapshotName, string destFilename, string incrementalName = null, string filterProgram = null)
        {
            StringBuilder cmdline = new StringBuilder("sudo zfs send -P -v");
            if (incrementalName != null)
            {
                cmdline.AppendFormat(" -i '{0}'", incrementalName);
            }
            cmdline.AppendFormat(" '{0}'", snapshotName);
            if (filterProgram != null)
            {
                cmdline.AppendFormat(" | {0}", filterProgram);
            }
            cmdline.AppendFormat(" > '{0}_partial'", destFilename);

            Console.WriteLine(cmdline.ToString());

            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "sh";
            zfs.StartInfo.Arguments = string.Format("-c \"{0}\"", cmdline.ToString());

            DateTime start = DateTime.Now;
            zfs.Start();

            long size = 0;
            int lastLineLen = 0;
            for (;;)
            {
                string line = zfs.StandardError.ReadLine();

                if (line == null)
                    break;

                if (line.StartsWith("size\t"))
                {
                    size = line.Substring(line.IndexOf('\t') + 1).AsLong();
                    Console.WriteLine("Full size: {0}B", size.HumanNumber("G4"));
                }
                else if (line.StartsWith("full\t") || line.StartsWith("incremental\t"))
                {
                    continue;
                }
                else
                {
                    string[] parts = line.Split('\t');
                    string outLine = string.Format("{0} {1:0.#}% {2}B", parts[0], (((double)parts[1].AsLong() / size) * 100), parts[1].AsLong().HumanNumber("G4"));
                    Console.Write("\r{0}{1}", outLine, new string(' ', Math.Max(0, (lastLineLen - outLine.Length))));
                    lastLineLen = outLine.Length;
                }
            }

            zfs.WaitForExit();
            var duration = (DateTime.Now - start).Duration();

            File.Move(string.Format("{0}_partial", destFilename), destFilename);

            var fileInfo = new FileInfo(destFilename);
            string rate = (duration.TotalSeconds == 0) ? "NaN " : ((long)(size / duration.TotalSeconds)).HumanNumber("G4");

            string summary = string.Format("{0} 100% {1}B in {2}; {3}B/s",
                DateTime.Now.ToString("HH:mm:ss"),
                size.HumanNumber("G4"),
                duration,
                rate);
            Console.WriteLine("\r{0}{1}", summary, new string(' ', Math.Max(0, (lastLineLen - summary.Length))));

            Console.WriteLine("Final size: {0}B; {1:G4}% compression ratio", fileInfo.Length.HumanNumber("G4"), (double)fileInfo.Length / size * 100);
        }
    }
}
