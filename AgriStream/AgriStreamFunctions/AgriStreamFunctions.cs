using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AgriStreamFunctions
{
    public class AgriFunctions
    {
        private readonly string connection =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

        // FUNCTION 1: BLOB TRIGGER
        [Function("BlobUploadFunction")]
        public async Task BlobUpload(
            [BlobTrigger("drone-images/{name}",
            Connection = "AzureWebJobsStorage")] Stream image,
            string name)
        {
            var table = new TableClient(
                connection, "BlobAudit");

            await table.CreateIfNotExistsAsync();

            var audit = new TableEntity("DroneImages",
                Guid.NewGuid().ToString())
            {
                ["FileName"] = name,
                ["FileSize"] = image.Length,
                ["ProcessingStatus"] = "Uploaded",
                ["UploadDate"] = DateTime.UtcNow
            };

            await table.AddEntityAsync(audit);
        }

        // FUNCTION 2: HTTP GET
        [Function("GetTelemetryFunction")]
        public async Task<HttpResponseData> GetTelemetry(
            [HttpTrigger(AuthorizationLevel.Function,
            "get", Route = "telemetry/{farmId}")]
            HttpRequestData req,
            string farmId)
        {
            var table = new TableClient(
                connection, "SensorTelemetry");

            var records = new List<TableEntity>();

            await foreach (var item in table.QueryAsync<TableEntity>(
                filter: $"PartitionKey eq '{farmId.Replace("'", "''")}'"))
            {
                records.Add(item);
            }

            var latest = records
                .OrderByDescending(x => x.Timestamp)
                .Take(20)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(latest);

            return response;
        }

        // FUNCTION 3: HTTP POST
        [Function("IrrigationCommandFunction")]
        public async Task<HttpResponseData> IrrigationCommand(
            [HttpTrigger(AuthorizationLevel.Function,
            "post", Route = "irrigation")]
            HttpRequestData req)
        {
            var command = await req.ReadFromJsonAsync<IrrigationRequest>();

            var response = req.CreateResponse();

            if (command == null ||
                string.IsNullOrWhiteSpace(command.FarmId) ||
                string.IsNullOrWhiteSpace(command.Command))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid irrigation command.");
                return response;
            }

            var queue = new QueueClient(
                connection, "irrigation-queue");

            await queue.CreateIfNotExistsAsync();

            await queue.SendMessageAsync(
                JsonSerializer.Serialize(command));

            response.StatusCode = HttpStatusCode.OK;

            await response.WriteStringAsync(
                "Irrigation command successfully queued.");

            return response;
        }

        // FUNCTION 4: QUEUE TRIGGER
        [Function("ProcessIrrigationFunction")]
        public async Task ProcessIrrigation(
            [QueueTrigger("irrigation-queue",
            Connection = "AzureWebJobsStorage")]
            string message)
        {
            var command =
                JsonSerializer.Deserialize<IrrigationRequest>(message);

            if (command == null)
                return;

            var table = new TableClient(
                connection, "IrrigationAudit");

            await table.CreateIfNotExistsAsync();

            var audit = new TableEntity(
                command.FarmId,
                Guid.NewGuid().ToString())
            {
                ["Command"] = command.Command,
                ["Status"] = "Processed",
                ["ProcessedDate"] = DateTime.UtcNow
            };

            await table.AddEntityAsync(audit);
        }
    }

    public class IrrigationRequest
    {
        public string FarmId { get; set; } = "";

        public string SensorId { get; set; } = "";

        public string Command { get; set; } = "";

        public int DurationMinutes { get; set; }
    }
}
