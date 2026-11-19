using System.Data.Common;
using ContosoUniversity.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.Data
{
    public class SchoolCommandInterceptorTests
    {
        private static SqlCommand NewCommand() => new SqlCommand("SELECT 1");

        private static CommandExecutedEventData ExecutedData(SqlCommand cmd, SchoolContext ctx) =>
            new CommandExecutedEventData(
                null, (d, e) => "msg", cmd.Connection, cmd, cmd.CommandText, ctx, DbCommandMethod.ExecuteReader,
                Guid.NewGuid(), Guid.NewGuid(), null, false, false,
                DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(12), CommandSource.Unknown);

        private static CommandErrorEventData ErrorData(SqlCommand cmd, SchoolContext ctx, Exception ex) =>
            new CommandErrorEventData(
                null, (d, e) => "msg", cmd.Connection, cmd, cmd.CommandText, ctx, DbCommandMethod.ExecuteReader,
                Guid.NewGuid(), Guid.NewGuid(), ex, false, false,
                DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(5), CommandSource.Unknown);

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var i = new SchoolCommandInterceptor(new ListLogger<SchoolCommandInterceptor>());
            Assert.NotNull(i);
        }

        [Fact]
        public void ReaderExecuted_LogsInformationAndReturnsResult()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            DbDataReader reader = null;
            var result = i.ReaderExecuted(cmd, ExecutedData(cmd, ctx), reader);
            Assert.Null(result);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("ReaderExecuted", entry.Message);
            Assert.Contains("SELECT 1", entry.Message);
        }

        [Fact]
        public async Task ReaderExecutedAsync_Logs()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            await i.ReaderExecutedAsync(cmd, ExecutedData(cmd, ctx), null);
            Assert.Contains("ReaderExecutedAsync", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void NonQueryExecuted_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            Assert.Equal(3, i.NonQueryExecuted(cmd, ExecutedData(cmd, ctx), 3));
            Assert.Contains("NonQueryExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public async Task NonQueryExecutedAsync_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            Assert.Equal(4, await i.NonQueryExecutedAsync(cmd, ExecutedData(cmd, ctx), 4));
            Assert.Contains("NonQueryExecutedAsync", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void ScalarExecuted_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            Assert.Equal("x", i.ScalarExecuted(cmd, ExecutedData(cmd, ctx), "x"));
            Assert.Contains("ScalarExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public async Task ScalarExecutedAsync_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            Assert.Equal("y", await i.ScalarExecutedAsync(cmd, ExecutedData(cmd, ctx), "y"));
            Assert.Contains("ScalarExecutedAsync", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void CommandFailed_LogsError()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            var ex = new InvalidOperationException("boom");
            i.CommandFailed(cmd, ErrorData(cmd, ctx, ex));
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(ex, entry.Exception);
            Assert.Contains("SELECT 1", entry.Message);
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError()
        {
            var logger = new ListLogger<SchoolCommandInterceptor>();
            var i = new SchoolCommandInterceptor(logger);
            using var ctx = TestHelpers.CreateContext();
            using var cmd = NewCommand();
            var ex = new InvalidOperationException("boom");
            await i.CommandFailedAsync(cmd, ErrorData(cmd, ctx, ex));
            Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
        }
    }
}
