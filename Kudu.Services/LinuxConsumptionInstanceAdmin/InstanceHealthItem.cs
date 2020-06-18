namespace Kudu.Services.LinuxConsumptionInstanceAdmin
{
    public class InstanceHealthItem
    {
        public string Name { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}