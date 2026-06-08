using Asp.Versioning;
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
            builder.Services.AddSwaggerGen(options           // The actual Swagger generator service
                =>
            {
                // Tell Swagger to generate a distinct JSON document for V1
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "OutdatedDev API v1",
                    Version = "v1"
                });

                // Tell Swagger to generate a distinct JSON document for V2
                options.SwaggerDoc("v2", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "OutdatedDev API v2",
                    Version = "v2"
                });
            });


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

            ///api versioning
            ///
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true; // This adds the "API-supported-versions" header to responses, which can be helpful for clients to know which versions are available.

                options.ApiVersionReader = ApiVersionReader.Combine(
                    //new QueryStringApiVersionReader("api-version"), // Read version from query string, e.g., ?api-version=1.0
                    //new HeaderApiVersionReader("X-API-Version"),   // Read version from custom header
                    //new MediaTypeApiVersionReader("v")             // Read version from media type, e.g., application/json; v=1.0
                    new UrlSegmentApiVersionReader()                    // Read version from URL segment, e.g., /v1/endpoint
                );
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Format for Swagger documentation
                options.SubstituteApiVersionInUrl = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    // Add V1 to the dropdown (Points to the V1 JSON generated above)
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OutdatedDev v1");

                    // Add V2 to the dropdown (Points to the V2 JSON generated above)
                    options.SwaggerEndpoint("/swagger/v2/swagger.json", "OutdatedDev v2");
                });
            }

            app.UseRateLimiter(); //register the rate limiter MW.

            #region minimal api versioned
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .HasApiVersion(new ApiVersion(2, 0))
                .ReportApiVersions()
                .Build();
            // V1 Endpoint Group
            var v1Group = app.MapGroup("/api/v{version:apiVersion}/products")
                             .WithApiVersionSet(versionSet)
                             .MapToApiVersion(new ApiVersion(1, 0));

            v1Group.MapGet("/", () => new[] { "Legacy Product A", "Legacy Product B" });

            // V2 Endpoint Group (The Breaking Change)
            var v2Group = app.MapGroup("/api/v{version:apiVersion}/products")
                             .WithApiVersionSet(versionSet)
                             .MapToApiVersion(new ApiVersion(2, 0));

            // Returning structured object types instead of raw strings
            v2Group.MapGet("/", () => new[] {
                new { Id = 1, Name = "Modern Product A", Price = 99.99 },
                new { Id = 2, Name = "Modern Product B", Price = 149.50 }
            });
            #endregion

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMiddleware<RequestTimingMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
