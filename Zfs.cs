//#define NOZFS

using System;
using System.Diagnostics;
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
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = "list -t snap -H -p -o name,used,zsnapmgr:noautosnap";
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output.Trim();
#else
            var sb = new StringBuilder();
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
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = string.Format("snapshot {0}", snapshotName);
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

            /*
             * Don't delete; only suggest for now.
#if !NOZFS
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = string.Format("destroy {0} -v", snapshotName);
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output.Trim();
#else
            return "Snapshot destroy not implemented.";
#endif
             */
            return string.Format("{0} suggested for 'zfs destroy'\n");
        }
    }
}
