using System.Data;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using Microsoft.Extensions.Options;

namespace SqlTrainer.Api.Services;

public class RunnerOptions
{
    public int CommandTimeoutSeconds { get; set; } = 3;
    public int MaxRows { get; set; } = 500;
    public int PreviewRows { get; set; } = 50;
}

public interface ISqlRunnerService
{
    Task<(bool ok, bool? isCorrect, string message, string? studentJson, string? expectedJson)>
        RunAndCompareAsync(string sandboxConnStr, string seedSql, string studentSql, string expectedSql, SqlTrainer.Api.Models.TaskSqlMode mode, CancellationToken ct);

    Task<SqlTrainer.Api.Dtos.TaskSchemaResponse> BuildSchemaAsync(string sandboxConnStr, string seedSql, CancellationToken ct);

    Task<SqlTrainer.Api.Dtos.TaskPreviewResponse> BuildPreviewAsync(string sandboxConnStr, string seedSql, CancellationToken ct);
}

public class SqlRunnerService : ISqlRunnerService
{
    private static readonly SemaphoreSlim SandboxLock = new(1, 1);

    private readonly RunnerOptions _opt;

    public SqlRunnerService(IOptions<RunnerOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string EnsureDatabaseInConnStr(string sandboxConnStr)
    {
        var b = new MySqlConnectionStringBuilder(sandboxConnStr);

        if (string.IsNullOrWhiteSpace(b.Database))
            b.Database = "sqltrainer_sandbox";

        return b.ConnectionString;
    }

    private async Task<string> GetCurrentDatabaseAsync(MySqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand("SELECT DATABASE();", conn)
        {
            CommandTimeout = _opt.CommandTimeoutSeconds
        };

        var db = (await cmd.ExecuteScalarAsync(ct))?.ToString();

        return string.IsNullOrWhiteSpace(db) ? string.Empty : db;
    }

    private async Task ResetSandboxAsync(MySqlConnection conn, CancellationToken ct)
    {
        var db = await GetCurrentDatabaseAsync(conn, ct);

        if (string.IsNullOrWhiteSpace(db))
            throw new InvalidOperationException("A sandbox connection string nem tartalmaz adatbázist.");

        await ExecAsync(conn, "SET FOREIGN_KEY_CHECKS=0;", ct);

        var views = new List<string>();
        var tables = new List<string>();

        await using (var cmd = new MySqlCommand(
            "SELECT TABLE_NAME, TABLE_TYPE FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME;",
            conn))
        {
            cmd.Parameters.AddWithValue("@schema", db);
            cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

            await using var r = await cmd.ExecuteReaderAsync(ct);

            while (await r.ReadAsync(ct))
            {
                var name = r.GetString(0);
                var type = r.GetString(1);

                if (string.Equals(type, "VIEW", StringComparison.OrdinalIgnoreCase))
                    views.Add(name);
                else
                    tables.Add(name);
            }
        }

        foreach (var v in views)
            await ExecAsync(conn, $"DROP VIEW IF EXISTS `{v}`;", ct);

        foreach (var t in tables)
            await ExecAsync(conn, $"DROP TABLE IF EXISTS `{t}`;", ct);

        await ExecAsync(conn, "SET FOREIGN_KEY_CHECKS=1;", ct);
    }

    public async Task<(bool ok, bool? isCorrect, string message, string? studentJson, string? expectedJson)>
        RunAndCompareAsync(
            string sandboxConnStr,
            string seedSql,
            string studentSql,
            string expectedSql,
            SqlTrainer.Api.Models.TaskSqlMode mode,
            CancellationToken ct)
    {
        await SandboxLock.WaitAsync(ct);

        try
        {
            if (mode == SqlTrainer.Api.Models.TaskSqlMode.SelectOnly)
            {
                if (!SqlSafety.IsProbablySelectOnly(studentSql))
                    return (false, null, "Csak SELECT lekérdezés engedélyezett.", null, null);

                studentSql = SqlSafety.EnsureLimit(studentSql, _opt.MaxRows);
            }
            else
            {
                if (!SqlSafety.IsSandboxSafe(studentSql))
                    return (false, null, "A lekérdezés nem biztonságos a sandbox környezetben.", null, null);
            }

            expectedSql = SqlSafety.EnsureLimit(expectedSql, _opt.MaxRows);

            sandboxConnStr = EnsureDatabaseInConnStr(sandboxConnStr);

            await using var conn = new MySqlConnection(sandboxConnStr);
            await conn.OpenAsync(ct);

            try
            {
                await ResetSandboxAsync(conn, ct);

                if (!string.IsNullOrWhiteSpace(seedSql))
                    await ExecScriptAsync(conn, seedSql, ct);

                DataTable studentTable;

                if (mode == SqlTrainer.Api.Models.TaskSqlMode.SelectOnly)
                    studentTable = await QueryToTableAsync(conn, studentSql, ct);
                else
                    studentTable = await ExecAndMaybeSelectAsync(conn, studentSql, ct);

                await ResetSandboxAsync(conn, ct);

                if (!string.IsNullOrWhiteSpace(seedSql))
                    await ExecScriptAsync(conn, seedSql, ct);

                var expectedTable = await QueryToTableAsync(conn, expectedSql, ct);

                var studentJson = DataTableToJson(studentTable);
                var expectedJson = DataTableToJson(expectedTable);

                var isCorrect = TablesEqual(studentTable, expectedTable);

                return (
                    true,
                    isCorrect,
                    isCorrect ? "Helyes megoldás ✅" : "Eltér az elvárt eredménytől ❌",
                    studentJson,
                    expectedJson
                );
            }
            catch (MySqlException ex)
            {
                return (false, null, $"MySQL hiba: {ex.Message}", null, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Runner hiba: {ex.Message}", null, null);
            }
            finally
            {
                try
                {
                    await ResetSandboxAsync(conn, ct);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }
        finally
        {
            SandboxLock.Release();
        }
    }

    public async Task<SqlTrainer.Api.Dtos.TaskSchemaResponse> BuildSchemaAsync(
        string sandboxConnStr,
        string seedSql,
        CancellationToken ct)
    {
        await SandboxLock.WaitAsync(ct);

        try
        {
            sandboxConnStr = EnsureDatabaseInConnStr(sandboxConnStr);

            await using var conn = new MySqlConnection(sandboxConnStr);
            await conn.OpenAsync(ct);

            await ResetSandboxAsync(conn, ct);

            if (!string.IsNullOrWhiteSpace(seedSql))
                await ExecScriptAsync(conn, seedSql, ct);

            var db = await GetCurrentDatabaseAsync(conn, ct);

            if (string.IsNullOrWhiteSpace(db))
                throw new InvalidOperationException("A sandbox connection string nem tartalmaz adatbázist.");

            var tables = new List<SqlTrainer.Api.Dtos.TableSchema>();
            var tableNames = new List<string>();

            await using (var cmd = new MySqlCommand(
                "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME;",
                conn))
            {
                cmd.Parameters.AddWithValue("@schema", db);
                cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

                await using var r = await cmd.ExecuteReaderAsync(ct);

                while (await r.ReadAsync(ct))
                    tableNames.Add(r.GetString(0));
            }

            foreach (var tname in tableNames)
            {
                var keyInfo = await GetTableKeyInfoAsync(conn, db, tname, ct);
                var cols = new List<SqlTrainer.Api.Dtos.ColumnSchema>();

                await using var cmd = new MySqlCommand(
                    "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION;",
                    conn);

                cmd.Parameters.AddWithValue("@schema", db);
                cmd.Parameters.AddWithValue("@table", tname);
                cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

                await using var r = await cmd.ExecuteReaderAsync(ct);

                while (await r.ReadAsync(ct))
                {
                    var colName = r.GetString(0);
                    var dataType = r.GetString(1);
                    var isNullable = string.Equals(r.GetString(2), "YES", StringComparison.OrdinalIgnoreCase);

                    keyInfo.TryGetValue(colName, out var key);

                    cols.Add(new SqlTrainer.Api.Dtos.ColumnSchema(
                        colName,
                        dataType,
                        isNullable,
                        key.isPrimaryKey,
                        key.isForeignKey,
                        key.isIndexed
                    ));
                }

                tables.Add(new SqlTrainer.Api.Dtos.TableSchema(tname, cols));
            }

            try
            {
                await ResetSandboxAsync(conn, ct);
            }
            catch
            {
                // best-effort cleanup
            }

            return new SqlTrainer.Api.Dtos.TaskSchemaResponse(tables);
        }
        finally
        {
            SandboxLock.Release();
        }
    }

    public async Task<SqlTrainer.Api.Dtos.TaskPreviewResponse> BuildPreviewAsync(
        string sandboxConnStr,
        string seedSql,
        CancellationToken ct)
    {
        await SandboxLock.WaitAsync(ct);

        try
        {
            sandboxConnStr = EnsureDatabaseInConnStr(sandboxConnStr);

            await using var conn = new MySqlConnection(sandboxConnStr);
            await conn.OpenAsync(ct);

            await ResetSandboxAsync(conn, ct);

            if (!string.IsNullOrWhiteSpace(seedSql))
                await ExecScriptAsync(conn, seedSql, ct);

            var db = await GetCurrentDatabaseAsync(conn, ct);

            if (string.IsNullOrWhiteSpace(db))
                throw new InvalidOperationException("A sandbox connection string nem tartalmaz adatbázist.");

            var tableNames = new List<string>();

            await using (var cmd = new MySqlCommand(
                "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;",
                conn))
            {
                cmd.Parameters.AddWithValue("@schema", db);
                cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

                await using var r = await cmd.ExecuteReaderAsync(ct);

                while (await r.ReadAsync(ct))
                    tableNames.Add(r.GetString(0));
            }

            var previews = new List<SqlTrainer.Api.Dtos.TablePreview>();

            foreach (var tname in tableNames)
            {
                var keyInfo = await GetTableKeyInfoAsync(conn, db, tname, ct);
                var dt = await QueryToTableAsync(conn, $"SELECT * FROM `{tname}` LIMIT {_opt.PreviewRows};", ct);

                var cols = new List<SqlTrainer.Api.Dtos.PreviewColumn>();

                foreach (DataColumn c in dt.Columns)
                {
                    keyInfo.TryGetValue(c.ColumnName, out var key);

                    cols.Add(new SqlTrainer.Api.Dtos.PreviewColumn(
                        c.ColumnName,
                        key.isPrimaryKey,
                        key.isForeignKey,
                        key.isIndexed
                    ));
                }

                var rows = new List<Dictionary<string, object?>>();

                foreach (DataRow r in dt.Rows)
                {
                    var dict = new Dictionary<string, object?>();

                    foreach (DataColumn c in dt.Columns)
                        dict[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];

                    rows.Add(dict);
                }

                previews.Add(new SqlTrainer.Api.Dtos.TablePreview(tname, cols, rows));
            }

            try
            {
                await ResetSandboxAsync(conn, ct);
            }
            catch
            {
                // best-effort cleanup
            }

            return new SqlTrainer.Api.Dtos.TaskPreviewResponse(previews);
        }
        finally
        {
            SandboxLock.Release();
        }
    }

    private async Task<Dictionary<string, (bool isPrimaryKey, bool isForeignKey, bool isIndexed)>> GetTableKeyInfoAsync(
        MySqlConnection conn,
        string db,
        string tableName,
        CancellationToken ct)
    {
        var map = new Dictionary<string, (bool isPrimaryKey, bool isForeignKey, bool isIndexed)>(
            StringComparer.OrdinalIgnoreCase);

        await using (var cmd = new MySqlCommand(
            "SELECT COLUMN_NAME, COLUMN_KEY FROM information_schema.COLUMNS " +
            "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table;",
            conn))
        {
            cmd.Parameters.AddWithValue("@schema", db);
            cmd.Parameters.AddWithValue("@table", tableName);
            cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

            await using var r = await cmd.ExecuteReaderAsync(ct);

            while (await r.ReadAsync(ct))
            {
                var columnName = r.GetString(0);
                var columnKey = r.IsDBNull(1) ? string.Empty : r.GetString(1);

                var isPrimaryKey = string.Equals(columnKey, "PRI", StringComparison.OrdinalIgnoreCase);

                var isIndexed =
                    isPrimaryKey ||
                    string.Equals(columnKey, "MUL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(columnKey, "UNI", StringComparison.OrdinalIgnoreCase);

                map[columnName] = (isPrimaryKey, false, isIndexed);
            }
        }

        await using (var cmd = new MySqlCommand(
            "SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE " +
            "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND REFERENCED_TABLE_NAME IS NOT NULL;",
            conn))
        {
            cmd.Parameters.AddWithValue("@schema", db);
            cmd.Parameters.AddWithValue("@table", tableName);
            cmd.CommandTimeout = _opt.CommandTimeoutSeconds;

            await using var r = await cmd.ExecuteReaderAsync(ct);

            while (await r.ReadAsync(ct))
            {
                var columnName = r.GetString(0);

                map.TryGetValue(columnName, out var current);

                map[columnName] = (
                    current.isPrimaryKey,
                    true,
                    true
                );
            }
        }

        return map;
    }

    private async Task<DataTable> ExecAndMaybeSelectAsync(MySqlConnection conn, string sql, CancellationToken ct)
    {
        var statements = SplitSqlStatements(RemoveComments(sql))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (statements.Count == 0)
            return new DataTable();

        for (int i = 0; i < statements.Count - 1; i++)
        {
            await ExecAsync(conn, statements[i].EndsWith(";") ? statements[i] : statements[i] + ";", ct);
        }

        var last = statements[^1];

        if (SqlSafety.IsProbablySelectOnly(last))
        {
            last = SqlSafety.EnsureLimit(last, _opt.MaxRows);
            return await QueryToTableAsync(conn, last, ct);
        }

        await ExecAsync(conn, last.EndsWith(";") ? last : last + ";", ct);
        return new DataTable();
    }

    private async Task ExecAsync(MySqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(sql, conn)
        {
            CommandTimeout = _opt.CommandTimeoutSeconds
        };

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task ExecScriptAsync(MySqlConnection conn, string scriptSql, CancellationToken ct)
    {
        foreach (var stmt in SplitSqlStatements(RemoveComments(scriptSql)))
        {
            var s = stmt.Trim();

            if (string.IsNullOrWhiteSpace(s))
                continue;

            await ExecAsync(conn, s.EndsWith(";") ? s : s + ";", ct);
        }
    }

    private static string RemoveComments(string sql)
    {
        sql = System.Text.RegularExpressions.Regex.Replace(
            sql,
            @"/\*.*?\*/",
            "",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        var sb = new StringBuilder();

        using var reader = new StringReader(sql);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var idx = line.IndexOf("--", StringComparison.Ordinal);

            if (idx >= 0)
                line = line[..idx];

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static IEnumerable<string> SplitSqlStatements(string sql)
    {
        var sb = new StringBuilder();

        var inSingle = false;
        var inDouble = false;
        var inBacktick = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if ((inSingle || inDouble) && c == '\\' && i + 1 < sql.Length)
            {
                sb.Append(c);
                sb.Append(sql[++i]);
                continue;
            }

            if (!inDouble && !inBacktick && c == '\'')
                inSingle = !inSingle;
            else if (!inSingle && !inBacktick && c == '"')
                inDouble = !inDouble;
            else if (!inSingle && !inDouble && c == '`')
                inBacktick = !inBacktick;

            if (!inSingle && !inDouble && !inBacktick && c == ';')
            {
                var statement = sb.ToString().Trim();

                if (statement.Length > 0)
                    yield return statement;

                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        var last = sb.ToString().Trim();

        if (last.Length > 0)
            yield return last;
    }

    private async Task<DataTable> QueryToTableAsync(MySqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(sql, conn)
        {
            CommandTimeout = _opt.CommandTimeoutSeconds
        };

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var table = new DataTable();
        table.Load(reader);

        return table;
    }

    private static string DataTableToJson(DataTable dt)
    {
        var rows = new List<Dictionary<string, object?>>();

        foreach (DataRow r in dt.Rows)
        {
            var dict = new Dictionary<string, object?>();

            foreach (DataColumn c in dt.Columns)
                dict[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];

            rows.Add(dict);
        }

        return JsonSerializer.Serialize(rows);
    }

    private static bool TablesEqual(DataTable a, DataTable b)
    {
        if (a.Columns.Count != b.Columns.Count)
            return false;

        for (var i = 0; i < a.Columns.Count; i++)
        {
            if (!string.Equals(a.Columns[i].ColumnName, b.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (a.Rows.Count != b.Rows.Count)
            return false;

        for (var r = 0; r < a.Rows.Count; r++)
        {
            for (var c = 0; c < a.Columns.Count; c++)
            {
                var av = NormalizeValue(a.Rows[r][c]);
                var bv = NormalizeValue(b.Rows[r][c]);

                if (!Equals(av, bv))
                    return false;
            }
        }

        return true;
    }

    private static object? NormalizeValue(object value)
    {
        return value == DBNull.Value ? null : value;
    }
}