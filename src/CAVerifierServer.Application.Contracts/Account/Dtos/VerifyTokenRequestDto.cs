using System.ComponentModel.DataAnnotations;

namespace CAVerifierServer.Account;

public class VerifyTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    [Required] public string IdentifierHash { get; set; }
    [Required] public string Salt { get; set; }
    [Required] public string OperationType { get; set; }
    
    public string TargetChainId { get; set; }
    
    
}