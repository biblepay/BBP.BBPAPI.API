using static BMSCommon.Model.BitcoinSyncModel;
using static BMSCommon.Common;
using BMSCommon;
using BMSCommon.Model;
using Nethereum.Contracts;
using BBP.CORE.API.Database;
using BMSShared;
namespace BBP.CORE.API.Utilities
{
    public class NVRAM
    {
        // The purpose of this class is to allow us to serialize private objects on this node
        // Giving us locally fast performance
        // Useful for Batch Job latches (IE Has this job ran?) and caching local files etc.
        // It also acts as non volatile ram for storage with persistence between machine restarts.
        // So its Volatile in the sense that it is constantly replaced, but Non Volatile in that it persists between reboots.

        private static bool fCheckedSchema = false;
        private static void CheckSchema()
        {
            SystemKey sk = new SystemKey();
            if (!fCheckedSchema)
            {
                fCheckedSchema = true;
                Sqlite.CreateView(sk);
            }
        }

        public static async Task<bool> InsertObject<T>(Object o)
        {
            CheckSchema();
            List<T> list = new List<T>();
            list.Add((T)o);
            Sqlite.UpsertObject(o);
            return true;
        }

        public static List<T> GetObjects<T>()
        {
            CheckSchema();
            List<T> l = Sqlite.GetViewObjects<T>();
            return l;
        }

        public static T? GetObject<T>(object oOrigObject)
        {
            List<T> l = GetObjects<T>();
            if (l.Count < 1)
            {
                return default(T);
            }
            Type t1 = l[0].GetType();
            string sPrimaryKeyName = QuorumUtils.GetPrimaryKeyName(t1);
            for (int i = 0; i < l.Count; i++)
            {
                T obj = l[i];
                object oComparisonPKValue = GenericTypeManipulation.GetProperty(obj, sPrimaryKeyName);
                object oSourcePKValue = GenericTypeManipulation.GetProperty(oOrigObject, sPrimaryKeyName);
                bool fSame = (oComparisonPKValue.ToStr()) == (oSourcePKValue.ToStr());
                if (fSame)
                {
                    return obj;
                }
            }
            return default(T);
        }

        public static void SetNVRamValue(string sKey, string sValue)
        {
            SystemKey sk = new SystemKey();
            sk.id = sKey;
            sk.Added = DateTime.Now;
            sk.Value = sValue;
            bool f = InsertObject<SystemKey>(sk).Result;
        }

        public static string GetNVRamValue(string sKey)
        {
            SystemKey sk = new SystemKey();
            sk.id = sKey;
            sk = GetObject<SystemKey>(sk);
            if (sk == null) return String.Empty;
            return sk.Value.ToStr();
        }

        public static DateTime GetLastWriteTime(string sKey)
        {
            SystemKey sk = new SystemKey();
            sk.id = sKey;
            sk = GetObject<SystemKey>(sk);
            if (sk == null) return DateTime.MinValue;
            return sk.Added;
        }

        public static TimeSpan GetElapsedTime(string sKey)
        {
            DateTime dtLast = GetLastWriteTime(sKey);
            TimeSpan Elapsed = DateTime.Now.Subtract(dtLast);
            return Elapsed;
        }

        public static bool IsGreaterThanElapsedTime(string sKey, int nSeconds)
        {
            SystemKey sk = new SystemKey();
            sk.id = sKey;
            sk = GetObject<SystemKey>(sk);
            if (sk == null)
            {
                SetNVRamValue(sKey, "1");
            }
            TimeSpan ts = GetElapsedTime(sKey);
            if (ts.Seconds > nSeconds)
            {
                // Reset the write time to now.
                SetNVRamValue(sKey, "1");
                return true;
            }
            return false;
        }
        
    }
}
