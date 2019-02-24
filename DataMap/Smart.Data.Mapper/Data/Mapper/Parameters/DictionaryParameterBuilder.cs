namespace Smart.Data.Mapper.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    public sealed class DictionaryParameterBuilder : IParameterBuilder
    {
        public bool Handle(ISqlMapperConfig config, IDbCommand cmd, object parameter)
        {
            if (parameter is IDictionary<string, object> dictionary)
            {
                foreach (var keyValue in dictionary)
                {
                    // TODO
                    var param = cmd.CreateParameter();
                    param.ParameterName = keyValue.Key;
                    var value = keyValue.Value ?? DBNull.Value;
                    if (value != DBNull.Value)
                    {
                        //parameter.DbType = dbTypeMap.LookupDbType(value);
                    }
                    param.Value = value;
                    cmd.Parameters.Add(param);
                }

                return true;
            }

            return false;
        }
    }
}
