using BMSCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Bcpg;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace BMSShared
{
    public static class GenericTypeManipulation
    {

        public static string GetPropertyValue(Object o, string sPropertyName)
        {
            Type t = o.GetType();
            PropertyInfo propertyInfo = t.GetProperty(sPropertyName);
            if (propertyInfo != null)
            {
                object value = propertyInfo.GetValue(o);
                return value.ToStr();
            }
            return null;
        }

        public static object GetProperty(object target, string name)
        {
            return Microsoft.VisualBasic.CompilerServices.Versioned.CallByName(target, name, CallType.Get);
        }

        public class InlineFileStreamResult : FileStreamResult
        {
            public InlineFileStreamResult(Stream fileStream, string contentType)
                : base(fileStream, contentType)
            {
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var contentDispositionHeader = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
                contentDispositionHeader.SetHttpFileName(FileDownloadName);
                context.HttpContext.Response.Headers.Add(HeaderNames.ContentDisposition, contentDispositionHeader.ToString());
                FileDownloadName = null;
                return base.ExecuteResultAsync(context);
            }
        }
        public static Type GetTypeEx(string fullTypeName)
        {
            return Type.GetType(fullTypeName) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType(fullTypeName))
                            .FirstOrDefault(t => t != null);
        }

        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> l = new List<T>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic o= Activator.CreateInstance(typeof(T));
                SetObject<T>(o, dt.Rows[i]);
                l.Add(o);
            }
            return l;
        }

        public static void SetObject<T>(object item, DataRow dataRow)
        {
            // populates an object with values from a data row

            foreach (DataColumn column in dataRow.Table.Columns)
            {
                Type t = typeof(T);
                PropertyInfo property = t.GetProperty(column.ColumnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null && dataRow[column] != DBNull.Value && dataRow[column].ToString() != "NULL")
                {
                    property.SetValue(item, ChangeType(dataRow[column], property.PropertyType), null);
                }
            }
        }
        public static T ToObject<T>(this DataRow dataRow) where T : new()
        {
            // converts from a data row to an object
            T item = new T();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                PropertyInfo property = GetProperty(typeof(T), column.ColumnName);
                if (property != null && dataRow[column] != DBNull.Value && dataRow[column].ToString() != "NULL")
                {
                    property.SetValue(item, ChangeType(dataRow[column], property.PropertyType), null);
                }
            }
            return item;
        }

        private static PropertyInfo GetProperty(Type type, string attributeName)
        {
            PropertyInfo property = type.GetProperty(attributeName);

            if (property != null)
            {
                return property;
            }

            return type.GetProperties()
                 .Where(p => p.IsDefined(typeof(DisplayAttribute), false) && p.GetCustomAttributes(typeof(DisplayAttribute), false).Cast<DisplayAttribute>().Single().Name == attributeName)
                 .FirstOrDefault();
        }

        public static object ChangeType(object value, Type type)
        {
            try
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    if (value == null)
                    {
                        return null;
                    }
                    return Convert.ChangeType(value, Nullable.GetUnderlyingType(type));
                }
                string sSourceType = value.GetType().ToString();
                string sDestType = type.ToString();
                if (sDestType == "System.String" && sSourceType == "System.Guid")
                {
                    return value.ToString();
                }
                return Convert.ChangeType(value, type);
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
}
