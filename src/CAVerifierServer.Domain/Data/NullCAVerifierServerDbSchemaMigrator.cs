using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Data;

/* This is used if database provider does't define
 * ICAVerifierServerDbSchemaMigrator implementation.
 */
public class NullCAVerifierServerDbSchemaMigrator : ICAVerifierServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
