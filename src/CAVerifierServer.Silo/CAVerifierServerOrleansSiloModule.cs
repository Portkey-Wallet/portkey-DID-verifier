using System.IdentityModel.Tokens.Jwt;
using CAVerifierServer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CAVerifierServer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(CAVerifierServerApplicationModule),
    typeof(CAVerifierServerGrainsModule))]

public class CAVerifierServerOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<CAVerifierServerHostedService>();
        context.Services.AddScoped<JwtSecurityTokenHandler>();
    }
}