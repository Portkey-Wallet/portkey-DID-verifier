using Xunit;

namespace CAVerifierServer.Grain.Tests;

[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection:ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}