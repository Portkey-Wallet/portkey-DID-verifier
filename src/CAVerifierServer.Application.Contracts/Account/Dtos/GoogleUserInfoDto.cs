using Newtonsoft.Json;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class GoogleUserInfoDto
{
    [JsonProperty("id")] [Id(0)] public string Id { get; set; }
    [JsonProperty("name")] [Id(1)] public string FullName { get; set; }
    [JsonProperty("given_name")] [Id(2)] public string FirstName { get; set; }
    [JsonProperty("family_name")] [Id(3)] public string LastName { get; set; }
    [Id(4)] public string Email { get; set; }
    [JsonProperty("verified_email")] [Id(5)] public bool VerifiedEmail { get; set; }
    [Id(6)] public string Picture { get; set; }
}