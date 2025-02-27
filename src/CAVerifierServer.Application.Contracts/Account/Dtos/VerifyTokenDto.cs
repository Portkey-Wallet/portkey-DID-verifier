using System;
using Orleans;

namespace CAVerifierServer.Verifier.Dtos;

public class VerifyTokenDto<T>
{
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
    public T UserExtraInfo { get; set; }
}

[GenerateSerializer]
public class TelegramUserExtraInfo
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string UserName { get; set; }
    [Id(2)] public string AuthDate { get; set; }
    [Id(3)] public string FirstName { get; set; }
    [Id(4)] public string LastName { get; set; }
    [Id(5)] public string Hash { get; set; }
    [Id(6)] public string PhotoUrl { get; set; }
    [Id(7)] public string GuardianType { get; set; }
    [Id(8)] public DateTime AuthTime { get; set; }
    
    [Id(9)] public string BotId { get; set; }
}