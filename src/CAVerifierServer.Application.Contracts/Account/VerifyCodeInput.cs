using System;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class VerifyCodeInput
{
    [Id(0)] public string GuardianIdentifier { get; set; }
    [Id(1)] public Guid VerifierSessionId { get; set; }
    
    [Id(2)] public string Code { get; set; }

    [Id(3)] public string Salt { get; set; }

    [Id(4)] public string GuardianIdentifierHash { get; set; }
    
    [Id(5)] public string OperationType { get; set; }
    
    [Id(6)] public string ChainId { get; set; }
    
    [Id(7)] public string OperationDetails { get; set; }
}