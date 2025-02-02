using BBP.CORE.API.Utilities;
using BMSShared;
using System.Data;
using System.Data.SQLite;
using System.Reflection;


namespace BBP.CORE.API.Database
{
    public class Sqlite
    {
        private static string PropertyToSqliteType(PropertyInfo myProp)
        {
            if (myProp.PropertyType == typeof(string))
            {
                return "TEXT";
            }
            else if (myProp.PropertyType == typeof(int) || myProp.PropertyType == typeof(short)
                || myProp.PropertyType == typeof(Int64))
            {
                return "INT";
            }
            else if (myProp.PropertyType == typeof(DateTime) || myProp.PropertyType == typeof(DateOnly))
            {
                // sqlite does not have a date type
                return "TEXT";
            }
            else if (myProp.PropertyType == typeof(Double) || myProp.PropertyType == typeof(float)
                || myProp.PropertyType == typeof(Decimal))
            {
                return "REAL";
            }
            else
            {
                return "ANY";
            }
        }

        public static string ScriptCreateTable(object oReferenceObject)
        {
            if (oReferenceObject == null)
            {
                return String.Empty;
            }
            Type t = oReferenceObject.GetType();
            string sTableName = GetTableNameByType(t);
            string sCreate = "CREATE TABLE IF NOT EXISTS {tbl} ({fields}) STRICT;";
            sCreate = sCreate.Replace("{tbl}", sTableName);
            PropertyInfo[] properties = t.GetProperties();

            string sSchema = String.Empty;
            foreach (PropertyInfo property in properties)
            {
                string sType = PropertyToSqliteType(property);
                string sRow = property.Name + " " + sType + ",";
                sSchema += sRow;
            }
            if (sSchema.Length > 1)
            {
                sSchema = sSchema.Substring(0, sSchema.Length - 1);
            }
            sCreate = sCreate.Replace("{fields}", sSchema);
            return sCreate;
        }

        public static bool CreateView(object oRefObj)
        {
            string sSQL = ScriptCreateTable(oRefObj);
            bool f = ExecuteNonQuery(sSQL);
            return f;
        }

        public static string ScriptDeleteObject(object o)
        {
            // script a delete by primary key
            Type t = o.GetType();
            string sPK = QuorumUtils.GetPrimaryKeyName(t);
            string sVal = GenericTypeManipulation.GetPropertyValue(o, sPK);
            string sDel = "DELETE FROM {TBL} WHERE {ID}='{VALUE}';";
            sDel = sDel.Replace("{TBL}", GetTableNameByObj(o));
            sDel = sDel.Replace("{ID}", sPK);
            sDel = sDel.Replace("{VALUE}", sVal);
            return sDel;
        }

        public static string ScriptUpsertObject(object o)
        {
            string sDel = ScriptDeleteObject(o);
            string sIns = ScriptObjectInsert(o);
            string sCombined = sDel + "\r\n" + sIns;
            return sCombined;
        }
        public static bool UpsertObject(object o)
        {
            string sUpsert = ScriptUpsertObject(o);
            bool f = ExecuteNonQuery(sUpsert);
            return f;
        }

        public static string GetTableNameByObj(object o)
        {
            Type t = o.GetType();
            return GetTableNameByType(t);
        }

        public static string GetTableNameByType(Type t)
        {
            string sTableName = t.FullName ?? String.Empty;
            sTableName = sTableName.Replace(".", "_");
            return sTableName;
        }
        public static string ScriptObjectInsert(object o)
        {
            Type t = o.GetType();
            string sIns = "INSERT INTO {TBL} ({FIELDS}) VALUES ({VALUES});";
            PropertyInfo[] properties = t.GetProperties();
            string sTableName = GetTableNameByObj(o);

            string sFields = String.Empty;
            string sValues = String.Empty;
            foreach (PropertyInfo property in properties)
            {
                sFields += property.Name + ",";
                object oVal = GenericTypeManipulation.GetPropertyValue(o, property.Name);
                string sVal = oVal.ToString() ?? String.Empty;
                // if the property is a date, we must use the ISO8601 format:
                if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateOnly))
                {
                    DateTime dtRef = Convert.ToDateTime(oVal);
                    sVal = dtRef.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                sVal = sVal.Replace("'", "''");
                sValues += "'" + sVal + "',";
            }
            if (sFields.Length > 0) sFields = sFields.Substring(0, sFields.Length - 1);
            if (sValues.Length > 0) sValues = sValues.Substring(0, sValues.Length - 1);
            sIns = sIns.Replace("{TBL}", sTableName);
            sIns = sIns.Replace("{FIELDS}", sFields);
            sIns = sIns.Replace("{VALUES}", sValues);

            return sIns;
        }

        public static string GetConnStr()
        {
            string sFolder = BMSCommon.Common.GetFolder("Database");
            string sFile = "bbp.db";
            string sPath = Path.Combine(sFolder, sFile);
            string sCN = "Data source=" + sPath;
            return sCN;
        }
        public static bool ExecuteNonQuery(string sql)
        {
            try
            {
                using (var connection = new SQLiteConnection(GetConnStr()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            } catch (Exception ex)
            {
                return false;
            }
        }
        public static List<T> GetViewObjects<T>(string sWhere = "", string sOrderby = "")
        {
            Type t = typeof(T);
            List<T> l = new List<T>();
            try
            {
                // populates an object with values from a data row
                using (var connection = new SQLiteConnection(GetConnStr()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        string sTableName = GetTableNameByType(t);
                        string sql = "Select * from {TBL} {WHERE} {ORDERBY}";
                        if (sWhere != String.Empty)
                        {
                            sWhere = "WHERE " + sWhere;
                        }
                        if (sOrderby != String.Empty)
                        {
                            sOrderby = "ORDER BY " + sOrderby;
                        }
                        sql = sql.Replace("{TBL}", sTableName);
                        sql = sql.Replace("{WHERE}", sWhere);
                        sql = sql.Replace("{ORDERBY}", sOrderby);

                        command.CommandText = sql;
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            T obj = (T)Activator.CreateInstance(typeof(T));
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string sField = reader.GetName(i);
                                object oValue = reader[i];
                                PropertyInfo property = t.GetProperty(sField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                property.SetValue(obj, GenericTypeManipulation.ChangeType(oValue, property.PropertyType), null);
                            }
                            l.Add(obj);
                        }
                    }
                }
                return l;
            }
            catch (Exception ex)
            {
                return l;
            }
        }

        public static DataTable GetDataTable(string sql)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (var connection = new SQLiteConnection(GetConnStr()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {

                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
                return dataTable;
            }
            catch (Exception ex)
            {
                return dataTable;
            }
        }
    }
}
