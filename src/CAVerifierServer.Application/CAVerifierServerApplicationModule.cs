using AElf.ExceptionHandler;
using CAVerifierServer.Email;
using CAVerifierServer.Grains;
using CAVerifierServer.Options;
using CAVerifierServer.Phone;
using CAVerifierServer.VerifyCodeSender;
using CAVerifierServer.VerifyRevokeCode;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerDomainModule),
    typeof(CAVerifierServerApplicationContractsModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(CAVerifierServerGrainsModule),
    typeof(AOPExceptionModule)
)]
public class CAVerifierServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAVerifierServerApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<WhiteListExpireTimeOptions>(configuration.GetSection("WhiteListExpireTime"));
        Configure<VerifierInfoOptions>(configuration.GetSection("VerifierInfo"));
        Configure<AwsEmailOptions>(configuration.GetSection("awsEmail"));
        Configure<AwssmsMessageOptions>(configuration.GetSection("AWSSMSMessage"));
        Configure<SmsServiceOptions>(configuration.GetSection("SmsService"));
        Configure<TelesignSMSMessageOptions>(configuration.GetSection("TelesignSMSMessage"));
        Configure<SMSTemplateOptions>(configuration.GetSection("SMSTemplate"));
        Configure<MobileCountryRegularCategoryOptions>(configuration.GetSection("MobileCountryRegularCategory"));
        Configure<TwilioSmsMessageOptions>(configuration.GetSection("TwilioSmsMessage"));
        Configure<FacebookOptions>(configuration.GetSection("Facebook"));
        
        context.Services.AddSingleton<IEmailSender, AwsEmailSender>();
        context.Services.AddSingleton<ISMSServiceSender,AwsSmsMessageSender>();
        context.Services.AddSingleton<ISMSServiceSender, TelesignSmsMessageSender>();
        context.Services.AddSingleton<ISMSServiceSender, TwilioSmsMessageSender>();
        context.Services.AddSingleton<IVerifyCodeSender, EmailVerifyCodeSender>();
        context.Services.AddSingleton<IVerifyCodeSender, PhoneVerifyCodeSender>();
        
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, EmailRevokeCodeValidator>();
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, FaceBookRevokeCodeValidator>();
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, TwitterRevokeCodeValidator>();
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, TelegramRevokeCodeValidator>();
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, AppleRevokeCodeValidator>();
        context.Services.AddSingleton<IVerifyRevokeCodeValidator, GoogleRevokeCodeValidator>();
    
        context.Services.AddHttpClient();
    }
}