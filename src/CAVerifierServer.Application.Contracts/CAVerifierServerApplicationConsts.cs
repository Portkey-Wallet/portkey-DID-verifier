namespace CAVerifierServer;

public class CAVerifierServerApplicationConsts
{
    public const string MessageStreamName = "CAVerifierServer";
    // public const string MessageStreamNamespace = "default";
    // public const string BlockScanCheckGrainId = "BlockScanCheck";
    // public const string PrimaryKeyGrainIdSuffix = "BlockGrainPrimaryKey";
    // public const string BlockGrainIdSuffix = "BlockGrain";
    // public const string BlockDictionaryGrainIdSuffix = "BlockDictionaryGrain";
    
    public const string EmailRegex = @"[\w!#$%&'*+/=?^_`{|}~-]+(?:\.[\w!#$%&'*+/=?^_`{|}~-]+)*@(?:[\w](?:[\w-]*[\w])?\.)+[\w](?:[\w-]*[\w])?";
    public const string PORTKEY = "PORTKEY";
    public const string Subject = "Email Verification Code";
    public const string ChinaPhoneRegex = "^(\\+?0?86\\-?)?1[3456789]\\d{9}$";
    public const string PhoneNumReplacement = "$1****$2";
}