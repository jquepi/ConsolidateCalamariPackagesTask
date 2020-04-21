using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    static class ConsolidatedPackageCreator
    {
        public static void Create(SourceFile[] sourceFiles, string destination)
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
                .GroupBy(sourceFile => new {sourceFile.FullNameInDestinationArchive, sourceFile.Hash})
                .Select(g => new
                {
                    g.Key.FullNameInDestinationArchive,
                    g.Key.Hash,
                    g.First().FullNameInSourceArchive,
                    g.First().ArchivePath
                });

            foreach (var groupedBySourceArchive in uniqueFiles.GroupBy(f => f.ArchivePath))
            {
                using (var sourceZip = ZipFile.OpenRead(groupedBySourceArchive.Key))
                    foreach (var uniqueFile in groupedBySourceArchive)
                    {
                        var entryName = Path.Combine(uniqueFile.Hash, uniqueFile.FullNameInDestinationArchive);
                        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);

                        using (var destStream = entry.Open())
                        using (var sourceStream = sourceZip.Entries.First(e => e.FullName == uniqueFile.FullNameInSourceArchive).Open())
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
    }
}