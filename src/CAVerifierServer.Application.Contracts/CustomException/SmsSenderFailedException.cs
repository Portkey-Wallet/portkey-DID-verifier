using System;

namespace CAVerifierServer.CustomException;

public class SmsSenderFailedException : Exception
{
    
    public SmsSenderFailedException(string message) : base(message)
    {
    }
}