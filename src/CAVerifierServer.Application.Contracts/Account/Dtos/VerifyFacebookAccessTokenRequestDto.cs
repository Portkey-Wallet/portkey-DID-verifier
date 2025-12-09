using System.ComponentModel.DataAnnotations;

namespace CAVerifierServer.Account;

public class VerifyFacebookAccessTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    
}