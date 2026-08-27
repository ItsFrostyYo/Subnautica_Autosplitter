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
        private int polling;

        public TrackerService()
        {
            TrackerLog.StartSession();
            timer = new Timer(Poll, null, 0, 250);
        }

        public TrackerSnapshot Snapshot => snapshot;

        public void Dispose()
        {
            timer.Dispose();
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
                            TrackerCount.Unknown));
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
                    TrackerCount.Unknown));
            }
            finally
            {
                Interlocked.Exchange(ref polling, 0);
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
                achievements);
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
                && next.Achievements.Unlocked == lastLoggedAchievements)
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
            TrackerLog.Info(
                (progressUpdate ? "Progress" : "State -> " + next.State)
                + (string.IsNullOrWhiteSpace(next.Version) ? string.Empty : ", build " + next.Version)
                + (string.IsNullOrWhiteSpace(next.SaveSlot) ? string.Empty : ", slot " + next.SaveSlot)
                + (next.State == TrackerState.Tracking
                    ? ", BP " + next.Blueprints.Unlocked + "/" + next.Blueprints.Total
                        + ", DB " + next.Databanks.Unlocked + "/" + next.Databanks.Total
                        + ", A " + next.Achievements.Unlocked + "/" + next.Achievements.Total
                    : string.Empty));
        }

        private void Detach()
        {
            unlockReader = null;
            version = null;
            nextInitializeUtc = DateTime.MinValue;
            attachedUtc = DateTime.MinValue;

            if (processMemory != null)
            {
                TrackerLog.Info("Detached from the Subnautica process.");
                processMemory.Dispose();
                processMemory = null;
            }
        }
    }
}
