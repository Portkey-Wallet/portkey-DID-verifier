using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Identity;
using Xunit;

namespace CAVerifierServer.Samples;

/* This is just an example test class.
 * Normally, you don't test code of the modules you are using
 * (like IIdentityUserAppService here).
 * Only test your own application services.
 */
[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public class SampleAppServiceTests : CAVerifierServerApplicationTestBase
{
    private readonly IIdentityUserAppService _userAppService;

    public SampleAppServiceTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
    }

    [Fact]
    public async Task Initial_Data_Should_Contain_Admin_User()
    {
        //Act
        // var result = await _userAppService.GetListAsync(new GetIdentityUsersInput());
        //
        // //Assert
        // result.TotalCount.ShouldBeGreaterThan(0);
        // result.Items.ShouldContain(u => u.UserName == "admin");
    }

}