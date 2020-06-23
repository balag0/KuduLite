using System;

namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    public interface ISystemEnvironment
    {
        string GetEnvironmentVariable(string name);

        void SetEnvironmentVariable(string name, string value);
    }

    public class SystemEnvironment : ISystemEnvironment
    {
        private static readonly Lazy<SystemEnvironment> _instance = new Lazy<SystemEnvironment>(CreateInstance);

        private SystemEnvironment()
        {
        }

        public static SystemEnvironment Instance => _instance.Value;

        private static SystemEnvironment CreateInstance()
        {
            return new SystemEnvironment();
        }

        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
