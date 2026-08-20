using Microsoft.Data.Sqlite;
using web.Repositories.Logs.Interfaces;
using web.ViewModels;

namespace web.Repositories.Logs
{
    public class LogReaderService : ILogReaderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogReaderService> _logger;

        public LogReaderService(IConfiguration configuration, ILogger<LogReaderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LogFilterViewModel> GetLogsAsync(LogFilterViewModel filter, CancellationToken cancellationToken = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;
            filter.Entries = new List<LogEntryViewModel>();

            var connectionString = _configuration.GetConnectionString("LogConnection") ?? "Data Source=App_dbs/logs.db";

            try
            {
                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var tableName = await GetFirstLogTableAsync(connection, cancellationToken);
                if (tableName is null)
                {
                    return filter;
                }

                var columns = await GetColumnsAsync(connection, tableName, cancellationToken);
                var idColumn = PickColumn(columns, "Id", "id", "RowId") ?? "rowid";
                var timestampColumn = PickColumn(columns, "Timestamp", "TimeStamp", "UtcTimestamp", "Date");
                var levelColumn = PickColumn(columns, "Level", "LogLevel");
                var messageColumn = PickColumn(columns, "RenderedMessage", "Message", "MessageTemplate");
                var exceptionColumn = PickColumn(columns, "Exception");
                var propertiesColumn = PickColumn(columns, "Properties", "LogEvent");

                var where = new List<string>();
                var parameters = new List<SqliteParameter>();

                if (!string.IsNullOrWhiteSpace(filter.Level) && levelColumn is not null)
                {
                    where.Add($"{Quote(levelColumn)} = @level");
                    parameters.Add(new SqliteParameter("@level", filter.Level));
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var searchable = new[] { messageColumn, exceptionColumn, propertiesColumn }
                        .Where(column => column is not null)
                        .Select(column => $"{Quote(column!)} LIKE @search")
                        .ToList();

                    if (searchable.Count > 0)
                    {
                        where.Add("(" + string.Join(" OR ", searchable) + ")");
                        parameters.Add(new SqliteParameter("@search", $"%{filter.SearchText}%"));
                    }
                }

                if (filter.FromUtc.HasValue && timestampColumn is not null)
                {
                    where.Add($"{Quote(timestampColumn)} >= @fromUtc");
                    parameters.Add(new SqliteParameter("@fromUtc", filter.FromUtc.Value.ToString("O")));
                }

                if (filter.ToUtc.HasValue && timestampColumn is not null)
                {
                    where.Add($"{Quote(timestampColumn)} <= @toUtc");
                    parameters.Add(new SqliteParameter("@toUtc", filter.ToUtc.Value.ToString("O")));
                }

                var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
                var orderColumn = timestampColumn ?? idColumn;
                var offset = (filter.Page - 1) * filter.PageSize;

                await using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandText = $"SELECT COUNT(*) FROM {Quote(tableName)}{whereSql}";
                    countCommand.Parameters.AddRange(parameters.Select(CloneParameter).ToArray());
                    filter.TotalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
                }

                await using var command = connection.CreateCommand();
                command.CommandText = $@"
SELECT {Quote(idColumn)} AS Id,
       {(timestampColumn is null ? "NULL" : Quote(timestampColumn))} AS TimestampUtc,
       {(levelColumn is null ? "NULL" : Quote(levelColumn))} AS Level,
       {(messageColumn is null ? "NULL" : Quote(messageColumn))} AS Message,
       {(exceptionColumn is null ? "NULL" : Quote(exceptionColumn))} AS Exception,
       {(propertiesColumn is null ? "NULL" : Quote(propertiesColumn))} AS Properties
FROM {Quote(tableName)}
{whereSql}
ORDER BY {Quote(orderColumn)} DESC
LIMIT @limit OFFSET @offset";

                command.Parameters.AddRange(parameters.Select(CloneParameter).ToArray());
                command.Parameters.Add(new SqliteParameter("@limit", filter.PageSize));
                command.Parameters.Add(new SqliteParameter("@offset", offset));

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    filter.Entries.Add(new LogEntryViewModel
                    {
                        Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        TimestampUtc = TryReadDateTime(reader, 1),
                        Level = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Message = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Exception = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Properties = reader.IsDBNull(5) ? null : reader.GetString(5)
                    });
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning(ex, "Could not read logs from SQLite database");
            }

            return filter;
        }

        private static async Task<string?> GetFirstLogTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name LIMIT 1";
            return await command.ExecuteScalarAsync(cancellationToken) as string;
        }

        private static async Task<HashSet<string>> GetColumnsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({Quote(tableName)})";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(1));
            }

            return columns;
        }

        private static string? PickColumn(HashSet<string> columns, params string[] candidates)
        {
            return candidates.FirstOrDefault(columns.Contains);
        }

        private static string Quote(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        private static SqliteParameter CloneParameter(SqliteParameter parameter)
        {
            return new SqliteParameter(parameter.ParameterName, parameter.Value);
        }

        private static DateTime TryReadDateTime(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return DateTime.MinValue;
            }

            var value = reader.GetValue(ordinal);
            if (value is DateTime dateTime)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            return DateTime.TryParse(value.ToString(), out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : DateTime.MinValue;
        }
    }
}
