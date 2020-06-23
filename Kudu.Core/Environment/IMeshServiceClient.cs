using System.Threading.Tasks;

namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    public interface IMeshServiceClient
    {
        Task MountCifs(string connectionString, string contentShare, string targetPath);
    }
}
