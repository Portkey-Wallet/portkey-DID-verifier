using Orleans.TestingHost;

namespace CAVerifierServer.Grain.Tests;

public class CAVerifierServerGrainTestBase :CAVerifierServerTestBase<CAVerifierServerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public CAVerifierServerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}