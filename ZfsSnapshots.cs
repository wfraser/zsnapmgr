using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace zsnapmgr
{
    class ZfsSnapshots
    {
        public ZfsSnapshots()
        {
            m_snaps = new List<SnapInfo>();

            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = "list -t snap -H";
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            output = output.Trim();
            zfs.WaitForExit();
            
            /* debug:
            var sb = new StringBuilder();
            for (int i = 1; i < 1000; i++)
            {
                DateTime t = DateTime.Now.AddDays(-1 * i);
                if (t.Month != 1) // skip a month for a test
                {
                    sb.AppendFormat("foo@{0}-{1}-{2}\t\n", t.Year, t.Month, t.Day);
                }
            }
            output = sb.ToString().Trim();
            */

            foreach (string snap in output.Split('\n'))
            {
                string name = snap.Split('\t')[0];

                var info = new SnapInfo();
                info.Name = name;

                string[] parts = name.Split(new char[] { '@' }, 2);
                info.Filesystem = parts[0];

                parts = parts[1].Split(new char[] { '-' }, 3);
                info.Date = new DateTime(parts[0].AsInt(), parts[1].AsInt(), parts[2].AsInt());

                m_snaps.Add(info);
            }
        }

        public static string Delete(string snapshotName)
        {
            /*
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = "destroy " + snapshotName + " -v";
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output;
             */
            return "not deleting " + snapshotName + " (not implemented)\n";
        }

        public static string Snapshot(string filesystem)
        {
            var zfs = new Process();
            zfs.StartInfo.UseShellExecute = false;
            zfs.StartInfo.RedirectStandardOutput = true;
            zfs.StartInfo.RedirectStandardError = true;
            zfs.StartInfo.FileName = "zfs";
            zfs.StartInfo.Arguments = string.Format("snapshot {0}@{1}", filesystem, DateTime.Now.ToString("yyyy-MM-dd"));
            zfs.Start();

            string output = zfs.StandardOutput.ReadToEnd();
            output += zfs.StandardError.ReadToEnd();
            zfs.WaitForExit();

            return output;
        }

        public IEnumerable<string> Filesystems()
        {
            return m_snaps.Select(e => e.Filesystem).Distinct();
        }

        public IEnumerable<SnapInfo> Snapshots(string filesystem)
        {
            return m_snaps.Where(e => e.Filesystem == filesystem).OrderByDescending(e => e.Date);
        }

        public struct SnapInfo
        {
            public string Name;
            public string Filesystem;
            public DateTime Date;
        }

        private List<SnapInfo> m_snaps;

    }
}
