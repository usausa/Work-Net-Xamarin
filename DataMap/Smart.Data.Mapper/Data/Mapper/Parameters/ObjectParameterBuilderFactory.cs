namespace Smart.Data.Mapper.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Mapper.Handlers;

    public sealed class ObjectParameterBuilderFactory : IParameterBuilderFactory
    {
        public static ObjectParameterBuilderFactory Instance { get; } = new ObjectParameterBuilderFactory();

        private ObjectParameterBuilderFactory()
        {
        }

        private sealed class ParameterEntry
        {
            public string Name { get; }

            public Func<object, object> Getter { get; }

            public DbType DbType { get; }

            public ITypeHandler Handler { get; }

            public ParameterEntry(string name, Func<object, object> getter, DbType dbType, ITypeHandler handler)
            {
                Name = name;
                Getter = getter;
                DbType = dbType;
                Handler = handler;
            }
        }

        public bool IsMatch(Type type)
        {
            return true;
        }

        public Action<IDbCommand, object> CreateBuilder(ISqlMapperConfig config, Type type)
        {
            var entries = CreateParameterEntries(config, type);

            return (cmd, parameter) =>
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var param = cmd.CreateParameter();

                    param.ParameterName = entry.Name;

                    var value = entry.Getter(parameter);

                    if (value is null)
                    {
                        param.Value = DBNull.Value;
                    }
                    else
                    {
                        param.DbType = entry.DbType;
                        if (entry.Handler != null)
                        {
                            entry.Handler.SetValue(param, value);
                        }
                        else
                        {
                            param.Value = value;
                        }
                    }

                    cmd.Parameters.Add(param);
                }
            };
        }

        private static ParameterEntry[] CreateParameterEntries(ISqlMapperConfig config, Type type)
        {
            var list = new List<ParameterEntry>();
            foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanRead))
            {
                var getter = config.CreateGetter(pi);
                var entry = config.LookupTypeHandle(pi.PropertyType);
                list.Add(new ParameterEntry(pi.Name, getter, entry.DbType, entry.TypeHandler));
            }

            return list.ToArray();
        }
    }
}
