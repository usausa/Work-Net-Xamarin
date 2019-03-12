namespace Smart.Data.Mapper
{
    using System;
    using System.Data;
    using System.Runtime.CompilerServices;

    public static partial class SqlMapper
    {
        //--------------------------------------------------------------------------------
        // Core
        //--------------------------------------------------------------------------------

        private static IDbCommand SetupCommand(ISqlMapperConfig config, IDbConnection con, IDbTransaction transaction, string sql, object param, int? commandTimeout, CommandType? commandType)
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

            if (param != null)
            {
                config.BuildCommand(cmd, param);
            }

            return cmd;
        }

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
            using (var cmd = SetupCommand(config, con, transaction, sql, param, commandTimeout, commandType))
            {
                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var result = cmd.ExecuteNonQuery();

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

        //--------------------------------------------------------------------------------
        // ExecuteScalar
        //--------------------------------------------------------------------------------

        public static T ExecuteScalar<T>(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(config, con, transaction, sql, param, commandTimeout, commandType))
            {
                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var result = cmd.ExecuteScalar();

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

        //--------------------------------------------------------------------------------
        // ExecuteReader
        //--------------------------------------------------------------------------------

        public static IDataReader ExecuteReader(this IDbConnection con, ISqlMapperConfig config, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            var wasClosed = con.State == ConnectionState.Closed;
            using (var cmd = SetupCommand(config, con, transaction, sql, param, commandTimeout, commandType))
            {
                try
                {
                    if (wasClosed)
                    {
                        con.Open();
                    }

                    var reader = cmd.ExecuteReader(commandBehavior);
                    wasClosed = false;

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

        //--------------------------------------------------------------------------------
        // Query
        //--------------------------------------------------------------------------------

        // TODO QueryImpl

        // TODO QueryFirstOrDefault, Query
        // Lookup回数？、パラメータ、戻り値、カラムのマップはQueryなら一端配列を作って？ Stackで？

        ////--------------------------------------------------------------------------------

        //private static IEnumerable<T> QueryImpl<T>(this IDbConnection con, Func<T> factory, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType)
        //{
        //    var wasClosed = con.State == ConnectionState.Closed;
        //    using (var cmd = SetupCommand(con, transaction, sql, param, commandTimeout, commandType))
        //    {
        //        try
        //        {
        //            var type = typeof(T);
        //            var queryHandler = queryHandlers.FirstOrDefault(x => x.IsMatch(type));
        //            if (queryHandler == null)
        //            {
        //                throw new SqlMapperException(String.Format(CultureInfo.InvariantCulture, "Type {0} can't handle", type.FullName));
        //            }

        //            if (wasClosed)
        //            {
        //                con.Open();
        //            }

        //            var reader = cmd.ExecuteReader(wasClosed ? CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess : CommandBehavior.SequentialAccess);
        //            wasClosed = false;

        //            return queryHandler.Handle(factory, reader, Converter);
        //        }
        //        finally
        //        {
        //            Cleanup(wasClosed, con, cmd);
        //        }
        //    }
        //}

        ////--------------------------------------------------------------------------------
        //// Query
        //public static IEnumerable<T> Query<T>(this IDbConnection con, Func<T> factory, string sql)
        //    return QueryImpl(con, factory, sql, null, null, null, null);
        //// where T : new()
    }
}
