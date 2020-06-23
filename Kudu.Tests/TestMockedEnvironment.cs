using Kudu.Core;
using Kudu.Services.Deployment;
using Microsoft.AspNetCore.Http;

namespace Kudu.Tests
{
    public class TestMockedEnvironment
    {
        public static IEnvironment GetMockedEnvironment(string rootPath = "rootPath", string binPath = "binPath", string repositoryPath = "repositoryPath", string requestId = "requestId", string kuduConsoleFullPath = "kuduConsoleFullPath")
        {
            return new Environment(rootPath, binPath, repositoryPath, requestId, kuduConsoleFullPath, new HttpContextAccessor(), new DeploymentsPathProvider(null));
        }
    }
}
