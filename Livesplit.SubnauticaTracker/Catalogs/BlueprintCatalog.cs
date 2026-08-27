using LiveSplit.SubnauticaTracker.Versions;
using System.Collections.Generic;

namespace LiveSplit.SubnauticaTracker.Catalogs
{
    internal static class BlueprintCatalog
    {
        // The complete obtainable recipe set in the original game generation.
        // This is the documented 201-entry checklist expressed as the actual
        // TechType stored in KnownTech (never fragment/source/dev TechTypes).
        private static readonly int[] OriginalBlueprints =
        {
            3, 15, 16, 17, 27, 30, 32, 33, 34, 41, 42, 43,
            44, 53, 56, 57, 59, 61, 62, 64, 502, 503, 504, 505,
            507, 508, 509, 512, 513, 515, 517, 518, 519, 522, 523, 524,
            525, 526, 527, 528, 750, 751, 752, 754, 755, 757, 758, 759,
            761, 762, 801, 803, 804, 805, 806, 807, 808, 1025, 1026, 1027,
            1500, 1501, 1502, 1503, 1504, 1505, 1516, 1517, 1518, 1519, 1522, 1524,
            1525, 1526, 1527, 1528, 1529, 1530, 1532, 1533, 1534, 1535, 1536, 1537,
            1538, 1539, 1540, 1541, 1542, 1543, 1544, 1545, 1547, 1551, 1552, 1553,
            1554, 1555, 1557, 1558, 1802, 1803, 1805, 1806, 1812, 1813, 1819, 1820,
            1821, 1822, 2000, 2001, 2003, 2101, 2102, 2103, 2104, 2109, 2110, 2111,
            2112, 2113, 2114, 2115, 2116, 2117, 2119, 2120, 2121, 2122, 2128, 2129,
            2250, 2251, 4201, 4202, 4204, 4209, 4210, 4500, 4501, 4502, 4503, 4504,
            4505, 4506, 4507, 4508, 4509, 4510, 4511, 4512, 4514, 4517, 4518, 4519,
            4600, 4601, 4602, 4603, 4604, 4605, 4606, 4607, 4608, 4609, 4610, 4611,
            4612, 4613, 5500, 5501, 5504, 5505, 5509, 5510, 5511, 5512, 5513, 5514,
            5515, 5516, 5517, 5518, 5519, 5520, 5522, 5523, 5524, 5525, 5526, 5527,
            5528, 5529, 5530, 5900, 5901, 5902, 5903, 5904, 6009
        };

        // Living Large added five obtainable Habitat Builder recipes. The enum
        // also contains old/control-room placeholders; those are intentionally
        // excluded because the player cannot unlock them in Subnautica.
        private static readonly int[] LivingLargeBlueprints =
        {
            5534, // Large Room
            5539, // Partition Door
            5540, // Multipurpose Room Glass Dome
            5541, // Partition
            5542  // Large Room Glass Dome
        };

        public static HashSet<int> Create(SubnauticaVersion version)
        {
            var result = new HashSet<int>(OriginalBlueprints);
            if (version == SubnauticaVersion.Build2023
                || version == SubnauticaVersion.Build2025)
            {
                result.UnionWith(LivingLargeBlueprints);
            }

            return result;
        }
    }
}
