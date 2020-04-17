namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    public interface ILog
    {
        void Error(string s);
        void Normal(string s);
        void Low(string s);
    }
}