using CAVerifierServer.Grains.Common;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Timing;

namespace CAVerifierServer.Grains;
[DependsOn(typeof(CAVerifierServerApplicationContractsModule), typeof(AbpAutoMapperModule))]
public class CAVerifierServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAVerifierServerGrainsModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<VerifierCodeOptions>(configuration.GetSection("VerifierCode"));
        Configure<VerifierAccountOptions>(configuration.GetSection("verifierAccount"));
        Configure<GuardianTypeOptions>(configuration.GetSection("GuardianType"));
        Configure<AbpClockOptions>(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });
        Configure<AppleAuthOptions>(configuration.GetSection("AppleAuth"));
        context.Services.AddSingleton<ISigner, KeyStoreSigner>();
    }
}