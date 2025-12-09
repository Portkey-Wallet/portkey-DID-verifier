using System.Threading.Tasks;

namespace CAVerifierServer.Data;

public interface ICAVerifierServerDbSchemaMigrator
{
    Task MigrateAsync();
}
