using LiveSplit.Model;
using LiveSplit.SubnauticaTracker.Tracking;
using LiveSplit.UI.Components;
using System;

namespace LiveSplit.SubnauticaTracker
{
    internal sealed class BulbTimerRenderer : Timer
    {
        private TimeSpan? time;

        public string StatusText { get; private set; }

        public void Configure(TrackerRowSettings row, BulbTimerSnapshot bulbTimer)
        {
            Settings.DigitsFormat = row.TimerDigitsFormat;
            Settings.Accuracy = row.TimerAccuracy;
            Settings.DecimalsSize = row.TimerDecimalsSize;
            Settings.ShowGradient = row.TimerShowGradient;
            Settings.CenterTimer = row.TimerCenter;
            Settings.TimerColor = row.TimerColor;
            Settings.OverrideSplitColors = true;
            Settings.BackgroundColor = System.Drawing.Color.Transparent;
            Settings.BackgroundColor2 = System.Drawing.Color.Transparent;

            switch (bulbTimer.State)
            {
                case BulbTimerState.Running:
                    time = bulbTimer.Elapsed;
                    StatusText = null;
                    break;
                case BulbTimerState.Finished:
                    time = null;
                    StatusText = "Bulb Finished";
                    break;
                case BulbTimerState.Reset:
                    time = null;
                    StatusText = "Bulb Reset";
                    break;
                default:
                    time = null;
                    StatusText = "Not in Bulb";
                    break;
            }
        }

        public override TimeSpan? GetTime(LiveSplitState state, TimingMethod method)
        {
            return time;
        }
    }
}
