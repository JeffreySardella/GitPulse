namespace GitPulse.Api.Services;

public interface ISyncService
{
    Task SyncUserAsync(int userId);
}
