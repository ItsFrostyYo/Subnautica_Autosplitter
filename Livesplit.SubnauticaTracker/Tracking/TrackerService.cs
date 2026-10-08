using LiveSplit.SubnauticaTracker.Diagnostics;
using LiveSplit.SubnauticaTracker.Memory;
using LiveSplit.SubnauticaTracker.Versions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LiveSplit.SubnauticaTracker.Tracking
{
    internal sealed class TrackerService : IDisposable
    {
        private readonly Timer timer;
        private readonly Timer damageTimer;
        private readonly object damageSync = new object();

        private ProcessMemory processMemory;
        private SubnauticaUnlockReader unlockReader;
        private GameVersionInfo version;
        private DateTime nextInitializeUtc = DateTime.MinValue;
        private DateTime attachedUtc = DateTime.MinValue;
        private volatile TrackerSnapshot snapshot = TrackerSnapshot.Waiting;
        private TrackerState lastLoggedState = (TrackerState)(-1);
        private string lastLoggedSlot = string.Empty;
        private int lastLoggedBlueprints = -1;
        private int lastLoggedDatabanks = -1;
        private int lastLoggedAchievements = -1;
        private int lastLoggedDamageHits = -1;
        private double lastLoggedDamageTotal = -1d;
        private int polling;
        private int damagePolling;
        private string damageSlot = string.Empty;
        private bool hasPreviousHealth;
        private float previousHealth;
        private int previousDisplayedHealth;
        private IntPtr damagePlayer = IntPtr.Zero;
        private int damageHits;
        private double damageTotal;
        private int introDamageHits;
        private double introDamageTotal;
        private bool firstDamageObserved;

        public TrackerService()
        {
            TrackerLog.StartSession();
            timer = new Timer(Poll, null, 0, 250);
            damageTimer = new Timer(PollDamage, null, 0, 20);
        }

        public TrackerSnapshot Snapshot => snapshot;
        public static string MissingReportPath => MissingReportWriter.FilePath;

        public bool TryWriteMissingReport(out string path, out string error)
        {
            path = MissingReportWriter.FilePath;
            error = string.Empty;

            TrackerSnapshot current = snapshot;
            SubnauticaUnlockReader reader = unlockReader;
            MissingItems missing;
            if (current.State != TrackerState.Tracking
                || reader == null
                || !reader.TryGetMissingItems(out missing))
            {
                return false;
            }

            try
            {
                MissingReportWriter.Write(missing);
                TrackerLog.Info("Missing report written: " + path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                TrackerLog.Exception("Missing report write failed", exception);
                return false;
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            damageTimer.Dispose();
            Detach();
        }

        private void Poll(object state)
        {
            if (Interlocked.Exchange(ref polling, 1) != 0)
                return;

            try
            {
                if (processMemory == null || !processMemory.IsAlive)
                {
                    Detach();
                    if (!TryAttach())
                    {
                        Publish(new TrackerSnapshot(
                            TrackerState.WaitingForGame,
                            string.Empty,
                            string.Empty,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown,
                            DamageStats.Unknown));
                        return;
                    }
                }

                if (!unlockReader.IsInitialized)
                {
                    DateTime now = DateTime.UtcNow;
                    if (now >= nextInitializeUtc)
                    {
                        nextInitializeUtc = now.AddSeconds(2);
                        bool initialized = unlockReader.TryInitialize();
                        if (!initialized)
                        {
                            if (now - attachedUtc >= TimeSpan.FromSeconds(10))
                            {
                                TrackerLog.Throttled(
                                    "initialize",
                                    "Initialization waiting: " + unlockReader.LastError,
                                    TimeSpan.FromSeconds(5));
                            }
                        }
                        else
                        {
                            TrackerLog.Info(
                                "Managed metadata initialized using "
                                + unlockReader.RuntimeDescription + ".");
                        }
                    }

                    if (!unlockReader.IsInitialized)
                    {
                        Publish(CreateSnapshot(
                            TrackerState.Initializing,
                            string.Empty,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown));
                        return;
                    }
                }

                UnlockReadResult read;
                if (!unlockReader.TryRead(out read))
                {
                    TrackerLog.Throttled(
                        "read-failed",
                        "Live read failed: " + unlockReader.LastError,
                        TimeSpan.FromSeconds(5));
                    if (RetainLastTrackingSnapshot(string.Empty))
                        return;

                    Publish(CreateSnapshot(
                        TrackerState.Initializing,
                        string.Empty,
                        TrackerCount.Unknown,
                        TrackerCount.Unknown,
                        TrackerCount.Unknown));
                    return;
                }

                switch (read.State)
                {
                    case UnlockReaderState.MainMenu:
                        Publish(CreateSnapshot(
                            TrackerState.MainMenu,
                            string.Empty,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown));
                        break;

                    case UnlockReaderState.Tracking:
                        Publish(CreateSnapshot(
                            TrackerState.Tracking,
                            read.SaveSlot,
                            new TrackerCount(true, read.BlueprintsUnlocked, read.BlueprintsTotal),
                            new TrackerCount(true, read.DatabanksUnlocked, read.DatabanksTotal),
                            new TrackerCount(true, read.AchievementsUnlocked, read.AchievementsTotal)));
                        break;

                    default:
                        TrackerLog.Throttled(
                            "read-initializing",
                            "Save detected but data is not ready: " + unlockReader.LastError,
                            TimeSpan.FromSeconds(5));
                        if (RetainLastTrackingSnapshot(read.SaveSlot))
                            break;

                        Publish(CreateSnapshot(
                            TrackerState.Initializing,
                            read.SaveSlot,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown,
                            TrackerCount.Unknown));
                        break;
                }
            }
            catch (Exception exception)
            {
                TrackerLog.Exception("Unhandled polling error", exception);
                Publish(new TrackerSnapshot(
                    TrackerState.Error,
                    version?.DisplayName ?? string.Empty,
                    string.Empty,
                    TrackerCount.Unknown,
                    TrackerCount.Unknown,
                    TrackerCount.Unknown,
                    DamageStats.Unknown));
            }
            finally
            {
                Interlocked.Exchange(ref polling, 0);
            }
        }

        private void PollDamage(object state)
        {
            if (Interlocked.Exchange(ref damagePolling, 1) != 0)
                return;

            try
            {
                TrackerSnapshot current = snapshot;
                SubnauticaUnlockReader reader = unlockReader;
                if (current.State != TrackerState.Tracking
                    || string.IsNullOrWhiteSpace(current.SaveSlot)
                    || reader == null)
                {
                    ResetDamageTracking();
                    return;
                }

                float health;
                int displayedHealth;
                IntPtr player;
                if (!reader.TryReadPlayerHealth(out health, out displayedHealth, out player))
                {
                    if (player == IntPtr.Zero)
                        ResetDamageTracking();
                    else
                    {
                        lock (damageSync)
                            hasPreviousHealth = false;
                    }
                    return;
                }

                int gameMode;
                bool hasGameMode = reader.TryReadGameMode(out gameMode);

                lock (damageSync)
                {
                    if (!string.Equals(damageSlot, current.SaveSlot, StringComparison.Ordinal)
                        || damagePlayer != player)
                    {
                        damageSlot = current.SaveSlot;
                        damagePlayer = player;
                        damageHits = 0;
                        damageTotal = 0d;
                        introDamageHits = 0;
                        introDamageTotal = 0d;
                        firstDamageObserved = false;
                        hasPreviousHealth = false;
                    }

                    if (hasPreviousHealth)
                    {
                        float lost = previousHealth - health;
                        if (lost > 0.0001f)
                        {
                            bool isIntroDamage = !firstDamageObserved
                                && hasGameMode
                                && IsIntroDamageGameMode(gameMode)
                                && previousHealth >= 99.9f
                                && health >= 79.9f
                                && health <= 80.1f
                                && lost >= 19.9f
                                && lost <= 20.1f;
                            firstDamageObserved = true;
                            int wholeDamage = isIntroDamage
                                ? 20
                                : Math.Max(0, previousDisplayedHealth - displayedHealth);
                            damageHits++;
                            damageTotal += wholeDamage;
                            if (isIntroDamage)
                            {
                                introDamageHits++;
                                introDamageTotal += wholeDamage;
                                TrackerLog.Info("Detected the unavoidable 20-damage lifepod intro hit.");
                            }
                        }
                    }

                    previousHealth = health;
                    previousDisplayedHealth = displayedHealth;
                    hasPreviousHealth = true;
                }
            }
            catch (Exception exception)
            {
                TrackerLog.Throttled(
                    "damage-read-failed",
                    "Damage tracking read failed: " + exception.Message,
                    TimeSpan.FromSeconds(5));
            }
            finally
            {
                Interlocked.Exchange(ref damagePolling, 0);
            }
        }

        private bool TryAttach()
        {
            Process process = null;
            try
            {
                process = Process.GetProcessesByName("Subnautica")
                    .Where(candidate =>
                    {
                        try { return !candidate.HasExited && candidate.MainModule != null; }
                        catch { return false; }
                    })
                    .OrderByDescending(candidate =>
                    {
                        try { return candidate.StartTime; }
                        catch { return DateTime.MinValue; }
                    })
                    .FirstOrDefault();

                if (process == null)
                {
                    return false;
                }

                version = GameVersionDetector.Detect(process);
                processMemory = new ProcessMemory(process);
                unlockReader = new SubnauticaUnlockReader(processMemory, version.Version);
                nextInitializeUtc = DateTime.MinValue;
                attachedUtc = DateTime.UtcNow;
                TrackerLog.Info(
                    "Attached to PID " + process.Id
                    + " at '" + version.GameRoot + "'. Detected build "
                    + version.DisplayName
                    + (version.ExactMatch ? " (exact assembly hash)." : " (structural fallback)."));
                return true;
            }
            catch (Exception exception)
            {
                TrackerLog.Exception("Process attachment failed", exception);
                process?.Dispose();
                return false;
            }
        }

        private TrackerSnapshot CreateSnapshot(
            TrackerState state,
            string saveSlot,
            TrackerCount blueprints,
            TrackerCount databanks,
            TrackerCount achievements)
        {
            return new TrackerSnapshot(
                state,
                version?.DisplayName ?? string.Empty,
                saveSlot,
                blueprints,
                databanks,
                achievements,
                state == TrackerState.Tracking
                    ? GetDamageStats(saveSlot)
                    : DamageStats.Unknown);
        }

        private DamageStats GetDamageStats(string saveSlot)
        {
            lock (damageSync)
            {
                if (!string.Equals(damageSlot, saveSlot, StringComparison.Ordinal))
                    return new DamageStats(true, 0, 0d, 0, 0d);

                return new DamageStats(
                    true,
                    damageHits,
                    damageTotal,
                    introDamageHits,
                    introDamageTotal);
            }
        }

        private void ResetDamageTracking()
        {
            lock (damageSync)
            {
                damageSlot = string.Empty;
                hasPreviousHealth = false;
                previousHealth = 0f;
                previousDisplayedHealth = 0;
                damagePlayer = IntPtr.Zero;
                damageHits = 0;
                damageTotal = 0d;
                introDamageHits = 0;
                introDamageTotal = 0d;
                firstDamageObserved = false;
            }
        }

        private static bool IsIntroDamageGameMode(int gameMode)
        {
            return gameMode == 0     // Survival
                || gameMode == 2     // Freedom
                || gameMode == 257;  // Hardcore
        }

        private bool RetainLastTrackingSnapshot(string saveSlot)
        {
            TrackerSnapshot current = snapshot;
            if (current.State != TrackerState.Tracking)
                return false;

            return string.IsNullOrWhiteSpace(saveSlot)
                || string.Equals(current.SaveSlot, saveSlot, StringComparison.Ordinal);
        }

        private void Publish(TrackerSnapshot next)
        {
            snapshot = next;
            if (next.State == lastLoggedState
                && string.Equals(next.SaveSlot, lastLoggedSlot, StringComparison.Ordinal)
                && next.Blueprints.Unlocked == lastLoggedBlueprints
                && next.Databanks.Unlocked == lastLoggedDatabanks
                && next.Achievements.Unlocked == lastLoggedAchievements
                && next.Damage.Hits == lastLoggedDamageHits
                && Math.Abs(next.Damage.TotalDamage - lastLoggedDamageTotal) < 0.0001d)
            {
                return;
            }

            bool progressUpdate = next.State == TrackerState.Tracking
                && lastLoggedState == TrackerState.Tracking
                && string.Equals(next.SaveSlot, lastLoggedSlot, StringComparison.Ordinal);

            lastLoggedState = next.State;
            lastLoggedSlot = next.SaveSlot;
            lastLoggedBlueprints = next.Blueprints.Unlocked;
            lastLoggedDatabanks = next.Databanks.Unlocked;
            lastLoggedAchievements = next.Achievements.Unlocked;
            lastLoggedDamageHits = next.Damage.Hits;
            lastLoggedDamageTotal = next.Damage.TotalDamage;
            TrackerLog.Info(
                (progressUpdate ? "Progress" : "State -> " + next.State)
                + (string.IsNullOrWhiteSpace(next.Version) ? string.Empty : ", build " + next.Version)
                + (string.IsNullOrWhiteSpace(next.SaveSlot) ? string.Empty : ", slot " + next.SaveSlot)
                + (next.State == TrackerState.Tracking
                    ? ", BP " + next.Blueprints.Unlocked + "/" + next.Blueprints.Total
                        + ", DB " + next.Databanks.Unlocked + "/" + next.Databanks.Total
                        + ", A " + next.Achievements.Unlocked + "/" + next.Achievements.Total
                        + ", hits " + next.Damage.Hits
                        + ", damage " + Math.Floor(next.Damage.TotalDamage).ToString("0")
                    : string.Empty));
        }

        private void Detach()
        {
            unlockReader = null;
            version = null;
            nextInitializeUtc = DateTime.MinValue;
            attachedUtc = DateTime.MinValue;
            ResetDamageTracking();

            if (processMemory != null)
            {
                TrackerLog.Info("Detached from the Subnautica process.");
                processMemory.Dispose();
                processMemory = null;
            }
        }
    }
}
