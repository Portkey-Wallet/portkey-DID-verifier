using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer;

[Dependency(ReplaceServices = true)]
public class CAVerifierServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "CAVerifierServer";
}
