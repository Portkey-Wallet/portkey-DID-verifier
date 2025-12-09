using System;
using System.Collections.Generic;
using System.Text;
using CAVerifierServer.Localization;
using Volo.Abp.Application.Services;

namespace CAVerifierServer;

/* Inherit your application services from this class.
 */
public abstract class CAVerifierServerAppService : ApplicationService
{
    protected CAVerifierServerAppService()
    {
        LocalizationResource = typeof(CAVerifierServerResource);
    }
}
