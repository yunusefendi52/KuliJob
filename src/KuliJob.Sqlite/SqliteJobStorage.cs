using KuliJob.Internals;
using KuliJob.Storage.Data;

namespace KuliJob.Sqlite;

internal class SqliteJobStorage(
    SqliteDataSource dataSource,
    JobConfiguration configuration,
    MyClock myClock) : BaseJobStorage(dataSource, configuration, myClock)
{
}
