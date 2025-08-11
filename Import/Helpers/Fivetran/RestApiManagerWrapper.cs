using FivetranClient;

namespace Import.Helpers.Fivetran;

public class RestApiManagerWrapper(RestApiManager restApiManager, string groupId) : IDisposable
{
    private bool _disposed;
    public RestApiManager RestApiManager { get; } = restApiManager ?? throw new ArgumentNullException(nameof(restApiManager));
    public string GroupId { get; } = groupId ?? throw new ArgumentNullException(nameof(groupId));

    public void Dispose()
    {
        if (!_disposed)
        {
            this.RestApiManager.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}