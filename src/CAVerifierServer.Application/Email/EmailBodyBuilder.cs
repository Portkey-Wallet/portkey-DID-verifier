using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAVerifierServer.Email;

public class EmailBodyBuilder
{
    public static string BuildBodyTemplateWithOperationDetails(string verifierName, string image, string portkey,
        string verifyCode, string showOperationDetails)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: left"">
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px""
        >
          <img
            src=""{image}""
            style=""
              width: 32px;
              height: 32px;
              border-radius: 50%;
              margin-right: 8px;
            ""
          />
          <div
            style=""
              color: rgba(37, 39, 42, 1);
              font-size: 18px;
              font-weight: 500;
              font-family: 'Roboto';
            ""
          >
            {verifierName}
          </div>
        </div>
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 500;
          font-family: 'Roboto';
        ""
      >
        {verifierName} & {portkey} Verification Code
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          text-align: left;
          font-weight: 400;
          font-family: 'Roboto';
        ""
      >
        <div style=""margin: 16px 0"">
          In order to verify this email, please enter the 6-digit verification
          code:
        </div>
        <div
          style=""
            text-align: left;
            font-weight: 500;
            font-family: 'Roboto';
            text-decoration: underline;
            color: rgba(37, 39, 42, 1);
            font-size: 32px;
          ""
        >
          
          {verifyCode}
        </div>
        <div
          style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-family: 'Roboto';
          ""
        >
        <div style=""margin: 32px 0 16px 0"">
          Verification details are as follows. Proceed only if all data matches:
        </div>
          <div style="" width:100%; background: rgba(245, 246, 247); margin: 8px 0 24px 0; padding: 20px ; border-radius: 6px; "">
         {HandleShowOperationDetailsJson(showOperationDetails)}
          </div>

        </div>      
        <div style=""margin: 32px 0 16px 0"">
          This verification code will expire in 10 mins, please complete the
          verification as soon as possible.
        </div>
        <div>If you did not request this, please ignore this email.</div>

      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 600;
          font-family: 'Roboto';
        ""
      >
        PORTKEY Team
      </div>

        <div style="" width:100%; background: rgba(245, 246, 247); margin: 32px 0 16px 0 ; padding: 20px ; "">
        <div text-align: left;>
          <font  color='#979AA1';>Community</font>
        </div>
        
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px ; ""
        >
          {HandleCommunityList()}
        </div>
        <div>
          <a href=https://portkey.finance/terms-of-service><font color='5B8EF4' >Term of Service</font></a><span style='margin: 0 8px;color: #979AA1'> | </span><a href=https://portkey.finance/privacy-policy><font color='5B8EF4';>Privacy Policy</font>
        </div>


      </div>
    </div>
";
    }


    private static string HandleShowOperationDetailsJson(string json)
    {
        try
        {
            JsonConvert.DeserializeObject(json);
            var jsonObj = JObject.Parse(json);
            var concat = "";
            foreach (var child in jsonObj.Children())
            {
                if (child is not JProperty property)
                {
                    continue;
                }

                var value = property.Value.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var fontStr =
                    "<div  style='margin-bottom: 0; color: #979AA1; flex: 2 ; margin-right: 32px ; font-weight: 300;'>" +
                    property.Name + "</div>";
                var valueStr = "<div   style='flex: 3;'>" + property.Value + "</div>";

                var divWrap =
                    $@" <div style='text-align:left; width: 500px; margin: left auto; display: flex ; margin-bottom: 10px' >
                            {fontStr}                
                            {valueStr}
                        </div>
                      ";
                concat += divWrap;
            }

            return concat;
        }
        catch (Exception e)
        {
            return "";
        }
    }

    private static string HandleCommunityList()
    {
        try
        {
            var map = new Dictionary<string, string>();
            map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/medium.png",
                "https://medium.com/@PortkeyDID");
            map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/youtube.png",
                "https://www.youtube.com/@PortkeyDID");
            map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/telegram.png",
                "https://t.me/Portkey_Official_Group");
            map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/Twitter%2BX.png",
                "https://twitter.com/Portkey_DID");
            // map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/discord.png",
            //     "https://discord.gg/EUBq3rHQhr");
            map.Add("https://portkey-did.s3.ap-northeast-1.amazonaws.com/MediaIcons/github.png",
                "https://github.com/Portkey-Wallet");


            var result = map.Keys
                .Select(key =>
                    "<a href=" + map[key] + " style='margin-right: 24px'>" + "<img src='" + key +
                    "'; style='width: 24px; height: 24px;' /></a>")
                .Aggregate("", (current, communityItem) => current + communityItem);
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public static string BuildTransactionTemplateBeforeApproval(string verifierName, string image, string portkey,
        string showOperationDetails)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: left"">
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px""
        >
          <img
            src=""{image}""
            style=""
              width: 32px;
              height: 32px;
              border-radius: 50%;
              margin-right: 8px;
            ""
          />
          <div
            style=""
              color: rgba(37, 39, 42, 1);
              font-size: 18px;
              font-weight: 500;
              font-family: 'Roboto';
            ""
          >
            {verifierName}
          </div>
        </div>
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 500;
          font-family: 'Roboto';
        ""
      >
        {verifierName} & {portkey}
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          text-align: left;
          font-weight: 400;
          font-family: 'Roboto';
        ""
      >
       
        <div
          style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-family: 'Roboto';
          ""
        >
        <div style=""margin: 32px 0 16px 0"">
          You're approving a transaction, the details of which are as follows. Proceed only if all data matches:
        </div>
          <div style="" width:100%; background: rgba(245, 246, 247); margin: 8px 0 24px 0; padding: 20px ; border-radius: 6px; "">
         {HandleShowOperationDetailsJson(showOperationDetails)}
          </div>

        </div>      
        <div style=""margin: 32px 0 16px 0"">
          If the verification message in the app differs from what you see here, please exercise caution and refrain from approving the transaction.
        </div>

      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 600;
          font-family: 'Roboto';
        ""
      >
        PORTKEY Team
      </div>

        <div style="" width:100%; background: rgba(245, 246, 247); margin: 32px 0 16px 0 ; padding: 20px ; "">
        <div text-align: left;>
          <font  color='#979AA1';>Community</font>
        </div>
        
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px ; ""
        >
          {HandleCommunityList()}
        </div>
        <div>
          <a href=https://portkey.finance/terms-of-service><font color='5B8EF4' >Term of Service</font></a><span style='margin: 0 8px;color: #979AA1'> | </span><a href=https://portkey.finance/privacy-policy><font color='5B8EF4';>Privacy Policy</font>
        </div>


        </div>
      </div>
      ";
    }
    
    public static string BuildTransactionTemplateAfterApproval(string verifierName, string image, string portkey,
        string showOperationDetails)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: left"">
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px""
        >
          <img
            src=""{image}""
            style=""
              width: 32px;
              height: 32px;
              border-radius: 50%;
              margin-right: 8px;
            ""
          />
          <div
            style=""
              color: rgba(37, 39, 42, 1);
              font-size: 18px;
              font-weight: 500;
              font-family: 'Roboto';
            ""
          >
            {verifierName}
          </div>
        </div>
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 500;
          font-family: 'Roboto';
        ""
      >
        {verifierName} & {portkey}
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          text-align: left;
          font-weight: 400;
          font-family: 'Roboto';
        ""
      >
       
        <div
          style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-family: 'Roboto';
          ""
        >
        <div style=""margin: 32px 0 16px 0"">
          The transaction is approved. You can view the verification details below:
        </div>
          <div style="" width:100%; background: rgba(245, 246, 247); margin: 8px 0 24px 0; padding: 20px ; border-radius: 6px; "">
         {HandleShowOperationDetailsJson(showOperationDetails)}
          </div>

        </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 600;
          font-family: 'Roboto';
        ""
      >
        PORTKEY Team
      </div>

        <div style="" width:100%; background: rgba(245, 246, 247); margin: 32px 0 16px 0 ; padding: 20px ; "">
        <div text-align: left;>
          <font  color='#979AA1';>Community</font>
        </div>
        
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px ; ""
        >
          {HandleCommunityList()}
        </div>
        <div>
          <a href=https://portkey.finance/terms-of-service><font color='5B8EF4' >Term of Service</font></a><span style='margin: 0 8px;color: #979AA1'> | </span><a href=https://portkey.finance/privacy-policy><font color='5B8EF4';>Privacy Policy</font>
        </div>


        </div>
      </div>
      ";
    }
    
    public static string BuildBodyTemplateForSecondaryEmail(string verifierName, string image, string portkey, string verifyCode)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: left"">
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px""
        >
          <img
            src=""{image}""
            style=""
              width: 32px;
              height: 32px;
              border-radius: 50%;
              margin-right: 8px;
            ""
          />
          <div
            style=""
              color: rgba(37, 39, 42, 1);
              font-size: 18px;
              font-weight: 500;
              font-family: 'Roboto';
            ""
          >
            {verifierName}
          </div>
        </div>
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 500;
          font-family: 'Roboto';
        ""
      >
        {verifierName} & {portkey} Verification Code
      </div>
      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          text-align: left;
          font-weight: 400;
          font-family: 'Roboto';
        ""
      >
        <div style=""margin: 16px 0"">
          Use this code to complete the verification process:
        </div>
        <div
          style=""
            text-align: left;
            font-weight: 500;
            font-family: 'Roboto';
            text-decoration: underline;
            color: rgba(37, 39, 42, 1);
            font-size: 32px;
          ""
        >
          
          {verifyCode}
        </div>
        <div style=""margin: 32px 0 16px 0"">
          This verification code will expire in 10 mins, please complete the
          verification as soon as possible.
        </div>
        <div>If you did not request this, please ignore this email.</div>

      <div
        style=""
          color: rgba(37, 39, 42, 1);
          font-size: 16px;
          margin: 38px 0 18px 0;
          text-align: left;
          font-weight: 600;
          font-family: 'Roboto';
        ""
      >
        PORTKEY Team
      </div>

        <div style="" width:100%; background: rgba(245, 246, 247); margin: 32px 0 16px 0 ; padding: 20px ; "">
        <div text-align: left;>
          <font  color='#979AA1';>Community</font>
        </div>
        
        <div
          style=""display: inline-flex; align-items: left; margin-top: 16px ; ""
        >
          {HandleCommunityList()}
        </div>
        <div>
          <a href=https://portkey.finance/terms-of-service><font color='5B8EF4' >Term of Service</font></a><span style='margin: 0 8px;color: #979AA1'> | </span><a href=https://portkey.finance/privacy-policy><font color='5B8EF4';>Privacy Policy</font>
        </div>


      </div>
    </div>
";
    }
}