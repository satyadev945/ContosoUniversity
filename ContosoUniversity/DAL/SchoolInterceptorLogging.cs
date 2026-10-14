using System;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ContosoUniversity.Logging;

namespace ContosoUniversity.DAL
{
    public class SchoolInterceptorLogging : DbCommandInterceptor
    {
        private ILogger _logger = new Logger();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            _stopwatch.Restart();
            LogCommand(command, eventData);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            _stopwatch.Restart();
            LogCommand(command, eventData);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            _stopwatch.Restart();
            LogCommand(command, eventData);
            return base.ScalarExecuting(command, eventData, result);
        }

        private void LogCommand(DbCommand command, CommandEventData eventData)
        {
            _stopwatch.Stop();
            _logger.TraceApi("SQL Database", "SchoolInterceptor.CommandExecuting", _stopwatch.Elapsed, "Command: {0}: ", command.CommandText);
        }
    }
}
