using System.Diagnostics;

namespace OutdatedDev.Middlewares
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        //private readonly RequestDelegate _previous;

        public RequestTimingMiddleware(RequestDelegate next)
        {
            _next = next;
            //_previous = previous;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //var stopwatch = Stopwatch.StartNew();
            ////await _previous(context);
            ////stopwatch.Stop();
            ////Console.WriteLine($"Previous middleware took {stopwatch.ElapsedMilliseconds} ms");
            ////stopwatch.Restart();
            //await _next(context);
            //stopwatch.Stop();
            //// Add the execution time to the response headers
            //// (Note: To strictly modify headers safely in .NET, it's better to use context.Response.OnStarting, 
            //// but this simplified version illustrates the flow).
            //context.Response.Headers.Append("X-Processing-Time-ms", stopwatch.ElapsedMilliseconds.ToString());

            //why this exception happened?
            //due to performance optimization , .NET locks the response headers right before they are sent to the client. and starts streaming immediately


            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // 1. Register a callback that fires EXACTLY before the headers are sent
            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();

                // Now it is safe to add the header, because we caught it right before they locked
                context.Response.Headers.Append("X-Processing-Time-ms", stopwatch.ElapsedMilliseconds.ToString());

                return Task.CompletedTask;
            });

            // 2. Call the next middleware in the pipeline
            await _next(context);

            // Notice we don't put the stopwatch.Stop() here anymore!
        }
    }
}
