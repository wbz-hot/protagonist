using System;
using System.Data;
using Dapper;
using DLCS.Core.Enum;

namespace DLCS.Repository.TypeMappings
{
    /// <summary>
    /// <see cref="Dapper.SqlMapper.ITypeHandler"/> for mapping string (in DB) to Enum (in Model).
    /// </summary>
    /// <typeparam name="T">Type of enum.</typeparam>
    public class EnumStringMapper<T> : SqlMapper.ITypeHandler
        where T : Enum
    {
        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.DbType = DbType.String;
            // ReSharper disable once MergeConditionalExpression
            parameter.Value = value == null ? null : ((T) value).GetDescription();
        }

        public object Parse(Type destinationType, object value)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue) || destinationType != typeof(T))
            {
                return null;
            }

            return value.ToString().GetEnumFromString<T>();
        }
    }
}