using System;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MeatGeek.Sessions.Configurations;

[assembly: FunctionsStartup(typeof(MeatGeek.Sessions.Startup))]
namespace MeatGeek.Sessions
{
    /// <summary>
    /// This represents the entity to be invoked during the runtime startup.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot Configuration { get; set; }


        /// <inheritdoc />
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // SOURCE
            //this pattern: https://athousanddevelopers.blogspot.com/2020/05/c-azure-functions-v2-injecting.html 

            // Get the original configuration provider from the Azure Function
			var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

            // Create a new IConfigurationRoot and add our configuration along with Azure's original configuration 
			this.Configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddConfiguration(configuration) // Add the original function configuration
                .AddEnvironmentVariables()
                .Build();

			// Replace the Azure Function configuration with our new one

            builder.Services.AddSingleton<IConfiguration>(this.Configuration);
            
            //TODO: do we need?? builder.Services.AddSingleton<AppSettings>();
            this.ConfigureServices(builder.Services);

        }

        private void ConfigureServices(IServiceCollection services)
		{
        	// Add any DI services
			
            // Register the Cosmos DB client as a Singleton.
            services.AddSingleton<CosmosClient>((s) => {
                var connectionString = Configuration["CosmosDBConnection"];
                var cosmosDbConnectionString = new CosmosDbConnectionString(connectionString);
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("Please specify a value for CosmosDBConnection in the local.settings.json file or your Azure Functions Settings.");
                }
                CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDbConnectionString.ServiceEndpoint.OriginalString, cosmosDbConnectionString.AuthKey).WithBulkExecution(true);
                return configurationBuilder
                    .Build();
            });            
		}

    }
}