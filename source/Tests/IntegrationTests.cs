using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Assent;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NSubstitute;
using NUnit.Framework;
using Octopus.Build.ConsolidateCalamariPackagesTask;
using TestStack.BDDfy;

namespace Tests
{
    public class IntegrationTests
    {
        private string temp;
        private string expectedZip;
        private MsBuildPackageReference[] packageReferences;
        private bool returnValue;

        public void SetUp()
        {
            temp = Path.GetTempFileName();
            File.Delete(temp);
            Directory.CreateDirectory(temp);
            expectedZip = Path.Combine(temp, $"Calamari.54d634ceb0b28d3d0463f4cd674461c5.zip");
        }

        public void TearDown()
        {
            Directory.Delete(temp, true);
        }

        public void GivenABunchOfPackageReferences()
        {
            MsBuildPackageReference CreatePackageReference(string packageId, string version)
                => new MsBuildPackageReference()
                {
                    Name = packageId,
                    Version = version,
                    ResolvedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", packageId, version)
                };

            packageReferences = new[]
            {
                CreatePackageReference("Assent", "1.5.0"),
                CreatePackageReference("Calamari", "12.0.2"),
                CreatePackageReference("Calamari.Cloud", "12.0.2"),
                CreatePackageReference("Calamari.linux-x64", "12.0.2"),
                CreatePackageReference("Calamari.osx-x64", "12.0.2"),
                CreatePackageReference("Calamari.win-x64", "12.0.2"),
            };
        }

        public void WhenTheTaskIsExecuted()
        {
            var sw = Stopwatch.StartNew();
            var task = new Consolidate(Substitute.For<ILog>());
            task.AssemblyVersion = "1.2.3";
            (returnValue, _) = task.Execute(temp, packageReferences);
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds:n0}ms");
        }

        public void ThenTheReturnValueIsTrue()
            => returnValue.Should().BeTrue();

        public void AndThenThePackageIsCreated()
        {
            Directory.GetFiles(temp).Should().BeEquivalentTo(new[] {expectedZip});
            Console.WriteLine($"Package Size: {new FileInfo(expectedZip).Length / 1024 / 1024}MB");
        }

        public void AndThenThePackageContentsShouldBe()
        {
            using (var zip = ZipFile.Open(expectedZip, ZipArchiveMode.Read))
                this.Assent(string.Join("\r\n", zip.Entries.Select(e => e.FullName).OrderBy(k => k)));
        }

        public void AndThenTheIndexShouldBe()
        {
            using (var zip = ZipFile.Open(expectedZip, ZipArchiveMode.Read))
            using (var entry = zip.Entries.First(e => e.FullName == "index.json").Open())
            using (var sr = new StreamReader(entry))
                this.Assent(sr.ReadToEnd());
        }

        [Test]
        public void Execute()
            => this.BDDfy();
    }
}