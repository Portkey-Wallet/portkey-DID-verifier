using CAVerifierServer.MongoDB;
using Xunit;

namespace CAVerifierServer;

[CollectionDefinition(CAVerifierServerTestConsts.CollectionDefinitionName)]
public class CAVerifierServerApplicationCollection : CAVerifierServerMongoDbCollectionFixtureBase
{
   
}
