namespace CAVerifierServer.Account;

public class TwitterUserInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public bool Verified { get; set; }
}
public class TwitterUserInfoDto
{
    public TwitterUserInfo Data { get; set; }
}