using System;
using System.Text;
using Volo.Abp;

namespace CAVerifierServer;

public class RandomNumProvider
{
    private const string BASECODE = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GetCode(int length)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var rnNum = RandomHelper.GetRandom(BASECODE.Length);
            builder.Append(BASECODE[rnNum]);
        }

        return builder.ToString();
    }
}