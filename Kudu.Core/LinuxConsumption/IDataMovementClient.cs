using System;
using System.IO;
using System.Threading.Tasks;
using Kudu.Core.Deployment;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace Kudu.Core.LinuxConsumption
{
    public interface IDataMovementClient
    {
        Task<TransferStatus> UploadDirectory(string sourceDir, string deploymentId, string destBlobName);
    }

    public class DataMovementClient : IDataMovementClient
    {
        private const int MaxRetries = 3;

        private readonly IEnvironment _environment;

        public DataMovementClient(IEnvironment environment)
        {
            _environment = environment;
        }

        private static void EnumDirs(string dir)
        {
            foreach (var enumerateFileSystemEntry in Directory.EnumerateFileSystemEntries(dir))
            {
                Console.WriteLine(enumerateFileSystemEntry);
            }
        }

        public async Task<TransferStatus> UploadDirectory(string sourceDir, string deploymentId, string destBlobName)
        {
            EnumDirs(sourceDir);

            var cloudBlobDirectory = await GetBlobDirectory(_environment.AzureWebJobsStorage, destBlobName);

            var attempt = 0;
            while (true)
            {
                try
                {
                    return await TransferLocalDirectoryToAzureBlobDirectory(cloudBlobDirectory, sourceDir, deploymentId);
                }
                catch (Exception)
                {
                    if (++attempt > MaxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        private static async Task<CloudBlobDirectory> GetBlobDirectory(string connectionString, string blobName)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(blobName);
            await container.CreateIfNotExistsAsync();

            // passing empty string means we get a reference to the dir representing the whole container
            return container.GetDirectoryReference("");
        }

        private async Task<TransferStatus> TransferLocalDirectoryToAzureBlobDirectory(CloudBlobDirectory blobDirectory, string localDirectoryPath, string deploymentId)
        {
            try
            {
                Task<bool> TransferFilter(object sourceFileName, object destination)
                {
                    var s = sourceFileName as string;
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (!s.Contains(deploymentId, StringComparison.OrdinalIgnoreCase))
                        {
                            return Task.FromResult(false);
                        }

                        if (s.EndsWith(DeploymentManager.XmlLogFile, StringComparison.OrdinalIgnoreCase) ||
                            s.EndsWith(DeploymentManager.TextLogFile, StringComparison.OrdinalIgnoreCase))
                        {
                            return Task.FromResult(true);
                        }
                    }

                    return Task.FromResult(false);
                }

                var directoryTransferContext = new DirectoryTransferContext();
                directoryTransferContext.ShouldTransferCallbackAsync = TransferFilter;

                var options = new UploadDirectoryOptions { Recursive = true };

                return await TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options,
                    directoryTransferContext);
            }
            catch (Exception e)
            {
                KuduEventGenerator.Log().KuduException(ServerConfiguration.GetApplicationName(),
                    nameof(TransferLocalDirectoryToAzureBlobDirectory), string.Empty, string.Empty,
                    "Failed to upload deployment logs", e.ToString());
                throw;
            }
        }
    }
}
