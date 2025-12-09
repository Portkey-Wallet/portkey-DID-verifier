using CAVerifierServer.MongoDB;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerMongoDbTestModule)
    )]
public class CAVerifierServerDomainTestModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        
    }
    
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }
    
    

}
