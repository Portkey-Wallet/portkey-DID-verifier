using AElf.ExceptionHandler;
using CAVerifierServer.Telegram.Options;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Timing;

namespace CAVerifierServer.Grains;
[DependsOn(typeof(CAVerifierServerApplicationContractsModule),
    typeof(AbpAutoMapperModule),
    typeof(AOPExceptionModule))]
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
        Configure<TelegramAuthOptions>(configuration.GetSection("TelegramAuth"));
        Configure<JwtTokenOptions>(configuration.GetSection("JwtToken"));
    }
}