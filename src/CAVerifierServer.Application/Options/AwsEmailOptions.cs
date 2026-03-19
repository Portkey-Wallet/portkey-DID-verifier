using System.Collections.Generic;

namespace CAVerifierServer.Options;

public class AwsEmailOptions
{
    // Legacy single-account fields kept for backward compatibility.
    public string From { get; set; }
    public string FromName { get; set; }
    public string SmtpUsername { get; set; }
    public string SmtpPassword { get; set; }
    public string ConfigSet { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }

    public string Image { get; set; }
    public AwsEmailSelectionMode SelectionMode { get; set; } = AwsEmailSelectionMode.RoundRobin;
    public int FailureCooldownSeconds { get; set; } = 300;
    public List<AwsEmailAccountOptions> Accounts { get; set; } = new();
}

public class AwsEmailAccountOptions
{
    public string Key { get; set; }
    public bool Enabled { get; set; } = true;
    public string From { get; set; }
    public string FromName { get; set; }
    public string SmtpUsername { get; set; }
    public string SmtpPassword { get; set; }
    public string ConfigSet { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
}

public enum AwsEmailSelectionMode
{
    RoundRobin = 0
}
