using LiveSplit.SubnauticaMarco.Configuration;
using LiveSplit.SubnauticaTracker.Versions;
using System.Drawing;

namespace LiveSplit.SubnauticaMarco.Macros
{
    internal sealed class MacroProfile
    {
        public Point Quit { get; private set; }
        public Point ConfirmQuit { get; private set; }
        public Point ConfirmQuitAgain { get; private set; }
        public Point Play { get; private set; }
        public Point NewGame { get; private set; }
        public Point GameMode { get; private set; }
        public bool ModernMenu { get; private set; }

        public static MacroProfile Create(SubnauticaVersion version, ResetGameMode requestedMode)
        {
            bool modern = version == SubnauticaVersion.Build2023
                || version == SubnauticaVersion.Build2025;
            ResetGameMode mode = ResolveMode(requestedMode, modern);

            if (modern)
            {
                return new MacroProfile
                {
                    Quit = new Point(mode == ResetGameMode.Hardcore ? 1067 : 1021, mode == ResetGameMode.Hardcore ? 691 : 728),
                    ConfirmQuit = new Point(903, 568),
                    ConfirmQuitAgain = new Point(830, 623),
                    Play = new Point(635, 728),
                    NewGame = new Point(1300, 569),
                    GameMode = GetModernModePoint(mode),
                    ModernMenu = true
                };
            }

            bool betterRng = IsBetterRng(requestedMode);
            return new MacroProfile
            {
                Quit = new Point(mode == ResetGameMode.Hardcore ? 1052 : mode == ResetGameMode.Creative ? 1012 : 1020,
                    mode == ResetGameMode.Hardcore ? 662 : mode == ResetGameMode.Creative ? 686 : 689),
                ConfirmQuit = new Point(893, 553),
                ConfirmQuitAgain = new Point(830, 623),
                Play = new Point(635, 795),
                NewGame = betterRng ? new Point(1157, 662) : new Point(1373, 540),
                GameMode = GetLegacyModePoint(mode),
                ModernMenu = false
            };
        }

        private static Point GetLegacyModePoint(ResetGameMode mode)
        {
            switch (mode)
            {
                case ResetGameMode.Hardcore: return new Point(1107, 754);
                case ResetGameMode.Creative: return new Point(1184, 911);
                case ResetGameMode.Freedom: return new Point(1157, 662);
                default: return new Point(1403, 537);
            }
        }

        private static Point GetModernModePoint(ResetGameMode mode)
        {
            switch (mode)
            {
                case ResetGameMode.Hardcore: return new Point(1093, 781);
                case ResetGameMode.Creative: return new Point(1125, 878);
                case ResetGameMode.Freedom: return new Point(1154, 674);
                default: return new Point(986, 552);
            }
        }

        private static ResetGameMode ResolveMode(ResetGameMode mode, bool modern)
        {
            if (!modern)
                return mode;

            switch (mode)
            {
                case ResetGameMode.BetterRngHardcore: return ResetGameMode.Hardcore;
                case ResetGameMode.BetterRngCreative: return ResetGameMode.Creative;
                case ResetGameMode.BetterRngSurvival: return ResetGameMode.Survival;
                default: return mode;
            }
        }

        private static bool IsBetterRng(ResetGameMode mode)
        {
            return mode == ResetGameMode.BetterRngSurvival
                || mode == ResetGameMode.BetterRngHardcore
                || mode == ResetGameMode.BetterRngCreative;
        }
    }
}
