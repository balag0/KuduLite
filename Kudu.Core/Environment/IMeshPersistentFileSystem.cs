using System.Threading.Tasks;

namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    public interface IMeshPersistentFileSystem
    {
        Task MountFileShare();

        bool GetStatus(out string message);

        string GetDeploymentsPath();
    }
}