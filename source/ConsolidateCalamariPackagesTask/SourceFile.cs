namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class SourceFile
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string Platform { get; set; }
        public string ArchivePath { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Hash { get; set; }
    }
}