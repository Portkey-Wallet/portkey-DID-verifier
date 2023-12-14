using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CAVerifier.Monitor;
using CAVerifier.Monitor.Logger;
using CAVerifierServer.Account;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAVerifierServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAVerification")]
[Route("api/app/account")]
public class CAVerificationController : CAVerifierServerController
{
    private readonly IAccountAppService _accountAppService;
    private readonly IIndicatorLogger _indicatorLogger;

    public CAVerificationController(IAccountAppService accountAppService, IIndicatorLogger indicatorLogger)
    {
        _accountAppService = accountAppService;
        _indicatorLogger = indicatorLogger;
    }

    [HttpPost]
    [Route("sendVerificationRequest")]
    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(SendVerificationRequestInput input)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            return await _accountAppService.SendVerificationRequestAsync(input);
        }
        catch (Exception e)
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.sendVerificationRequestFail.ToString(), (int)watcher.ElapsedMilliseconds); 
            throw e;
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.sendVerificationRequest.ToString(), (int)watcher.ElapsedMilliseconds);           
        }
    }
    
    [HttpPost]
    [Route("verifyCode")]
    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            return await _accountAppService.VerifyCodeAsync(input);
        }
        catch (Exception e)
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyCodeFail.ToString(), (int)watcher.ElapsedMilliseconds);
            throw e;
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyCode.ToString(), (int)watcher.ElapsedMilliseconds);
        }
    }

    [HttpPost("verifyGoogleToken")]
    public async Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(VerifyTokenRequestDto tokenRequestDto)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            return await _accountAppService.VerifyGoogleTokenAsync(tokenRequestDto);
        }
        catch (Exception e)
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyGoogleTokenFail.ToString(), (int) watcher.ElapsedMilliseconds);
            throw e;
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyGoogleToken.ToString(), (int) watcher.ElapsedMilliseconds);
        }
    }

    [HttpPost("verifyAppleToken")]
    public async Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(VerifyTokenRequestDto tokenRequestDto)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            return await _accountAppService.VerifyAppleTokenAsync(tokenRequestDto);
        }
        catch (Exception e)
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyAppleTokenFail.ToString(), (int) watcher.ElapsedMilliseconds);
            throw e; 
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Verifier, MonitorTarget.verifyAppleToken.ToString(), (int) watcher.ElapsedMilliseconds);
        }
    }
}