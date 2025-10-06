using Serilog;
using Serilog.Events;

namespace HotelReservation.API.DL.Logging
{
    public static class SerilogSetup
    {
        public static void Configure(IHostBuilder host)
        {
            host.UseSerilog((context, services, configuration) =>
            {
                // Read the entire configuration from appsettings.json
                configuration.ReadFrom.Configuration(context.Configuration);

                // You can add additional overrides or sinks here if needed
                // For example, force a console sink for development
                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);
                }
            });
        }
    }
}
