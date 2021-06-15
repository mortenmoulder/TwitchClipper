using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchClipper.Helpers
{
    public static class ParallelExtensions
    {
        public static async Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction, int maxDegreeOfParallelism)
        {
            var throttler = new SemaphoreSlim(initialCount: maxDegreeOfParallelism);
            var tasks = source.Select(async item =>
            {
                await throttler.WaitAsync();
                try
                {
                    await asyncAction(item).ConfigureAwait(false);
                }
                finally
                {
                    throttler.Release();
                }
            });
            await Task.WhenAll(tasks);
        }
    }
}
