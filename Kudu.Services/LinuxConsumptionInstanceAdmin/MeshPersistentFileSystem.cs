using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    /// <summary>
    /// Manages access to File shares for Linux consumption IMeshPersistentFileSystem
    /// </summary>
    public class MeshPersistentFileSystem : IMeshPersistentFileSystem
    {
        private const string FileShareFormat = "{0}-{1}";

        private readonly ISystemEnvironment _environment;
        private readonly IMeshServiceClient _meshServiceClient;

        private bool _fileShareMounted;
        private string _fileShareMountMessage;

        public MeshPersistentFileSystem(ISystemEnvironment environment, IMeshServiceClient meshServiceClient)
        {
            _fileShareMounted = false;
            _fileShareMountMessage = string.Empty;
            _environment = environment;
            _meshServiceClient = meshServiceClient;
        }

        private bool IsPersistentLogsEnabled()
        {
            var persistentLogsEnabled = _environment.GetEnvironmentVariable(Constants.EnablePersistentLogs);
            if (!string.IsNullOrWhiteSpace(persistentLogsEnabled))
            {
                return string.Equals("1", persistentLogsEnabled, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals("true", persistentLogsEnabled, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private bool TryGetStorageConnectionString(out string connectionString)
        {
            connectionString = _environment.GetEnvironmentVariable(Constants.AzureWebJobsStorage);
            return !string.IsNullOrWhiteSpace(connectionString);
        }

        private bool IsLinuxConsumption()
        {
            return !string.IsNullOrEmpty(_environment.GetEnvironmentVariable(Constants.ContainerName));
        }

        private bool IsKuduShareMounted()
        {
            return _fileShareMounted;
        }

        private void UpdateStatus(bool status, string message)
        {
            _fileShareMounted = status;
            _fileShareMountMessage = message;
        }

        /// <summary>
        /// Mounts file share
        /// </summary>
        /// <returns></returns>
        public async Task MountFileShare()
        {
            var siteName = ServerConfiguration.GetApplicationName();

            if (IsKuduShareMounted())
            {
                const string message = "Kudu file share mounted already";
                UpdateStatus(true, message);
                KuduEventGenerator.Log().LogMessage(EventLevel.Warning, siteName, nameof(MountFileShare), message);
                return;
            }

            if (!IsLinuxConsumption())
            {
                const string message =
                    "Mounting kudu file share is only supported on Linux consumption environment";
                UpdateStatus(false, message);
                KuduEventGenerator.Log().LogMessage(EventLevel.Warning, siteName, nameof(MountFileShare), message);
                return;
            }

            if (!IsPersistentLogsEnabled())
            {
                const string message = "Kudu file share was not mounted since persistent storage is disabled";
                UpdateStatus(false, message);
                KuduEventGenerator.Log().LogMessage(EventLevel.Warning, siteName, nameof(MountFileShare), message);
                return;
            }

            if (!TryGetStorageConnectionString(out var connectionString))
            {
                var message = $"Kudu file share was not mounted since {nameof(Constants.AzureWebJobsStorage)} is empty";
                UpdateStatus(false, message);
                KuduEventGenerator.Log().LogMessage(EventLevel.Warning, siteName, nameof(MountFileShare), message);
                return;
            }

            var errorMessage = await MountKuduFileShare(siteName, connectionString);
            var mountResult = string.IsNullOrEmpty(errorMessage);

            UpdateStatus(mountResult, errorMessage);
            KuduEventGenerator.Log().LogMessage(EventLevel.Informational, siteName,
                $"Mounting Kudu file share result: {mountResult}", string.Empty);
        }

        public bool GetStatus(out string message)
        {
            message = _fileShareMountMessage;
            return _fileShareMounted;
        }

        private async Task<string> MountKuduFileShare(string siteName, string connectionString)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var fileClient = storageAccount.CreateCloudFileClient();
                var fileShareName = string.Format(FileShareFormat, Constants.KuduFileSharePrefix,
                    ServerConfiguration.GetApplicationName().ToLowerInvariant());

                // Get a reference to the file share we created previously.
                CloudFileShare share = fileClient.GetShareReference(fileShareName);

                var fileRequestOptions = new FileRequestOptions
                {
                    RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 5),
                    MaximumExecutionTime = TimeSpan.FromSeconds(30)
                };

                KuduEventGenerator.Log().LogMessage(EventLevel.Informational, siteName,
                    $"Creating Kudu mount file share {fileShareName}", string.Empty);

                await share.CreateIfNotExistsAsync(fileRequestOptions, new OperationContext());

                KuduEventGenerator.Log().LogMessage(EventLevel.Informational, siteName,
                    $"Mounting Kudu mount file share {fileShareName} at {Constants.KuduFileShareMountPath}",
                    string.Empty);

                await _meshServiceClient.MountCifs(connectionString, fileShareName, Constants.KuduFileShareMountPath);

                return string.Empty;
            }
            catch (Exception e)
            {
                var message = e.ToString();
                KuduEventGenerator.Log().LogMessage(EventLevel.Warning, siteName, nameof(MountKuduFileShare), message);
                return message;
            }
        }
    }
}
