using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

using Microsoft.Extensions.Logging;


[assembly: FunctionsStartup(typeof(MeatGeek.Sessions.Startup))]
namespace MeatGeek.Sessions
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot Configuration { get; set; }

        /// <inheritdoc />
        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddScoped<ISessionsServiceDI, SessionsServiceDI>();
            builder.Services.AddScoped<ISessionsRepository, SessionsRepository>();
            builder.Services.AddScoped<IEventGridPublisherServiceDI, EventGridPublisherServiceDI>();

        }

    }
}