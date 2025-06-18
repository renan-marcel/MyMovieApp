using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MyMovieApp.Infrastructure.Data;

public class QueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryInterceptor> _logger;

    public QueryInterceptor(ILogger<QueryInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<DbCommand> CommandCreating(CommandCorrelatedEventData eventData,
        InterceptionResult<DbCommand> result)
    {
        _logger.LogInformation($"Creating Command Source: {eventData.CommandSource}");
        return base.CommandCreating(eventData, result);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result)
    {
        _logger.LogInformation($"Executed SQL: {command.CommandText}");
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = new())
    {
        _logger.LogInformation($"Executed SQL (Async): {command.CommandText}");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}