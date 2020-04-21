using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    public class Consolidate : IDisposable
    {
        private readonly ILog log;
        private readonly MD5 md5 = MD5.Create();

        public Consolidate(ILog log)
        {
            this.log = log;
        }
        
        public string AssemblyVersion { get; set; } = typeof(Consolidate).Assembly.GetName().Version.ToString();

        public (bool result, string packageFileName) Execute(string outputDirectory, IReadOnlyList<PackageReference> packageReferences)
        {
            if (!Directory.Exists(outputDirectory))
            {
                log.Error($"The output directory {outputDirectory} does not exist");
                return (false, null);
            }

            var packagesToScan = packageReferences
                .Where(p => /*p.Name.StartsWith("Sashimi.") ||*/ p.Name.StartsWith("Calamari"))
                .ToArray();

            var packageHash = GetPackageCombinationHash(packagesToScan);
            var destination = Path.Combine(outputDirectory, $"Calamari.{packageHash}.zip");
            if (File.Exists(destination))
            {
                log.Normal("Calamari zip with the right package combination hash already exists");
                return (true, destination);
            }

            DeleteExistingCalamariZips(destination);

            log.Normal("Scanning Calamari Packages");

            var indexEntries = packagesToScan.SelectMany(GetSourceFilesFromCalamariPackage).ToArray();

            log.Normal("Creating consolidated Calamari package");
            var sw = Stopwatch.StartNew();

            CreateConsolidatedPackage(indexEntries, destination);

            log.Normal($"Package creation took {sw.ElapsedMilliseconds:n0}ms");

            foreach (var item in indexEntries.Select(i => new {i.PackageId, i.Platform}).Distinct())
                log.Normal($"Packaged {item.PackageId} for {item.Platform}");

            return (true, destination);
        }

        private static void CreateConsolidatedPackage(SourceFile[] sourceFiles, string destination)
        {
            using (var zip = ZipFile.Open(destination, ZipArchiveMode.Create))
            {
                WriteUniqueFilesToZip(sourceFiles, zip);

                var indexEntry = zip.CreateEntry("index.json", CompressionLevel.Fastest);
                using (var destStream = indexEntry.Open())
                    WriteIndexTo(destStream, sourceFiles);
            }
        }

        private static void WriteUniqueFilesToZip(SourceFile[] sourceFiles, ZipArchive zip)
        {
            var uniqueFiles = sourceFiles
                .GroupBy(sourceFile => new {sourceFile.FullName, sourceFile.Hash})
                .Select(g => new
                {
                    g.Key.FullName,
                    g.Key.Hash,
                    g.First().ArchivePath
                });

            foreach (var groupedBySourceArchive in uniqueFiles.GroupBy(f => f.ArchivePath))
            {
                using (var sourceZip = ZipFile.OpenRead(groupedBySourceArchive.Key))
                    foreach (var uniqueFile in groupedBySourceArchive)
                    {
                        var entryName = Path.Combine(uniqueFile.Hash, uniqueFile.FullName);
                        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);

                        using (var destStream = entry.Open())
                        using (var sourceStream = sourceZip.Entries.First(e => e.FullName == uniqueFile.FullName).Open())
                            sourceStream.CopyTo(destStream);
                    }
            }
        }

        private static void WriteIndexTo(Stream stream, SourceFile[] sourceFiles)
        {
            var index = sourceFiles
                .GroupBy(i => new {i.PackageId, i.Version, i.Platform})
                .Select(g => new
                {
                    g.Key.PackageId,
                    g.Key.Version,
                    g.Key.Platform,
                    Hashes = g.Select(i => i.Hash).OrderBy(h => h).ToArray()
                });
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(index, Formatting.Indented));
            stream.Write(bytes, 0, bytes.Length);
        }

        private IReadOnlyList<SourceFile> GetSourceFilesFromCalamariPackage(PackageReference package)
        {
            var isNetFx = package.Name == "Calamari";
            var isCloud = package.Name == "Calamari.Cloud";
            var platform = isNetFx || isCloud
                ? "netfx"
                : package.Name.Split('.')[1];

            var archivePath = Path.Combine(package.ResolvedPath, $"{package.Name}.{package.Version}.nupkg".ToLower());
            if (!File.Exists(archivePath))
                throw new Exception($"Could not find the source NuGet package {archivePath} does not exist");

            using (var zip = ZipFile.OpenRead(archivePath))
                return zip.Entries
                    .Where(e => e.FullName != "[Content_Types].xml")
                    .Where(e => !e.FullName.StartsWith("_rels"))
                    .Where(e => !e.FullName.StartsWith("package/services"))
                    .Select(entry => new SourceFile
                    {
                        PackageId = isCloud ? "Calamari.Cloud" : "Calamari",
                        Version = package.Version,
                        Platform = platform,
                        ArchivePath = archivePath,
                        Name = entry.FullName,
                        FullName = entry.FullName,
                        Hash = Hash(entry)
                    })
                    .ToArray();
        }

        private void DeleteExistingCalamariZips(string destination)
        {
            log.Low("Deleting existing Calamari Zips");
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(destination), "Calamari.*.zip"))
                File.Delete(file);
        }

        private string GetPackageCombinationHash(PackageReference[] packagesToScan)
        {
            var uniqueString = string.Join(",", packagesToScan.OrderBy(p => p.Name).Select(p => p.Name + p.Version));
            uniqueString += AssemblyVersion;
            var hash = Hash(uniqueString);
            log.Normal($"Hash of the package combination is {hash}");
            return hash;
        }

        string Hash(ZipArchiveEntry entry)
        {
            using (var entryStream = entry.Open())
                return BitConverter.ToString(md5.ComputeHash(entryStream)).Replace("-", "").ToLower();
        }

        string Hash(string str)
            => Hash(Encoding.UTF8.GetBytes(str));

        string Hash(byte[] bytes)
            => BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").ToLower();

        public void Dispose()
        {
            md5?.Dispose();
        }
    }
}