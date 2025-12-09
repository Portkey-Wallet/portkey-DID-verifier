using CAVerifierServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace CAVerifierServer.Permissions;

public class CAVerifierServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(CAVerifierServerPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(CAVerifierServerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<CAVerifierServerResource>(name);
    }
}
