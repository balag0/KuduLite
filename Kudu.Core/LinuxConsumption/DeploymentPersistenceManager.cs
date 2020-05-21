using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kudu.Core.Deployment;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;
using Microsoft.Azure.Storage.Blob;

namespace Kudu.Core.LinuxConsumption
{
    public class DeploymentPersistenceManager : IDeploymentPersistenceManager
    {
        private const string ZipDeployLogsContainerName = "kudu-zipdeploy-logs";

        private readonly IEnvironment _environment;
        private readonly IDeploymentLogsStorageClient _storageClient;

        private readonly HashSet<string> _logFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DeploymentManager.XmlLogFile,
            DeploymentManager.TextLogFile,
            DeploymentStatusFile.DeploymentStatusFileName()
        };

        public DeploymentPersistenceManager(IEnvironment environment,
            IDeploymentLogsStorageClient storageClient)
        {
            _environment = environment;
            _storageClient = storageClient;
        }

        public async Task Persist(string deploymentId)
        {
            KuduEventGenerator.Log().GenericEvent(ServerConfiguration.GetApplicationName(),
                $"Persisting logs for deployment {deploymentId}", string.Empty, string.Empty, string.Empty,
                string.Empty);

            var filesToCopy = GetDeploymentLogFiles(_environment.DeploymentsPath, deploymentId);

            int totalFiles = filesToCopy.Count;
            int successCount = 0;

            if (filesToCopy.Any())
            {
                successCount = await _storageClient.UploadFiles(ZipDeployLogsContainerName, filesToCopy);
            }

            KuduEventGenerator.Log().GenericEvent(ServerConfiguration.GetApplicationName(),
                $"Persisting logs for deployment {deploymentId} Total = {totalFiles} Success = {successCount}",
                string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public async Task<IEnumerable<DeployResult>> GetDeployments()
        {
            var deployResults = new List<DeployResult>();
            var deploymentIds = await _storageClient.GetDeploymentIds();
            foreach (var deploymentId in deploymentIds)
            {
                deployResults.Add(new DeployResult
                {
                    Id = deploymentId
                });
            }

            return deployResults;
        }

        public async Task Download()
        {
            // Download the logs from storage to deployments folder.
            // Other apis should work as is
            // First get all the deployments
        }



        private Dictionary<string, string> GetDeploymentLogFiles(string sourceDir, string deploymentId)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var deploymentFolder = Directory.EnumerateDirectories(sourceDir, $"*{deploymentId}*",
                SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (deploymentFolder != null)
            {
                foreach (var fileName in Directory.EnumerateFiles(deploymentFolder))
                {
                    if (_logFileNames.Contains(Path.GetFileName(fileName)))
                    {
                        // logFiles.Add(fileName);

                        var remoteFileName = Path.Combine(deploymentId, Path.GetFileName(fileName));
                        if (!string.IsNullOrEmpty(remoteFileName))
                        {
                            dictionary.TryAdd(fileName, remoteFileName);
                        }
                    }
                }
            }

            return dictionary;
        }

    }
}
