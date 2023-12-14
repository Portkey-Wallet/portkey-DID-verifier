using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace CAVerifier.Monitor;

public class CAServerMonitorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<IndicatorOptions>(context.Services.GetConfiguration().GetSection("Indicator"));
    }
}