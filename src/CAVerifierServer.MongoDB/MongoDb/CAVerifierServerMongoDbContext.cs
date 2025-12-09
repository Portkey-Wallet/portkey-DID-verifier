using CAVerifierServer.Account;
using MongoDB.Driver;
using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace CAVerifierServer.MongoDB;

[ConnectionStringName("Default")]
public class CAVerifierServerMongoDbContext : AbpMongoDbContext
{
   
}
