using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitExercises.Core
{
    public class AsyncHelpers
    {
        public static async Task<string> GetStringWithRetries(HttpClient client, string url, int maxTries = 3, CancellationToken token = default)
        {
            try
            {
                if(maxTries < 2)
                {
                    throw new ArgumentException($"maxTries must be at least 2. Value: {maxTries}");
                }
                var options = new RetryStrategyOptions
                {
                    MaxRetryAttempts = maxTries,
                    Delay = TimeSpan.FromSeconds(1)
                };

                var retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(options.MaxRetryAttempts, i =>
                    {
                        options.Delay = TimeSpan.FromSeconds(options.Delay.Seconds * 2);
                        return options.Delay;
                    });

                var response = await retryPolicy.ExecuteAsync(async ctx =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    return await client.SendAsync(request, token);
                }, token);

                response.EnsureSuccessStatusCode();
                // Create a method that will try to get a response from a given `url`, retrying `maxTries` number of times.
                // It should wait one second before the second try, and double the wait time before every successive retry
                // (so pauses before retries will be 1, 2, 4, 8, ... seconds).
                // * `maxTries` must be at least 2
                // * we retry if:
                //    * we get non-successful status code (outside of 200-299 range), or
                //    * HTTP call thrown an exception (like network connectivity or DNS issue)
                // * token should be able to cancel both HTTP call and the retry delay
                // * if all retries fails, the method should throw the exception of the last try
                // HINTS:
                // * `HttpClient.GetStringAsync` does not accept cancellation token (use `GetAsync` instead)
                // * you may use `EnsureSuccessStatusCode()` method

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
