namespace Smart.Data.Mapper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public static class SqlMapper
    {
        private const CommandBehavior CommandBehaviorQueryWithClose =
            CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess;

        private const CommandBehavior CommandBehaviorQuery =
            CommandBehavior.SequentialAccess;

        private const CommandBehavior CommandBehaviorQueryFirstOrDefaultWithClose =
            CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

        private const CommandBehavior CommandBehaviorQueryFirstOrDefault =
            CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

        //--------------------------------------------------------------------------------
        // Core
        //--------------------------------------------------------------------------------

        private static IDbCommand SetupCommand(IDbConnection con, IDbTransaction transaction, string sql, int? commandTimeout, CommandType? commandType)
        {
            var cmd = con.CreateCommand();

            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.CommandText = sql;

            if (commandTimeout.HasValue)
            {
                cmd.CommandTimeout = commandTimeout.Value;
            }

            if (commandType.HasValue)
            {
                cmd.CommandType = commandType.Value;
            }

            return cmd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DbCommand SetupAsyncCommand(IDbConnection con, IDbTransaction transaction, string sql, int? commandTimeout, CommandType? commandType)
        {
            if (SetupCommand(con, transaction, sql, commandTimeout, commandType) is DbCommand dbCommand)
            {
                return dbCommand;
            }

            throw new SqlMapperException("Async operation is not supported.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task OpenAsync(IDbConnection con, CancellationToken token)
        {
            if (con is DbConnection dbConnection)
            {
                return dbConnection.OpenAsync(token);
            }

            throw new SqlMapperException("Async operation is not supported.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Cleanup(bool wasClosed, IDbConnection con, IDbCommand cmd)
        {
            cmd.Dispose();

            if (wasClosed)
            {
                con.Close();
            }
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        public static int Execute(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var result = cmd.ExecuteNonQuery();

                    // TODO

                    return result;
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Execute(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return Execute(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType);
        }

        public static async Task<int> ExecuteAsync(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupAsyncCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        await OpenAsync(con, token).ConfigureAwait(false);
                    }

                    var result = await cmd.ExecuteNonQueryAsync(token);

                    // TODO

                    return result;
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> ExecuteAsync(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            return ExecuteAsync(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, token);
        }

        //--------------------------------------------------------------------------------
        // ExecuteScalar
        //--------------------------------------------------------------------------------

        public static T ExecuteScalar<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var result = cmd.ExecuteScalar();

                    // TODO

                    if (result is DBNull)
                    {
                        return default;
                    }

                    if (result is T scalar)
                    {
                        return scalar;
                    }

                    var parser = config.CreateParser(result.GetType(), typeof(T));
                    return (T)parser(result);
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ExecuteScalar<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return ExecuteScalar<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType);
        }

        public static async Task<T> ExecuteScalarAsync<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupAsyncCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        await OpenAsync(con, token).ConfigureAwait(false);
                    }

                    var result = await cmd.ExecuteScalarAsync(token);

                    // TODO

                    if (result is DBNull)
                    {
                        return default;
                    }

                    if (result is T scalar)
                    {
                        return scalar;
                    }

                    var parser = config.CreateParser(result.GetType(), typeof(T));
                    return (T)parser(result);
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> ExecuteScalarAsync<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            return ExecuteScalarAsync<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, token);
        }

        //--------------------------------------------------------------------------------
        // ExecuteReader
        //--------------------------------------------------------------------------------

        public static IDataReader ExecuteReader(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var reader = cmd.ExecuteReader(wasClosed ? commandBehavior | CommandBehavior.CloseConnection : commandBehavior);
                    wasClosed = false;

                    // TODO

                    return reader;
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDataReader ExecuteReader(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            return ExecuteReader(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, commandBehavior);
        }

        public static async Task<DbDataReader> ExecuteReaderAsync(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupAsyncCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        await OpenAsync(con, token).ConfigureAwait(false);
                    }

                    var reader = await cmd.ExecuteReaderAsync(wasClosed ? commandBehavior | CommandBehavior.CloseConnection : commandBehavior, token);
                    wasClosed = false;

                    // TODO

                    return reader;
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<DbDataReader> ExecuteReaderAsync(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default)
        {
            return ExecuteReaderAsync(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, commandBehavior, token);
        }

        //--------------------------------------------------------------------------------
        // Query
        //--------------------------------------------------------------------------------

        public static IEnumerable<T> Query<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    using (var reader = cmd.ExecuteReader(wasClosed ? CommandBehaviorQueryWithClose : CommandBehaviorQuery))
                    {
                        wasClosed = false;

                        // TODO

                        var mapper = config.CreateMapper<T>(reader);

                        while (reader.Read())
                        {
                            yield return mapper(reader);
                        }
                    }
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Query<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return Query<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupAsyncCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                var reader = default(DbDataReader);
                try
                {
                    if (wasClosed)
                    {
                        await OpenAsync(con, token).ConfigureAwait(false);
                    }

                    reader = await cmd.ExecuteReaderAsync(wasClosed ? CommandBehaviorQueryWithClose : CommandBehaviorQuery, token);
                    wasClosed = false;

                    // TODO

                    var mapper = config.CreateMapper<T>(reader);

                    var deferred = ExecuteReaderSync(reader, mapper);
                    reader = null;
                    return deferred;
                }
                finally
                {
                    reader?.Dispose();
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        private static IEnumerable<T> ExecuteReaderSync<T>(IDataReader reader, Func<IDataRecord, T> mapper)
        {
            while (reader.Read())
            {
                yield return mapper(reader);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            return QueryAsync<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, token);
        }

        //--------------------------------------------------------------------------------
        // QueryFirstOrDefault
        //--------------------------------------------------------------------------------

        public static T QueryFirstOrDefault<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    using (var reader = cmd.ExecuteReader(wasClosed ? CommandBehaviorQueryFirstOrDefaultWithClose : CommandBehaviorQueryFirstOrDefault))
                    {
                        wasClosed = false;

                        // TODO

                        var mapper = config.CreateMapper<T>(reader);

                        return reader.Read() ? mapper(reader) : default;
                    }
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T QueryFirstOrDefault<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return QueryFirstOrDefault<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType);
        }

        public static async Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupAsyncCommand(con, transaction, sql, commandTimeout, commandType))
            {
                // TODO
                if (param != null)
                {
                    var builder = config.CreateParameterBuilder(param.GetType());
                    builder(cmd, param);
                }

                try
                {
                    if (wasClosed)
                    {
                        await OpenAsync(con, token).ConfigureAwait(false);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync(wasClosed ? CommandBehaviorQueryFirstOrDefaultWithClose : CommandBehaviorQueryFirstOrDefault, token))
                    {
                        wasClosed = false;

                        // TODO

                        var mapper = config.CreateMapper<T>(reader);

                        return await reader.ReadAsync(token).ConfigureAwait(false) ? mapper(reader) : default;
                    }
                }
                finally
                {
                    Cleanup(wasClosed, con, cmd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection con, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CancellationToken token = default)
        {
            return QueryFirstOrDefaultAsync<T>(con, SqlMapperConfig.Default, sql, param, transaction, commandTimeout, commandType, token);
        }
    }
}
