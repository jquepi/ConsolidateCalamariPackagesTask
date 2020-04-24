using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    public class ConsolidateCalamariPackagesTask : Task
    {
        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public string OutputDirectory { get; set; }
        
        [Output]
        public string ConsolidatedPackageFilename { get; set; }
        
        public override bool Execute()
        {
            // The Metadata of these ITaskItems contains:
            // Name= Microsoft.Build.Framework	
            // Type= package	
            // Version= 16.5.0	
            // Path= microsoft.build.framework/16.5.0	
            // ResolvedPath= C:\\Users\\rober\\.nuget\\packages\\microsoft.build.framework\\16.5.0	

            var packageReferences = Packages
                .Select(p => new MsBuildPackageReference
                {
                    Name = p.GetMetadata("Name"),
                    Version = p.GetMetadata("Version"),
                    ResolvedPath = p.GetMetadata("ResolvedPath"),
                })
                .ToArray();
            
            var (result, packageFilename) = new Consolidate(new MsBuildTaskLog(Log)).Execute(OutputDirectory, packageReferences);
            ConsolidatedPackageFilename = packageFilename;
            return result;
        }

     
    }
}