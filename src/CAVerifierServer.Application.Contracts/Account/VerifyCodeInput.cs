using System;

namespace CAVerifierServer.Account;

public class VerifyCodeInput
{
    public string GuardianIdentifier { get; set; }
    public Guid VerifierSessionId { get; set; }
    
    public string Code { get; set; }

    public string Salt { get; set; }

    public string GuardianIdentifierHash { get; set; }
    
    public string OperationType { get; set; }
    
    public string ChainId { get; set; }
    
    public string OperationDetails { get; set; }
}