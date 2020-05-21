using System.Collections.Generic;
using System.Threading.Tasks;
using Kudu.Core.Deployment;

namespace Kudu.Core.LinuxConsumption
{
    public interface IDeploymentPersistenceManager
    {
        Task Persist(string deploymentId);

        Task<IEnumerable<DeployResult>> GetDeployments();

        Task Download();
    }
}