using System;
using System.Collections.Generic;

namespace LiveSplit.SubnauticaTracker.Catalogs
{
    internal static class DatabankCatalog
    {
        // PDAEncyclopedia contains 323 raw keys in every supported build.
        // These obsolete, duplicate, or inconsistent records are not counted.
        // VMS is intentionally excluded: the normal Vehicle Upgrade Console
        // data-box unlock does not award it, and its legacy fragment route is
        // not consistently available in released gameplay.
        private static readonly HashSet<string> ExcludedEntries =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CoralSample",
                "BloodGrass",
                "BasaltOutcrops",
                "IslandsPDAPaal",
                "LifepodCTODialog1",
                "LifepodCTODialog2",
                "LifepodCTOLog1",
                "LifepodCTOLog2",
                "InnerBiomeWreckLore6",
                "InnerBiomeWreckLore9",
                "Exo_Code_LifepodPDA",
                "Exo_Code_WreckPDA",
                "Aurora_RingRoom_Terminal1",
                "Aurora_RingRoom_Terminal2",
                "AuroraEngineeringLog",
                "InnerBiomeWreckLore8",
                "OuterBiomeWreckLore7",
                "OuterBiomeWreckLore8",
                "DeepPDABase3",
                "LifepodCaptainsQuartersCode",
                "PrecursorPrisonArtifact9",
                "SeaEmperorLeviathan",
                "SeaEmperorLeviathanEnzymeCloud",
                "Precursor_LostRiverBase_StructuralDamage",
                "VMS",
                "Mercury",
                "DataCoil",
                "PrecursorPrisonEggChamberEmperorEgg",
                "RaysAdvanced",
                "RadioMushroom24NoSignalAltDatabank",
                "RadioGrassy25NoSignalAltDatabank"
            };

        public static bool IsTracked(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && !ExcludedEntries.Contains(key);
        }
    }
}
