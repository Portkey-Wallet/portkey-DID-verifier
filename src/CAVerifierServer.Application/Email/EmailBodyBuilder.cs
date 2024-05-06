namespace CAVerifierServer.Email;

public class EmailBodyBuilder
{
    public static string BuildBodyTemplate(string verifierName, string image, string portkey, string verifyCode)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: center"">
        <div
          style=""display: inline-flex; align-items: center; margin-top: 16px""
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
          text-align: center;
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
            text-align: center;
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
      </div>
    </div>
";
    }
    
    public static string BuildBodyTemplateWithOperationDetails(string verifierName, string image, string portkey, string verifyCode, string showOperationDetails)
    {
        return $@" <div style=""width: 550px; margin: 0 auto; background-color: rgba(255, 255, 255, 1);"">
      <div style=""text-align: center"">
        <div
          style=""display: inline-flex; align-items: center; margin-top: 16px""
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
          text-align: center;
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
            text-align: center;
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
          text-align: center;
          font-weight: 500;
          font-family: 'Roboto';
          ""
        >
          {showOperationDetails}
        </div>
        <div style=""margin: 32px 0 16px 0"">
          This verification code will expire in 10 mins, please complete the
          verification as soon as possible.
        </div>
        <div>If you did not request this, please ignore this email.</div>
      </div>
    </div>
";
    }
}