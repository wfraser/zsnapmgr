using System;
using System.Collections.Generic;
using System.Linq;

namespace zsnapmgr
{
    class Program
    {
        static void Main(string[] args)
        {
            ManageSnapshots();
        }

        static void ManageSnapshots()
        {
            var allSnaps = new ZfsSnapshots();
            DateTime now = DateTime.Now;
            var toDelete = new List<ZfsSnapshots.SnapInfo>();

            foreach (string fs in allSnaps.Filesystems())
            {
                int count = 0;
                foreach (ZfsSnapshots.SnapInfo snap in allSnaps.Snapshots(fs))
                {
                    count++;
                    int daysOld = (now - snap.Date).Days;

                    // Keep 30 daily snapshots,
                    // then 30 weekly snapshots,
                    // Then monthly snapshots forever.

                    bool delete = false;

                    if ((count == 1) && (daysOld != 0) && !snap.NoAutoSnapshot)
                    {
                        Console.Write(ZfsSnapshots.Snapshot(fs));
                        Console.WriteLine("{0}\t{1}\t[NEW]", fs, DateTime.Now.ToString("yyyy-MM-dd"));
                        count++;
                    }

                    Console.Write("{0}\t{1}\t{2} days old\t{3}B\t#{4}",
                        snap.Filesystem,
                        snap.Date.ToString("yyyy-MM-dd"),
                        daysOld,
                        snap.Size.HumanNumber("F2"),
                        count);

                    if (count > 60)
                    {
                        // Keep only if the first snapshot of the month.

                        var firstOfMonth = allSnaps.Snapshots(fs)
                            .Select(e => e.Date)
                            .Where(e => e.Year == snap.Date.Year)
                            .Where(e => e.Month == snap.Date.Month)
                            .OrderBy(e => e)
                            .First();

                        if (snap.Date != firstOfMonth)
                        {
                            delete = true;
                        }
                    }
                    else if (count > 30)
                    {
                        // Keep only if the first snapshot of the week.

                        var firstOfWeek = allSnaps.Snapshots(fs)
                            .Select(e => e.Date)
                            .Where(e => e.Year == snap.Date.Year)
                            .Where(e => e.GetWeekOfYear() == snap.Date.GetWeekOfYear())
                            .OrderBy(e => e)
                            .First();

                        if (snap.Date != firstOfWeek)
                        {
                            delete = true;
                        }
                    }

                    if (delete)
                    {
                        Console.Write("\t[delete]");
                        toDelete.Add(snap);
                    }

                    Console.WriteLine();
                }
            }

            foreach (ZfsSnapshots.SnapInfo delete in toDelete)
            {
                Console.Write(ZfsSnapshots.Delete(delete));
            }
        }
    }
}
