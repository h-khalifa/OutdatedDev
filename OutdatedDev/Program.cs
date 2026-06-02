using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using OutdatedDev.Middlewares;

namespace OutdatedDev
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddEndpointsApiExplorer(); // Helps Swagger discover your endpoints
            builder.Services.AddSwaggerGen();           // The actual Swagger generator service

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false; // Remove the "Server" header for security hardening
            });

            ///adding the rate limiting service
            builder.Services.AddRateLimiter(options =>
            {
                // Policy 1: Strict fixed window for a specific heavy endpoint
                options.AddFixedWindowLimiter("StrictPolicy", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 5;
                });

                // Policy 2: Token bucket for standard API endpoints to allow bursts
                options.AddTokenBucketLimiter("StandardPolicy", opt =>
                {
                    opt.TokenLimit = 100;
                    opt.TokensPerPeriod = 10;
                    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                });

                //pro tip: customize the response when a request is rejected due to rate limiting. the defa
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRateLimiter(); //register the rate limiter MW.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMiddleware<RequestTimingMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
