using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Kudu.Core.LinuxConsumption
{
    public class DeploymentLogsStorageClient : IDeploymentLogsStorageClient
    {
        private readonly IEnvironment _environment;

        public DeploymentLogsStorageClient(IEnvironment environment)
        {
            _environment = environment;
        }

        // copies the specified 'filesToCopy' under the same directory structure in the specific 'containerName'
        public async Task<int> UploadFiles(string containerName, Dictionary<string, string> filesToCopy)
        {
            var successCount = 0;

            var cloudBlobDirectory = GetBlobDirectory(containerName);
            foreach (var fileToCopy in filesToCopy)
            {
                if (await SafeUploadFile(cloudBlobDirectory, fileToCopy.Key, fileToCopy.Value))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        private async Task<bool> SafeUploadFile(CloudBlobContainer cloudBlobDirectory, string localFile, string remoteFilePath)
        {
            try
            {
                var blockBlobReference = cloudBlobDirectory.GetBlockBlobReference(remoteFilePath);
                await InvokeWithRetries(async () => await blockBlobReference.UploadFromFileAsync(localFile), 3,
                    nameof(UploadFiles), remoteFilePath, TimeSpan.FromSeconds(1));
                return true;
            }
            catch (Exception ex)
            {
                KuduEventGenerator.Log().KuduException(ServerConfiguration.GetApplicationName(), nameof(SafeUploadFile), localFile,
                    string.Empty, $"Failed: {nameof(SafeUploadFile)}", ex.ToString());
                return false;
            }
        }

        private static async Task InvokeWithRetries(Func<Task> action, int maxRetries, string method, string file,
            TimeSpan retryInterval)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    KuduEventGenerator.Log().KuduException(ServerConfiguration.GetApplicationName(), method, file,
                        string.Empty, $"Failed: {method}", ex.ToString());
                    if (++attempt > maxRetries)
                    {
                        throw;
                    }
                    await Task.Delay(retryInterval);
                }
            }
        }

        private CloudBlobContainer GetBlobDirectory(string containerName)
        {
            var account = CloudStorageAccount.Parse(_environment.AzureWebJobsStorage);
            var blobClient = account.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            return container;
        }

        public async Task<IEnumerable<string>> GetDeploymentIds()
        {
            var deploymentIds = new List<string>();

            var currentLevel = 1;
            const int maxLevels = 1;

            var cloudBlobContainer = GetBlobDirectory(_environment.AzureWebJobsStorage);

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken = results.ContinuationToken;
                foreach (var item in results.Results)
                {
                    if (item is CloudBlobDirectory cloudBlobDirectory)
                    {
                        deploymentIds.Add(cloudBlobDirectory.Prefix.TrimEnd(new char[] { '\\', '/' }));
                    }
                }

                if (currentLevel++ > maxLevels) break;

            } while (blobContinuationToken != null); // Loop while the continuation token is not null.

            return deploymentIds;
        }
    }
}