using System.Threading.Tasks;

namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    public class NullMeshServiceClient : IMeshServiceClient
    {
        public Task MountCifs(string connectionString, string contentShare, string targetPath)
        {
            return Task.CompletedTask;
        }
    }
}