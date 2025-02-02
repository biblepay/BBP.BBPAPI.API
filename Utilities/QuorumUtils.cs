using bbp.core.api.Controllers;
using BBP.CORE.API.Database;
using BBP.CORE.API.Service;
using BBP.CORE.API.Service.Doggy;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using BMSShared;
using System.Data;
using System.Reflection;
using System.Text;
using static BMSCommon.Common;
using static BMSCommon.Model.BitcoinSyncModel;

namespace BBP.CORE.API.Utilities
{
    public class QuorumNode
    {
        public string URL { get; set; }
        public bool Master { get; set; }
        public QuorumNode()
        {
            Master = false;
            URL = String.Empty;
        }
    }

    public static class QuorumSyncer
    {

        /*
        private static async Task<bool> OneTimeInit()
        {
            for (int i = 1; i <= 72; i++)
            {
                QuorumUtils.CommitBlockLocally(i);
                QuorumUtils.CommitViewBlockLocally(i);
                int iBlockNum = QuorumUtils.GetBestBlock();
                await QuorumUtils.PushBlockExternally(i);
                await QuorumUtils.GetBlockDataExternally(i);
                bool f14 = false;
            }
            return true;
        }
        */


        private static async Task<bool> SyncGenesisKey(string sKeyName)
        {
            try
            {
                string sKV = NVRAM.GetNVRamValue(sKeyName);
                if (String.IsNullOrEmpty(sKV))
                {
                    DatabaseQuery db1 = new DatabaseQuery();
                    db1.Key = sKeyName;
                    db1.SanctuaryPrivateKey = BMSCommon.Common.GetConfigKeyValue("sancprivkey");
                    db1 = await CoreController.GetGenesisBlockRequest(db1);
                    if (db1 != null)
                    {
                        if (!String.IsNullOrEmpty(db1.Value) && db1.Value != "501")
                        {
                            NVRAM.SetNVRamValue(sKeyName, db1.Value);
                        }
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        public static async Task<bool> InitializeGenesisBlock()
        {
            /* Genesis Block Operations */
            
            await SyncGenesisKey("storjrokey");
            await SyncGenesisKey("storjrwkey");
            // Reach out to master for genesis block
            await RefreshSanctuaryList();
            return true;
        }

        public static List<MasternodeListItem> mgSanctuaries = null;

        private static async Task<bool> RefreshSanctuaryList()
        {
            bool fLatch = NVRAM.IsGreaterThanElapsedTime("RefreshSanctuaryList", 60 * 60 * 1);
            if (fLatch || mgSanctuaries == null)
            {
                mgSanctuaries = WebRPC.GetMasternodeList(false);
            }
            return true;
        }


        public static async void Looper()
        {
            // Initialize the Block List and the Block View List
            BlockList bl = new BlockList();
            Sqlite.CreateView(bl);
            await InitializeGenesisBlock();
            System.Threading.Thread.Sleep(5000);

            while (true)
            {
                // Check for new blocks
                try
                {
                    await Sync();
                    QuorumUtils.ProcessIntoViews();
                    await RefreshSanctuaryList();
                    await Dogone.MatchAtomicTrades();
                    await KeyServer.GenKeys();
                }
                catch (Exception ex)
                {
                    BMSCommon.Common.Log("QuorumSyncer::" + ex.Message);
                }
                System.Threading.Thread.Sleep(15000);

            }
        }
        public static async Task<bool> Sync()
        {
            int iBestBlock = QuorumUtils.GetBestBlock();
            int iStart = iBestBlock + 1;
            int iEnd = iStart + 50;
            for (int i = iStart; i <= iEnd; i++)
            {
                bool fGot = await QuorumUtils.GetBlockDataExternally(i);
                if (!fGot)
                {
                    return false;
                }
            }
            return true;
        }
    }


    public static class QuorumUtils
    {

        public static string TWELVE_TRIBES_OF_ISRAEL = "Reuben,Simeon,Levi,Judah,Dan,Naphtali,Gad,Asher,Issachar,Zebulun,Joseph,Benjamin";
        public static List<QuorumNode> QuorumNodes = new List<QuorumNode>();
        
        public static string GetFullyQualifiedName(object o)
        {
            string fullyQualifiedName = o.GetType().FullName;
            return fullyQualifiedName;
        }

        public static int GetBestBlock()
        {
            string sql = "Select max(BlockNumber) bn from bmscommon_model_BlockList;";
            DataTable dt = Sqlite.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                int n = dt.Rows[0]["bn"].ToInt32();
                return n;
            }
            return 0;
        }
        public static bool CommitBlockLocally(int nBlockNumber)
        {
            BlockList bl = new BlockList();
            bl.BlockNumber = nBlockNumber;
            bl.Added = DateTime.Now;
            bl.ProcessedView = 0;
            Sqlite.UpsertObject(bl);
            return true;
        }

        public static async Task<bool> PushBlockExternally(int nBlockNumber)
        {
            string sBlockPath = GetFolder("Database", nBlockNumber.ToString() + ".block");
            if (!System.IO.File.Exists(sBlockPath))
            {
                return false;
            }
            string sData = System.IO.File.ReadAllText(sBlockPath);
            // Storj
            string sKey = "block_" + nBlockNumber.ToString();
            bool f = await StorjIO.InternalStoreDatabaseData("block", sKey, sData);
            return f;
        }

        public static async Task<bool> GetBlockDataExternally(int nBlockNumber)
        {
            string sBlockPath = GetFolder("Database", nBlockNumber.ToString() + ".block");
            string sKey = "block_" + nBlockNumber.ToString();
            string s = await StorjIO.UplinkGetDatabaseData("block", sKey);
            if (String.IsNullOrEmpty(s))
            {
                return false;
            }
            if (true)
            {
                System.IO.File.WriteAllText(sBlockPath, s);
                CommitBlockLocally(nBlockNumber);
            }
            return true;
        }

        public static bool CommitViewBlockLocally(int nBlockNumber)
        {
            string sql = "Update BMSCommon_model_BlockList set ProcessedView=1,UpdatedView='" + DateTime.Now.ToIsoDate() + "' WHERE BlockNumber='" + nBlockNumber.ToStr() + "';";
            Sqlite.ExecuteNonQuery(sql);
            return true;
        }
        public static Type GetTypeEx(string fullTypeName)
        {
            return Type.GetType(fullTypeName) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType(fullTypeName))
                            .FirstOrDefault(t => t != null);
        }

        public static bool AddToMemoryPool(List<ChainObject> l)
        {
            // In this area, we append the chain objects into our memory pool file.
            string path = BMSCommon.Common.GetFolder("Database", "memorypool.dat");
            // Append text to the file
            // Note that we removed the CRLFs out of the object, so we can continue to use the OS's LF character as the file delimiter.
            using (StreamWriter sw = File.AppendText(path))
            {
                for (int i = 0; i < l.Count; i++)
                {
                    string sData = l[i].ReturnCleanSerialized();
                    sw.WriteLine(sData);
                }
            }
            return true;
        }

        internal static List<ChainObject> GetBlock(BlockRequest br)
        {
            List<ChainObject> co = new List<ChainObject>();
            string sBlockPath = GetFolder("Database", br.BlockNumber.ToString() + ".block");
            if (!System.IO.File.Exists(sBlockPath))
            {
                return co;
            }
            using (StreamReader reader = new StreamReader(sBlockPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string sClean = ChainObject.ReturnUnserializedWithCRLF(line);
                    ChainObject c = Newtonsoft.Json.JsonConvert.DeserializeObject<ChainObject>(sClean);
                    co.Add(c);
                }
            }
            return co;
        }


        public static string GetListHash(List<ChainObject> listChainObjects)
        {
            string sHash = String.Empty;
            string sData = String.Empty;
            StringBuilder combinedHash = new StringBuilder();
            foreach (ChainObject l in listChainObjects)
            {
                l.Hash = l.GetHash();
                combinedHash.Append(l.Hash);
            }
            sHash = Encryption.GetSha256HashI(combinedHash.ToStr());
            return sHash;
        }


        public async static Task<List<ChainObject>> ConvertObjectsToChainObjects<T>(List<T> l)
        {
            List<ChainObject> lObjects = new List<ChainObject>();
            for (int i = 0; i < l.Count; i++)
            {
                T obj = l[i];
                ChainObject co = new ChainObject();
                co.Data = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                co.FullyQualifiedName = QuorumUtils.GetFullyQualifiedName(obj);
                lObjects.Add(co);
            }
            return lObjects;
        }

        private async static Task<List<T>> ConvertThem<T>(List<T> myList)
        {
              List<ChainObject> co01 = await ConvertObjectsToChainObjects<T>(myList);
              await CoreController.InsertChainObjectsInternal(co01);
              List<T> u01 = QuorumUtils.GetBBPDatabaseObjects<T>();
              return u01;
        }

        public static bool StoreDataByType2<T>(T o)
        {
            List<T> l = new List<T>();
            l.Add(o);
            List<ChainObject> co01 = QuorumUtils.ConvertObjectsToChainObjects<T>(l).Result;
            DACResult dOK = CoreController.InsertChainObjectsInternal(co01).Result;
            return dOK.Result;
        }
        internal static async Task<bool> StoreDataByType3<T>(T o)
        {
            List<T> l = new List<T>();
            l.Add(o);
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<T>(l);
            DACResult dOK = await CoreController.InsertChainObjectsInternal(co01);
            return dOK.Result;
        }

        internal static bool StoreDataByType(DatabaseQuery q)
        {
            Type t = GenericTypeManipulation.GetTypeEx(q.FullyQualifiedName);
            var myClassType = Assembly.GetExecutingAssembly().GetType("BBP.CORE.API.Utilities.QuorumUtils");
            MethodInfo method = myClassType.GetMethod("StoreDataByType2");
            MethodInfo methodBuild = method.MakeGenericMethod(new Type[] { t });
            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(q.BusinessObjectJson, t);
            dynamic f = methodBuild.Invoke(method, new object[] { obj });
            return f;
        }


        public static dynamic GetDatabaseObjectsByType(DatabaseQuery q)
        {
            Type t = GenericTypeManipulation.GetTypeEx(q.FullyQualifiedName);
            var myClassType = Assembly.GetExecutingAssembly().GetType("BBP.CORE.API.Utilities.QuorumUtils");
            MethodInfo method = myClassType.GetMethod("GetDatabaseObjects");
            MethodInfo methodBuild = method.MakeGenericMethod(new Type[] { t });
            dynamic l = methodBuild.Invoke(method, new object[] { q.TableName });
            return l;
        }

        internal static async Task<bool> InsertObject<T>(T o)
        {
            return await StoreDataByType3<T>(o);
        }

        public static List<T> GetDatabaseObjects<T>(string sTableName) where T : new()
        {
            List<T> l = QuorumUtils.GetBBPDatabaseObjects<T>();
            return l;
        }

        internal static Type GetPropertyType1(object p)
        {
            try
            {
                Type m = p.GetType();
                return m;
            }
            catch (Exception)
            {
                return typeof(String);
            }
        }

        public static string GetPubKeyFromPrivKey(string sPrivKey, bool fTestNet)
        {
            string sPubKey = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(fTestNet, sPrivKey);
            return sPubKey;
        }

        public static bool VerifyTemple(string sPrivKey)
        {
            int nTS = BMSCommon.Common.UnixTimestamp();
            string sSig = ERCUtilities.SignMessage(false, sPrivKey, nTS.ToStr());
            string sPubKey = GetPubKeyFromPrivKey(sPrivKey, false);
            List<MasternodeListItem> lSancs = QuorumSyncer.mgSanctuaries.Where(a => a.votingaddress.ToLower().Trim() == sPubKey.ToLower().Trim()
                    && a.collateral_amount > 40000000).ToList();
            bool bTempleSigVerified = ERCUtilities.VerifySignature(false, sPubKey, nTS.ToStr(), sSig);
            bool fOK =  (lSancs.Count > 0 && bTempleSigVerified) ;
            if (fOK)
            {
                BMSCommon.Common.Log("Temple verification succeeded for " + sPubKey);
            }
            else
            {
                BMSCommon.Common.Log("Temple verification failed for " + sPubKey);
            }
            return fOK;
        }

        public static bool VerifyChainObject(ChainObject co)
        {
            List<MasternodeListItem> lSancs = QuorumSyncer.mgSanctuaries.Where(a => a.votingaddress.ToLower().Trim() 
                == co.SanctuaryPublicKey.ToLower().Trim() && a.collateral_amount > 40000000).ToList();
            bool bTempleSigVerified = ERCUtilities.VerifySignature(false, co.SanctuaryPublicKey, co.Time.ToStr(), co.SanctuarySignature);
            bool fOK = (lSancs.Count > 0 && bTempleSigVerified);
            return fOK;
        }

        public static ChainResponse HashChainObjects(List<ChainObject> chainObjects, User u, SanctuaryAuthority sa)
        {
            ChainResponse response = new ChainResponse();
            string sPrivKeyMainNet = String.Empty;
            int nTS = BMSCommon.Common.UnixTimestamp();
            u.BBPAddress = GetPubKeyFromPrivKey(u.BBPPrivKeyMainNet, false);
            sa.PublicKey = GetPubKeyFromPrivKey(sa.SanctuaryVotingAddressPrivateKey, false);
            List<MasternodeListItem> r2 = WebRPC.GetMasternodeList(false);
            List<MasternodeListItem> lSancs = r2.Where(a => a.votingaddress.ToLower().Trim() == sa.PublicKey.ToLower().Trim()
                    && a.collateral_amount > 40000000).ToList();
            string sSig = ERCUtilities.SignMessage(false, u.BBPPrivKeyMainNet, nTS.ToStr());
            string sSancSig  = ERCUtilities.SignMessage(false, sa.SanctuaryVotingAddressPrivateKey, nTS.ToStr());
            for (int i = 0; i < chainObjects.Count; i++)
            {
                // Insert the sub objects first
                chainObjects[i].Time = nTS;
                chainObjects[i].Added = DateTime.Now;
                chainObjects[i].Sequence = (short)i;
                chainObjects[i].BBPAddress = u.BBPAddress;
                chainObjects[i].Version = 1;
                chainObjects[i].SanctuaryPublicKey = sa.PublicKey;
                chainObjects[i].Hash = chainObjects[i].GetHash();
                chainObjects[i].Signature = sSig;
                chainObjects[i].SanctuarySignature = sSancSig;
                /* NOTE: The chain object is pre-verified in "VerifyChainObject()" so we do not need to check every individual tx at this level.
                 * I'm leaving this code in as a comment in case we need to add user based permissions on objects here.
                 * 
                bool bUserVerified = ERCUtilities.VerifySignature(false, chainObjects[i].BBPAddress, chainObjects[i].Time.ToStr(), chainObjects[i].Signature);
                bool bSancVerified = ERCUtilities.VerifySignature(false, chainObjects[i].SanctuaryPublicKey, chainObjects[i].Time.ToStr(), chainObjects[i].SanctuarySignature);
                bool bSancFullyVerified = bSancVerified && lSancs.Count > 0;
                if (!bSancFullyVerified)
                {
                    cr.ErrorMessage = "Tx #" + i.ToString() + " contains an invalid sanctuary signature.";
                    return cr;
                }
                // PLACEHOLDER to check permissions here
                // If user does not have permissions throw an error
                // Optionally: Determine if its an Insert or Edit here.
                */
            }
            response.RootHash = GetListHash(chainObjects);
            return response;
        }

        private static dynamic TryCreateInstance(Type t)
        {
            try
            {
                dynamic oSample = Activator.CreateInstance(t);
                return oSample;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public static bool ProcessView(int nBlockNum)
        {
            try
            {
                // Anything in this block, we need to dissect the chainobjects, and append those into the individual views.
                BlockRequest br = new BlockRequest();
                br.BlockNumber = nBlockNum;
                List<ChainObject> co = GetBlock(br);
                if (co.Count < 1)
                {
                    return false;
                }

                // Gather distinct view types
                List<string> lFQNames = new List<string>();
                for (int i = 0; i < co.Count; i++)
                {
                    ChainObject c = co[i];
                    string sFQN = c.FullyQualifiedName;
                    if (!lFQNames.Contains(sFQN))
                    {
                        lFQNames.Add(sFQN);
                    }
                }
                // ENSURE VIEWS EXIST HERE.
                foreach (string sFQName in lFQNames)
                {
                    // ensure views exist.
                    Type t = GenericTypeManipulation.GetTypeEx(sFQName);
                    if (t != null)
                    {
                        dynamic oSample = TryCreateInstance(t);
                        Sqlite.CreateView(oSample);
                    }
                }

                // Now upsert the objects
                StringBuilder sbUpsert = new StringBuilder();
                sbUpsert.AppendLine("BEGIN TRANSACTION;");
                int iBatchSeq = 0;
                for (int i = 0; i < co.Count; i++)
                {
                    ChainObject c = co[i];
                    Type t = GenericTypeManipulation.GetTypeEx(c.FullyQualifiedName);

                    if (t != null)
                    {
                        dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(c.Data, t);
                        string sUpsert = Sqlite.ScriptUpsertObject(obj);
                        sbUpsert.AppendLine(sUpsert);
                        iBatchSeq++;
                        if (iBatchSeq > 500)
                        {
                            sbUpsert.AppendLine("COMMIT;");
                            bool f1 = Sqlite.ExecuteNonQuery(sbUpsert.ToString());
                            sbUpsert.Clear();
                            sbUpsert.AppendLine("BEGIN TRANSACTION;");
                            iBatchSeq = 0;
                        }
                    }
                }
                sbUpsert.AppendLine("COMMIT;");
                bool f = Sqlite.ExecuteNonQuery(sbUpsert.ToString());
                //Add the block as processed
                CommitViewBlockLocally(nBlockNum);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public static string GetPrimaryKeyName(Type t)
        {
            PropertyInfo[] properties = t.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                Console.WriteLine("Property Name: " + property.Name);
                Attribute[] cattrs = Attribute.GetCustomAttributes(property);
                foreach (Attribute ca in cattrs)
                {
                    var caType = ca.TypeId;
                    string sType = ca.ToString();
                    if (sType.Contains("PrimaryKey"))
                    {
                        return property.Name;
                    }
                }
            }
            return String.Empty;
        }


        public static List<T> GetBBPDatabaseObjects<T>(string sFolderName="Database")
        {
            List<T> oObjects = new List<T>();
            oObjects = Sqlite.GetViewObjects<T>();
            return oObjects;
        }


        public static bool ProcessIntoViews()
        {
            // For each block that was not processed in a view, pull it into the view.
            string sql = "Select BlockNumber From bmscommon_model_BlockList where ProcessedView = 0 order by BlockNumber;";
            DataTable dt = Sqlite.GetDataTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int nBlockNum = dt.Rows[i]["BlockNumber"].ToInt32();
                // We have not processed this view yet.
                ProcessView(nBlockNum);
            }
            return true;
        }


        public static async Task<bool> MakeNewBlock()
        {
            // Anything in the memory pool can be added to the block, then we clear the memory pool.
            // We make a block according to the current unix timestamp.  Since blocks are never created more than once per 30 seconds these will be unique.
            // The block list is stored in a local file named blocks.dat, and this file is appended as new blocks are added.
            // New nodes that sync receive a block list from another node first.

            int iBlockNumber = GetBestBlock() + 1;
            if (iBlockNumber < 70)
            {
                throw new Exception("Bad Best Block");
            }
            // MISSION CRITICAL TODO!   WE MUST MAKE THIS SUPER SAFE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            string sBlockPath = BMSCommon.Common.GetFolder("Database", iBlockNumber + ".block");
            string sMemPoolPath = BMSCommon.Common.GetFolder("Database", "memorypool.dat");

            StringBuilder sb = new StringBuilder();
            int iInserted = 0;
            using (StreamReader reader = new StreamReader(sMemPoolPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string sClean = ChainObject.ReturnUnserializedWithCRLF(line);
                    ChainObject co = Newtonsoft.Json.JsonConvert.DeserializeObject<ChainObject>(sClean);
                    if (co != null)
                    {
                        if (co.Time > 1)
                        {
                            // Put basic chain object checks here that would disqualify them from being in a block.  
                            // Technically they shouldnt even be in the memory pool.
                            // At this point, it is at least a chainObject.
                            co.BlockNumber = iBlockNumber;
                            string sNewData = co.ReturnCleanSerialized();
                            sb.AppendLine(sNewData);
                            iInserted++;
                        }
                    }
                }
                if (sb.Length > 0)
                {
                    System.IO.File.WriteAllText(sBlockPath, sb.ToString());
                }
            }
            // Clear the memory pool.
            System.IO.File.Delete(sMemPoolPath);
            if (iInserted > 0)
            {
                await PushBlockExternally(iBlockNumber);

                CommitBlockLocally(iBlockNumber);
                // Relay the new block to the network
                // As of now this is done by remote node polling our api for blockcount, then the remote node asks for inventory.
                // Read the block into views.
                ProcessIntoViews();
            }
            return true;
        }
    }
}

