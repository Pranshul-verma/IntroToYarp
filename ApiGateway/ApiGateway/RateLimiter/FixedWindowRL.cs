using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.RateLimiter
{
    public class FixedWindowRL
    {
        private readonly RequestDelegate _next;
        private long windowSizeInSeconds;  // Size of each window in seconds
        private long maxRequestsPerWindow; // Maximum number of requests allowed per window
        private long currentWindowStart;         // Start time of the current window
        private long requestCount;               // Number of requests in the current window
        private readonly object _lock = new object();

        public FixedWindowRL(RequestDelegate next)
        {
            _next = next;
            this.windowSizeInSeconds = 60;
            this.maxRequestsPerWindow = 4;
            this.currentWindowStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.requestCount = 0;
        }

        public async Task Invoke(HttpContext context)
        {
            // Custom logic here
            Console.WriteLine("My Custom Middleware processing...");
            //var endpoint = context.GetEndpoint();
            //var useCustomMiddleware = endpoint?.Metadata.GetMetadata<RouteModel>()?.Config?.Metadata
            //    .TryGetValue("UseCustomMiddleware", out var value) == true &&
            //    bool.TryParse(value, out var result) && result;
            //  var yarpRoute = metadata?.Metadata.GetMetadata<Microsoft.YARP.Service.Proxy.IReverseProxyFeature>()?.Route;
            if (AllowRequest())
            {
                // Forward the request to the next middleware
               await _next(context);
            }
            else
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Too Many request");
            }

        }

        public bool AllowRequest()
        {
            lock (_lock)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Check if we're in a new window
                if (now - currentWindowStart >= windowSizeInSeconds)
                {
                    currentWindowStart = now;
                    requestCount = 0;
                }

                if (requestCount < maxRequestsPerWindow)
                {
                    requestCount++;
                    return true;
                }

                return false;
            }
        }
    }
}
