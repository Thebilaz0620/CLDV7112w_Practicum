using Azure;
using Azure.Data.Tables;
namespace AgriStream.Services
{
    public class TelemetryStorageService
    {
        private readonly TableClient _tableClient;

        public TelemetryStorageService(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("AzureStorage");

            var serviceClient = new TableServiceClient(connectionString);

            _tableClient = serviceClient.GetTableClient("SensorTelemetry");

            _tableClient.CreateIfNotExists();

        }

        public async Task SaveTelemetryAsync(string farmId,string sensorId,double soilMoisture,double temperature)
        {
            var telemetry = new TableEntity(farmId, Guid.NewGuid().ToString())
            {
                ["sensorId"] = sensorId,
                ["soilMoisture"] = soilMoisture,
                ["temperature"] = temperature,
                ["EventType"] = "MoistureReading",
                ["Timestamp"] = DateTime.UtcNow

            };

            await _tableClient.AddEntityAsync(telemetry);
        }
    }
}
