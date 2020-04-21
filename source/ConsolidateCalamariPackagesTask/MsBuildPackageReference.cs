namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class MsBuildPackageReference
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string ResolvedPath { get; set; }
    }
}