namespace GitPulse.Api.Services;

public interface ISnapshotService
{
    Task UpdateSnapshotsForUserAsync(int userId);
}
