using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CAVerifierServer.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAVerifierServer;

public class RealIpMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ILogger<RealIpMiddleware> _logger;
    private readonly RealIpOptions _realIpOptions;
    private readonly IAccountAppService _accountAppService;
    private const string LocalIpaddress = "127.0.0.1";

    public RealIpMiddleware(RequestDelegate requestDelegate, IOptions<RealIpOptions> realIpOptions,
        ILogger<RealIpMiddleware> logger, IAccountAppService accountAppService)
    {
        _requestDelegate = requestDelegate;
        _logger = logger;
        _realIpOptions = realIpOptions.Value;
        _accountAppService = accountAppService;
    }

    public async Task Invoke(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.ContainsKey(_realIpOptions.HeaderKey))
        {
            throw new ExternalException("Unknown ip address. no setting");
        }
        
        var ipArr = headers["X-Forwarded-For"].ToString().Split(',');
        if (ipArr.Length == 0)
        {
            _logger.LogDebug("Unknown ip address");
            throw new ExternalException("Unknown ip address. Refused visit server.ipArr is null");
        }
#if DEBUG
        if (ipArr.Contains(LocalIpaddress))
        {
            await _requestDelegate(context);
            return;
        }
#endif
        _logger.LogDebug("Received IpList is :{ipList}", headers["X-Forwarded-For"]);
        var ipList = ipArr.Select(ip => ip.Trim()).ToList();
        _logger.LogInformation("ipList count {count} :",ipList.Count);
        var caServerAddressIp = await _accountAppService.WhiteListCheckAsync(ipList);
        _logger.LogInformation("caServerAddressIp is {0}",caServerAddressIp);
        // if (string.IsNullOrEmpty(caServerAddressIp))
        // {
        //     _logger.LogDebug($"Resolve real ip is not in whiteList,Please check and retry.");
        //     throw new ExternalException("Resolve real ip is not in whiteList,Please check and retry.");
        // }
        await _requestDelegate(context);
    }
}