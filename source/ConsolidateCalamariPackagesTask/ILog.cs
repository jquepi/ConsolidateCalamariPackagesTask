namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    internal interface ILog
    {
        void Error(string s);
        void Normal(string s);
        void Low(string s);
    }
}