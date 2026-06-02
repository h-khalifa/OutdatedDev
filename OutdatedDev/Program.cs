using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //app.Use<RequestTimingMiddleware>();
            app.UseMiddleware<RequestTimingMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
