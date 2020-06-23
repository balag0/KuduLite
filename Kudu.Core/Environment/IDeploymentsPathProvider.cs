using Kudu.Core;
using Kudu.Services.LinuxConsumptionInstanceAdmin;

namespace Kudu.Services.Deployment
{
    public interface IDeploymentsPathProvider
    {
        string GetDeploymentsPath(IEnvironment environment, string defaultDeploymentsPath);
    }

    public class DeploymentsPathProvider : IDeploymentsPathProvider
    {
        private readonly IMeshPersistentFileSystem _persistentFileSystem;

        public DeploymentsPathProvider(IMeshPersistentFileSystem persistentFileSystem)
        {
            _persistentFileSystem = persistentFileSystem;
        }

        public string GetDeploymentsPath(IEnvironment environment, string defaultDeploymentsPath)
        {
            if (environment.IsOnLinuxConsumption)
            {
                if (_persistentFileSystem != null && _persistentFileSystem.GetStatus(out var _))
                {
                    return _persistentFileSystem.GetDeploymentsPath();
                }
            }

            return defaultDeploymentsPath;
        }
    }
}
