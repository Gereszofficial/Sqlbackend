using System.Text.RegularExpressions;

namespace SqlTrainer.Api.Services;

public static class SqlSafety
{
    // SELECT-only mód: minden nem SELECT jellegű művelet tiltva.
    private static readonly string[] ForbiddenInSelectOnly =
    [
        "insert ", "update ", "delete ", "drop ", "alter ", "create ",
        "truncate ", "grant ", "revoke ", "shutdown ", "load_file", "outfile", "dumpfile",
        "call ", "prepare ", "execute ", "deallocate ", "use ", "set "
    ];

    // Sandbox-safe mód: DDL/DML lehet, de veszélyes / rendszer-szintű parancsok nem.
    private static readonly string[] ForbiddenInSandbox =
    [
        "create user", "grant ", "revoke ", "shutdown", "flush ",
        "drop database", "drop schema", "create schema", "alter user", "rename user",
        "set global", "set @@global", "set persist", "use ",
        "load_file", "outfile", "dumpfile", "load data", "into outfile", "into dumpfile",
        "information_schema", "mysql.", "performance_schema"
    ];

    public static bool IsProbablySelectOnly(string sql)
    {
        var s = Normalize(sql);
        if (!s.StartsWith("select ")) return false;
        return !ForbiddenInSelectOnly.Any(f => s.Contains(f));
    }

    /// <summary>
    /// Engedélyez DDL/DML parancsokat is, de tiltja a veszélyes / rendszer-szintű műveleteket.
    /// A runner egy izolált, egyedi sémában futtat mindent, amit a végén töröl.
    /// </summary>
    public static bool IsSandboxSafe(string sql)
    {
        var s = Normalize(sql);
        if (string.IsNullOrWhiteSpace(s)) return false;

        // Ne engedjük, hogy bárki másik adatbázisba / rendszer sémába nyúljon.
        return !ForbiddenInSandbox.Any(f => s.Contains(f));
    }

    //public static string EnsureLimit(string sql, int maxRows)
    //{
    //    var s = sql.Trim().TrimEnd(';');
    //    if (Regex.IsMatch(s, @"\blimit\b", RegexOptions.IgnoreCase)) return s + ";";
    //    return $"{s} LIMIT {maxRows};";
    //}
    public static string EnsureLimit(string sql, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var s = sql.Trim();
        return s.EndsWith(";") ? s : s + ";";
    }
    

    private static string Normalize(string sql)
        => Regex.Replace(sql.ToLowerInvariant(), @"\s+", " ").Trim();
}
