using System.Threading.Tasks;
using Asp.Versioning;
using CAVerifierServer.Account;
using CAVerifierServer.Localization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace CAVerifierServer.Controllers;

/* Inherit your controllers from this class.
 */
[RemoteService]
[ControllerName("CAVerifierServer")]
[Route("/api/app/account/")]
public abstract class CAVerifierServerController : AbpController
{
   
    protected CAVerifierServerController()
    {
        LocalizationResource = typeof(CAVerifierServerResource);
    }
    
  
    
    
    

    
   
    
   
}
