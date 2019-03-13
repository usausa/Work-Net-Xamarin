namespace Smart.Data.Mapper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Collections.Concurrent;
    using Smart.Converter;
    using Smart.Data.Mapper.Handlers;
    using Smart.Data.Mapper.Mappers;
    using Smart.Data.Mapper.Namings;
    using Smart.Data.Mapper.Parameters;
    using Smart.Reflection;

    public sealed class SqlMapperConfig : ISqlMapperConfig
    {
        private static readonly IParameterBuilderFactory[] DefaultParameterBuilderFactories =
        {
            DynamicParameterBuilderFactory.Instance,
            DictionaryParameterBuilderFactory.Instance,
            ObjectParameterBuilderFactory.Instance
        };

        private static readonly IResultMapperFactory[] DefaultResultMapperFactories =
        {
            ObjectResultMapperFactory.Instance
        };

        private static readonly Dictionary<Type, DbType> DefaultTypeMap = new Dictionary<Type, DbType>
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(TimeSpan), DbType.Time },
            { typeof(byte[]), DbType.Binary },
            { typeof(object), DbType.Object },
        };

        private static readonly Dictionary<Type, ITypeHandler> DefaultTypeHandlers = new Dictionary<Type, ITypeHandler>();

        private readonly ThreadsafeTypeHashArrayMap<Action<IDbCommand, object>> parameterBuilderCache = new ThreadsafeTypeHashArrayMap<Action<IDbCommand, object>>();

        private readonly ResultMapperCache resultMapperCache = new ResultMapperCache();

        private readonly ThreadsafeTypeHashArrayMap<TypeHandleEntry> typeHandleEntries = new ThreadsafeTypeHashArrayMap<TypeHandleEntry>();

        private IParameterBuilderFactory[] parameterBuilderFactories;

        private IResultMapperFactory[] resultMapperFactories;

        private Dictionary<Type, DbType> typeMap;

        private Dictionary<Type, ITypeHandler> typeHandlers;

        //--------------------------------------------------------------------------------
        // Property
        //--------------------------------------------------------------------------------

        public static SqlMapperConfig Default { get; } = new SqlMapperConfig();

        public IDelegateFactory DelegateFactory { get; set; } = Smart.Reflection.DelegateFactory.Default;

        public IObjectConverter Converter { get; set; } = ObjectConverter.Default;

        public INaming Naming { get; set; } = DefaultNaming.Instance;

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        public SqlMapperConfig()
        {
            parameterBuilderFactories = DefaultParameterBuilderFactories;
            resultMapperFactories = DefaultResultMapperFactories;
            typeMap = DefaultTypeMap;
            typeHandlers = DefaultTypeHandlers;
        }

        //--------------------------------------------------------------------------------
        // Config
        //--------------------------------------------------------------------------------

        public SqlMapperConfig ResetParameterBuilderFactories()
        {
            parameterBuilderFactories = DefaultParameterBuilderFactories;
            parameterBuilderCache.Clear();
            return this;
        }

        public SqlMapperConfig ConfigureBuilderFactory(Action<IList<IParameterBuilderFactory>> action)
        {
            var list = new List<IParameterBuilderFactory>(parameterBuilderFactories);
            action(list);
            parameterBuilderFactories = list.ToArray();
            parameterBuilderCache.Clear();
            return this;
        }

        public SqlMapperConfig ResetResultMappers()
        {
            resultMapperFactories = DefaultResultMapperFactories;
            resultMapperCache.Clear();
            return this;
        }

        public SqlMapperConfig ConfigureMapperFactory(Action<IList<IResultMapperFactory>> action)
        {
            var list = new List<IResultMapperFactory>(resultMapperFactories);
            action(list);
            resultMapperFactories = list.ToArray();
            resultMapperCache.Clear();
            return this;
        }

        public SqlMapperConfig ResetTypeMap()
        {
            typeMap = DefaultTypeMap;
            typeHandleEntries.Clear();
            return this;
        }

        public SqlMapperConfig ConfigureTypeMap(Action<IDictionary<Type, DbType>> action)
        {
            var dictionary = new Dictionary<Type, DbType>(typeMap);
            action(dictionary);
            typeMap = dictionary;
            typeHandleEntries.Clear();
            return this;
        }

        public SqlMapperConfig ResetTypeHandlers()
        {
            typeHandlers = DefaultTypeHandlers;
            typeHandleEntries.Clear();
            return this;
        }

        public SqlMapperConfig ConfigureTypeHandlers(Action<IDictionary<Type, ITypeHandler>> action)
        {
            var dictionary = new Dictionary<Type, ITypeHandler>(typeHandlers);
            action(dictionary);
            typeHandlers = dictionary;
            typeHandleEntries.Clear();
            return this;
        }

        //--------------------------------------------------------------------------------
        // ISqlMapperConfig
        //--------------------------------------------------------------------------------

        Func<T> ISqlMapperConfig.CreateFactory<T>() => DelegateFactory.CreateFactory<T>();

        Func<object, object> ISqlMapperConfig.CreateGetter(PropertyInfo pi) => DelegateFactory.CreateGetter(pi);

        Action<object, object> ISqlMapperConfig.CreateSetter(PropertyInfo pi) => DelegateFactory.CreateSetter(pi);

        Func<string, string> ISqlMapperConfig.GetNameConverter() => Naming.Convert;

        Func<object, object> ISqlMapperConfig.CreateParser(Type sourceType, Type destinationType)
        {
            if (!typeHandleEntries.TryGetValue(destinationType, out var entry))
            {
                entry = typeHandleEntries.AddIfNotExist(destinationType, CreateTypeHandleInternal);
            }

            if (entry.TypeHandler != null)
            {
                return x => entry.TypeHandler.Parse(destinationType, x);
            }

            return Converter.CreateConverter(sourceType, destinationType);
        }

        TypeHandleEntry ISqlMapperConfig.LookupTypeHandle(Type type)
        {
            if (!typeHandleEntries.TryGetValue(type, out var entry))
            {
                entry = typeHandleEntries.AddIfNotExist(type, CreateTypeHandleInternal);
            }

            if (!entry.CanUseAsParameter)
            {
                throw new SqlMapperException($"Type cannot use as parameter. type=[{type.FullName}]");
            }

            return entry;
        }

        private TypeHandleEntry CreateTypeHandleInternal(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            var findDbType = typeMap.TryGetValue(type, out var dbType);
            if (!findDbType && type.IsEnum)
            {
                findDbType = typeMap.TryGetValue(Enum.GetUnderlyingType(type), out dbType);
            }

            typeHandlers.TryGetValue(type, out var handler);

            return new TypeHandleEntry(findDbType || (handler != null), dbType, handler);
        }

        Func<IDataRecord, T> ISqlMapperConfig.CreateMapper<T>(IDataReader reader)
        {
            // TODO ThreadLocal cache?
            var type = typeof(T);
            var columns = new ColumnInfo[reader.FieldCount];
            for (var i = 0; i < columns.Length; i++)
            {
                columns[i] = new ColumnInfo(reader.GetName(i), reader.GetFieldType(i));
            }

            if (resultMapperCache.TryGetValue(type, columns, out var value))
            {
                return (Func<IDataRecord, T>)value;
            }

            return (Func<IDataRecord, T>)resultMapperCache.AddIfNotExist(type, columns, CreateMapperInternal<T>);
        }

        private object CreateMapperInternal<T>(Type type, ColumnInfo[] columns)
        {
            foreach (var factory in resultMapperFactories)
            {
                if (factory.IsMatch(type))
                {
                    return factory.CreateMapper<T>(this, type, columns);
                }
            }

            throw new SqlMapperException($"Result type is not supported. type=[{type.FullName}]");
        }

        Action<IDbCommand, object> ISqlMapperConfig.CreateParameterBuilder(Type type)
        {
            if (!parameterBuilderCache.TryGetValue(type, out var parameterBuilder))
            {
                parameterBuilder = parameterBuilderCache.AddIfNotExist(type, CreateParameterBuilderInternal);
            }

            return parameterBuilder;
        }

        private Action<IDbCommand, object> CreateParameterBuilderInternal(Type type)
        {
            foreach (var factory in parameterBuilderFactories)
            {
                if (factory.IsMatch(type))
                {
                    return factory.CreateBuilder(this, type);
                }
            }

            throw new SqlMapperException($"Parameter type is not supported. type=[{type.FullName}]");
        }
    }
}
