using System.Linq;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Rabbitcs.Api.V1.Dtos;
using Rabbitcs.Infra;
using RabbitHole.Api;

namespace Rabbitcs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // PostgreSQL Context
            services.AddDbContext<SqlContext>(options => options.UseNpgsql(Configuration.GetValue<string>("ConnectionStrings:Postgres")));

            // Controllers with NewtonsoftJson setup rather than the new default System.Text
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                // Ignore reference loop (in case of db model serialization used on simplified routes)
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                // camelCasing for serialized JSONs
                options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

                // Enum string serialization
                options.SerializerSettings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
            });

            // RabbitMQ Client
            services.AddSingleton(Configuration.GetSection("RabbitMQ").Get<ConfigurationRabbitMQ>());
            services.AddSingleton<IRabbitBus, RabbitBus>();

            // Fluent validation for incoming DTOs
            services.AddMvc().AddFluentValidation();
            services.AddTransient<IValidator<OrderRequest>, OrderRequestValidator>();
            services.AddSingleton<IRabbitBus, RabbitBus>();

            // AutoMapper for DTO to domain and vice-versa mappings
            services.AddAutoMapper(typeof(Startup));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (!env.IsEnvironment("Production"))
                app.UseDeveloperExceptionPage();
            else
                app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            Migrate(app, logger, executeSeedDb: env.IsEnvironment("Local"));
        }
        public static void Migrate(IApplicationBuilder app, ILogger<Startup> logger, bool executeSeedDb = false)
        {
            using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<SqlContext>();

            // always execute possible missing migrations
            if (context.Database.GetPendingMigrations().ToList().Any())
            {
                logger.LogInformation("Applying migrations...");
                context.Database.Migrate();
            }

            // seeding DB only when asked
            if (!executeSeedDb) return;

            logger.LogInformation("Seeding the database...");
        }
    }
}
