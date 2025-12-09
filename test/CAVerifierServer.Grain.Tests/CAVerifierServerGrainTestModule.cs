using CAVerifierServer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace CAVerifierServer.Grain.Tests;

[DependsOn(
    typeof(CAVerifierServerGrainsModule),
    typeof(CAVerifierServerDomainTestModule),
    typeof(CAVerifierServerDomainModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class CAVerifierServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddHttpClient();
    }
}