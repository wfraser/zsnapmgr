﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace zsnapmgr
{
    class ZfsSnapshots
    {
        public ZfsSnapshots()
        {
            m_snaps = new Dictionary<string, List<SnapInfo>>();

            string output = Zfs.ListSnapshots();

            foreach (string snap in output.Split('\n'))
            {
                string[] parts = snap.Split('\t');
                string name = parts[0];
                long size = parts[1].AsLong();
                bool noAutoSnap = ((parts.Length > 2) && !string.IsNullOrEmpty(parts[2]) && (parts[2] != "-") && (parts[2] != "no"));

                var info = new SnapInfo();
                info.Name = name;
                info.Size = size;
                info.NoAutoSnapshot = noAutoSnap;

                parts = name.Split(new char[] { '@' }, 2);
                info.Filesystem = parts[0];

                parts = parts[1].Split(new char[] { '-' }, 3);
                info.Date = new DateTime(parts[0].AsInt(), parts[1].AsInt(), parts[2].AsInt());

                if (!m_snaps.ContainsKey(info.Filesystem))
                {
                    m_snaps[info.Filesystem] = new List<SnapInfo>();
                }
                m_snaps[info.Filesystem].Add(info);
            }
        }

        public static string Delete(SnapInfo snapshot)
        {
            return Zfs.DestroySnapshot(snapshot.Name);
        }

        public static string Snapshot(string filesystem)
        {
            return Zfs.Snapshot(string.Format("{0}@{1}", filesystem, DateTime.Now.ToString("yyyy-MM-dd")));
        }

        public IEnumerable<string> Filesystems()
        {
            return m_snaps.Keys;
        }

        public IEnumerable<SnapInfo> Snapshots(string filesystem)
        {
            return m_snaps[filesystem].OrderByDescending(e => e.Date);
        }

        public struct SnapInfo
        {
            public string Name;
            public string Filesystem;
            public DateTime Date;
            public long Size;
            public bool NoAutoSnapshot;
        }

        private Dictionary<string, List<SnapInfo>> m_snaps;
    }
}
