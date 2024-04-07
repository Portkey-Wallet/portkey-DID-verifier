using System.Collections.Generic;
using CAVerifierServer.Grain.Tests;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Sms;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerApplicationModule),
    typeof(CAVerifierServerDomainTestModule),
    typeof(CAVerifierServerGrainTestModule)
)]
public class CAVerifierServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<VerifierAccountOptions>(o =>
        {
            o.PrivateKey = "XXXXXXXX";
            o.Address = "XXXXXXXXX";
        });

        //context.Services.AddSingleton<IEmailSender, NullEmailSender>();
        context.Services.AddSingleton<ISmsSender, NullSmsSender>();

        context.Services.Configure<VerifierInfoOptions>(o =>
        {
            o.Name = "Verifier-001";
            o.CaServerUrl = "http://127.0.0.1:5577";
        });


        var chainInfo = new ChainInfo
        {
            ChainId = "AELF",
            BaseUrl = "http://127.0.0.1:8000",
            ContractAddress = "XXXXXX",
            IsMainChain = true,
            PrivateKey = "XXXXXXX"
        };
        var dic = new Dictionary<string, ChainInfo>();
        dic.Add("MockChainId", chainInfo);
        context.Services.Configure<ChainOptions>(o => { o.ChainInfos = dic; });
        base.ConfigureServices(context);
    }
}