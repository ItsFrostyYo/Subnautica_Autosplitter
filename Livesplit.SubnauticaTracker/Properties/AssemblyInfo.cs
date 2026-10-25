using LiveSplit.UI.Components;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Configurable Subnautica Progression Tracker for Speedrunning.")]
[assembly: AssemblyDescription("Configurable Subnautica Speedrunning Tracker for Tracking Progression, Damage Taken, Aurora Explosion Time, and Bulb Zone/Lifepod 4 Transmission, with manual Missing Entry Reports.")]
[assembly: AssemblyCompany("No Company")]
[assembly: AssemblyProduct("LiveSplit.SubnauticaTracker")]
[assembly: AssemblyCopyright("Copyright © Kaleb Austin 2026")]

[assembly: ComVisible(false)]
[assembly: Guid("6e869dbf-99e8-4b7c-9237-7192d6417d64")]

[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]
[assembly: AssemblyInformationalVersion("2.0.0")]

[assembly: ComponentFactory(typeof(LiveSplit.SubnauticaTracker.Factory))]
