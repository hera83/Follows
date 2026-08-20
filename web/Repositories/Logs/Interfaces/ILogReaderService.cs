using web.ViewModels;

namespace web.Repositories.Logs.Interfaces
{
    public interface ILogReaderService
    {
        Task<LogFilterViewModel> GetLogsAsync(LogFilterViewModel filter, CancellationToken cancellationToken = default);
    }
}
