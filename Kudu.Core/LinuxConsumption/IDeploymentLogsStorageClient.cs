using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kudu.Core.LinuxConsumption
{
    public interface IDeploymentLogsStorageClient
    {
        Task<int> UploadFiles(string containerName, Dictionary<string, string> filesToCopy);

        Task<IEnumerable<string>> GetDeploymentIds();
    }
}
