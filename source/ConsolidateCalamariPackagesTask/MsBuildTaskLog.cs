using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class MsBuildTaskLog : ILog
    {
        private readonly TaskLoggingHelper log;

        public MsBuildTaskLog(TaskLoggingHelper log)
        {
            this.log = log;
        }

        public void Error(string s)
            => log.LogError(s);

        public void Normal(string s)
            => log.LogMessage(s);

        public void Low(string s)
            => log.LogMessage(MessageImportance.Low, s);
    }
}