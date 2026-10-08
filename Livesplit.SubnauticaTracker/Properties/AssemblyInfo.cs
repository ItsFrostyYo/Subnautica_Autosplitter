using LiveSplit.UI.Components;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Configurable Subnautica Progression Tracker for Speedrunning.")]
[assembly: AssemblyDescription("Fully Configurable Subnautica Progression tracker for Speedrunning that displays Blueprint, Databanks and Achievement Unlocks until 100% Completion and can Provide Logging for what's Missing.")]
[assembly: AssemblyCompany("No Company")]
[assembly: AssemblyProduct("LiveSplit.SubnauticaTracker")]
[assembly: AssemblyCopyright("Copyright © Kaleb Austin 2026")]

[assembly: ComVisible(false)]
[assembly: Guid("6e869dbf-99e8-4b7c-9237-7192d6417d64")]

[assembly: AssemblyVersion("1.7.0.0")]
[assembly: AssemblyFileVersion("1.7.0.0")]
[assembly: AssemblyInformationalVersion("1.7.0")]

[assembly: ComponentFactory(typeof(LiveSplit.SubnauticaTracker.Factory))]
