using System.Collections.Generic;
using System.Threading.Tasks;
using Kudu.Core.Deployment;

namespace Kudu.Core.LinuxConsumption
{
    public class NullDeploymentPersistenceManager : IDeploymentPersistenceManager
    {
        public Task Persist(string deploymentId)
        {
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<DeployResult>> GetDeployments()
        {
            throw new System.NotImplementedException();
        }

        public async Task Download()
        {
            throw new System.NotImplementedException();
        }
    }
}