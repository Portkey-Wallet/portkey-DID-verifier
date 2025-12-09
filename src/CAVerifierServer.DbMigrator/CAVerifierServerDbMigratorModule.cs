using CAVerifierServer.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CAVerifierServer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(CAVerifierServerMongoDbModule),
    typeof(CAVerifierServerApplicationContractsModule)
    )]
public class CAVerifierServerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
      //  Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
