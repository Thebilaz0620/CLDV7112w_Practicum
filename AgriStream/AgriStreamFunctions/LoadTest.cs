using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace AgriStreamFunctions
{
    public class LoadTest
    {
        public static async Task RunTest()
        {
            using HttpClient client = new HttpClient();

            string url =
                "http://localhost:7039/api/telemetry/Farm001";

            int totalRequests = 100;
            int successful = 0;
            int failed = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            var tasks = new Task[totalRequests];

            for (int i = 0; i < totalRequests; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        var response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            System.Threading.Interlocked.Increment(
                                ref successful);
                        }
                        else
                        {
                            System.Threading.Interlocked.Increment(
                                ref failed);
                        }
                    }
                    catch
                    {
                        System.Threading.Interlocked.Increment(
                            ref failed);
                    }
                });
            }

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            Console.WriteLine("LOAD TEST RESULTS");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Total Requests: {totalRequests}");
            Console.WriteLine($"Successful: {successful}");
            Console.WriteLine($"Failed: {failed}");
            Console.WriteLine($"Duration: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}