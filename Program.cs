#define GPG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zsnapmgr
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "backup")
                {
                    string mountPoint = null;
                    if (args.Length > 1)
                    {
                        mountPoint = args[1];
                    }
                    BackupInteractive(mountPoint);
                }
                else
                {
                    Usage();
                }
            }
            else
            {
                ManageSnapshots();
            }
        }

        static void Usage()
        {
            string exeName = Process.GetCurrentProcess().MainModule.FileName;
            Console.WriteLine("Usage: {0} [command]", exeName);
            Console.WriteLine("If no arguments are given, runs automatic snapshot management.");
            Console.WriteLine("Commands:");
            Console.WriteLine("\tbackup [mount point] [volumes...]");
            Console.WriteLine("\t\tIf no options are given, runs interactively.");
        }

        static void BackupInteractive(string mountPoint = null)
        {
            if (mountPoint == null)
            {
                Console.Write("Backup location?: ");
                mountPoint = Console.ReadLine().Trim();
            }

            var volumes = new List<BackupOptions>();
            GatherVolumes(mountPoint, volumes);

            // Show menu and prompt for stuff            
            while (true)
            {
                var menu = new Table("_", "volume", "incremental", "snapshot date");
                for (int i = 0; i < volumes.Count; i++)
                {
                    menu.Add(
                        (i + 1).ToString(),
                        volumes[i].Filesystem,
                        volumes[i].IncrementalStartDate ?? "full backup",
                        volumes[i].SnapshotDate
                        );
                }
                
                Console.WriteLine();
                Console.WriteLine("Volumes to backup:");
                menu.Print();
                Console.WriteLine();

                Console.Write("Enter a number to make changes,\n"
                    + "\t'+' to add a volume,\n"
                    + "\t'-' to remove one,\n"
                    + "\t'd' to change all dates,\n"
                    + "\tor <return> to start backup: ");
                string input = Console.ReadLine();

                if (input == "+")
                {
                    Console.Write("Volume: ");
                    input = Console.ReadLine();

                    var opts = new BackupOptions();
                    if (GatherVolume(input, ref opts))
                    {
                        volumes.Add(opts);
                    }
                }
                else if (input.StartsWith("-"))
                {
                    int n;
                    if (!int.TryParse(input.Substring(1), out n))
                    {
                        Console.Write("Remove which one?: ");
                        if (!int.TryParse(Console.ReadLine(), out n))
                        {
                            Console.WriteLine("Invalid number");
                            continue;
                        }
                    }
                    
                    if (n <= 0 || n > volumes.Count)
                    {
                        Console.WriteLine("Index out of range.");
                        continue;
                    }

                    volumes.RemoveAt(n - 1);
                }
                else if (input.StartsWith("d") || input.StartsWith("D"))
                {
                    Console.Write("Snapshot date (yyyy-MM-dd): ");
                    string date = Console.ReadLine();

                    for (int i = 0; i < volumes.Count; i++)
                    {
                        volumes[i].SnapshotDate = date;
                    }
                }
                else if (input == string.Empty)
                {
                    break;
                }
                else
                {
                    int n;
                    if (!int.TryParse(input, out n))
                    {
                        Console.WriteLine("Invalid number.");
                        continue;
                    }

                    if (n <= 0 || volumes.Count < n)
                    {
                        Console.WriteLine("Index out of range.");
                        continue;
                    }

                    var opts = volumes[n - 1];

                    Console.Write("Change (I)ncremental starting snapshot, (S)napshot date: ");
                    input = Console.ReadLine();
                    if (input == "I" || input == "i")
                    {
                        Console.Write("Date (yyyy-MM-dd): ");
                        opts.IncrementalStartDate = Console.ReadLine();
                    }
                    else if (input == "S" || input == "s")
                    {
                        Console.Write("Date (yyyy-MM-dd): ");
                        opts.SnapshotDate = Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection.");
                        continue;
                    }

                    volumes.RemoveAt(n - 1);
                    volumes.Insert(n - 1, opts);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Starting backups.");
            Console.WriteLine();
            DoBackups(volumes, mountPoint);
        }

        static bool GatherVolume(string volume, ref BackupOptions opts)
        {
            ZfsSnapshots snaps = new ZfsSnapshots();
            if (!snaps.Filesystems().Contains(volume))
            {
                Console.WriteLine("No snapshots for that volume, or no such volume.");
                return false;
            }

            opts.Filesystem = volume;
            opts.Filename = volume.Replace('/', '_');
            opts.IncrementalStartDate = null;
            opts.SnapshotDate = snaps.Snapshots(volume).OrderByDescending(s => s.Date).First().Date.ToString("yyyy-MM-dd");

            return true;
        }

        static void GatherVolumes(string mountPoint, List<BackupOptions> volumes)
        {
            IEnumerable<string> files = null;
            try
            {
                files = Directory.EnumerateFiles(mountPoint, "*.zfs*", System.IO.SearchOption.TopDirectoryOnly);
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (files.Count() == 0)
                return;

            string snapList = string.Join("\n",
                files.Where(s => !s.EndsWith("_partial"))
                    .Select(s => new string(
                        s.Take(s.LastIndexOf(".zfs"))
                            .Skip(s.LastIndexOf(Path.DirectorySeparatorChar) + 1)
                            .ToArray())));

            ZfsSnapshots backups = new ZfsSnapshots(snapList);
            ZfsSnapshots snaps = new ZfsSnapshots();

            var namesToFilesystems = new Dictionary<string,string>();
            foreach (string name in backups.Filesystems())
            {
                if (snaps.Filesystems().Contains(name.Replace('_', '/')))
                {
                    namesToFilesystems.Add(name, name.Replace('_', '/'));
                }
                else
                {
                    IEnumerable<string> matches = snaps.Filesystems().Where(s => s.EndsWith("/" + name));
                    if (matches.Count() == 1)
                    {
                        namesToFilesystems.Add(name, matches.Single());
                    }
                    else
                    {
                        Console.WriteLine("Backup filename \"{0}\" is ambiguous. Not sure what volume it's for. Skipping it.", name);
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in namesToFilesystems)
            {
                var opts = new BackupOptions();

                opts.Filename = pair.Key;
                opts.Filesystem = pair.Value;

                var newestBackup = backups.Snapshots(pair.Key).OrderByDescending(s => s.Date).First();
                var newestSnapshot = snaps.Snapshots(pair.Value).OrderByDescending(s => s.Date).First();

                opts.SnapshotDate = newestSnapshot.Date.ToString("yyyy-MM-dd");

                try
                {
                    ZfsSnapshots.SnapInfo snapForBackup = snaps.Snapshots(pair.Value).Where(s => s.Date == newestBackup.Date).Single();

                    if (newestSnapshot.Date == snapForBackup.Date)
                    {
                        Console.WriteLine("Backup file \"{0}\" is already current.", newestBackup.Name);
                        opts.SnapshotDate = null;
                    }
                    else
                    {
                        opts.IncrementalStartDate = snapForBackup.Date.ToString("yyyy-MM-dd");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Couldn't find a snapshot for backup file \"{0}\". Doing non-incremental backup.", newestBackup.Name);
                    opts.IncrementalStartDate = null;
                }

                volumes.Add(opts);
            }
        }

        class BackupOptions
        {
            public string Filename;
            public string Filesystem;
            public string SnapshotDate;
            public string IncrementalStartDate;
        }

#if GPG
        static string ReadPassphrase(string prompt)
        {
            Console.Write(string.Format("{0}: ", prompt));

            string pass = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b");
                    pass = pass.Substring(0, pass.Length - 1);
                }
                else if (key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
            }
            while (key.Key != ConsoleKey.Enter);
            
            Console.WriteLine();

            return pass;
        }
#endif

        static void DoBackups(IEnumerable<BackupOptions> volumes, string mountPoint)
        {
#if GPG
            string pass1 = null, pass2 = null;
            do
            {
                if (pass1 != pass2)
                {
                    Console.WriteLine("Passwords do not match.");
                }
                pass1 = ReadPassphrase("GPG passphrase");
                pass2 = ReadPassphrase("again");
            }
            while (pass1 != pass2);
#endif

            foreach (BackupOptions vol in volumes)
            {
                if (vol.SnapshotDate == null)
                    continue;

                Console.WriteLine(new string('#', 79));
                Console.WriteLine(vol.Filesystem);
                if (vol.IncrementalStartDate == null)
                {
                    Console.Write("Full Backup ");
                }
                else
                {
                    Console.Write("{0} - ", vol.IncrementalStartDate);
                }
                Console.WriteLine(vol.SnapshotDate);
                Console.WriteLine();

#if GPG
                var mkfifo = new ProcessStartInfo();
                mkfifo.FileName = "mkfifo";
                mkfifo.Arguments = "z.fifo";
                Process.Start(mkfifo);

                var tasks = new Task[2];

                tasks[0] = Task.Run(() => {
                    Zfs.Send(
                       string.Format("{0}@{1}", vol.Filesystem, vol.SnapshotDate),
                        Path.Combine(mountPoint, string.Format("{0}@{1}.zfs.bz2.gpg", vol.Filename, vol.SnapshotDate)),
                        (vol.IncrementalStartDate == null) ? null : string.Format("{0}@{1}", vol.Filesystem, vol.IncrementalStartDate),
                        "pbzip2 | gpg --symmetric --output - --passphrase-file z.fifo --batch");
                });

                // HACK! This shouldn't be needed! :(
                Thread.Sleep(100);
                
                tasks[1] = Task.Run(() => {
                    using (var fifo = File.Open("z.fifo", FileMode.Open))
                    {
                        var enc = new UnicodeEncoding();
                        byte[] bytes = enc.GetBytes(pass1);
                        fifo.Write(bytes, 0, bytes.Length);
                        fifo.Flush(true);
                    }
                });


                Task.WaitAll(tasks);

                var rm = new ProcessStartInfo();
                rm.FileName = "rm";
                rm.Arguments = "z.fifo";
                Process.Start(rm);
#else
                Zfs.Send(
                    string.Format("{0}@{1}", vol.Filesystem, vol.SnapshotDate),
                    Path.Combine(mountPoint, string.Format("{0}@{1}.zfs.bz2", vol.Filename, vol.SnapshotDate)),
                    (vol.IncrementalStartDate == null) ? null : string.Format("{0}@{1}", vol.Filesystem, vol.IncrementalStartDate),
                    "pbzip2");
#endif
                Console.WriteLine();
            }
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
                        snap.Size.Value.HumanNumber("F2"),
                        count);
                        
                    var firstOfMonth = allSnaps.Snapshots(fs)
                        .Select(e => e.Date)
                        .Where(e => e.Year == snap.Date.Year)
                        .Where(e => e.Month == snap.Date.Month)
                        .OrderBy(e => e)
                        .First();

                    if (count > 60)
                    {
                        // Keep only if the first snapshot of the month.

                        if (snap.Date != firstOfMonth)
                        {
                            delete = true;
                        }
                    }
                    else if (count > 30)
                    {
                        // Keep only if the first snapshot of the week or month.

                        var firstOfWeek = allSnaps.Snapshots(fs)
                            .Select(e => e.Date)
                            .Where(e => e.Year == snap.Date.Year)
                            .Where(e => e.GetWeekOfYear() == snap.Date.GetWeekOfYear())
                            .OrderBy(e => e)
                            .First();

                        if ((snap.Date != firstOfWeek) && (snap.Date != firstOfMonth))
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
                Console.WriteLine("zfs destroy {0}", delete.Name);
                Console.WriteLine(ZfsSnapshots.Delete(delete));
            }
        }
    }
}
