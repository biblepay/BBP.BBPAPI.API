using bbp.core.api.Controllers;
using BBP.CORE.API.Utilities;
using BiblePay.BMS;
using BMSCommon.Model;
using Npgsql;
using System.Data;
using static BMSCommon.Common;
using static BMSCommon.Model.BitcoinSyncModel;


namespace BBPAPI
{
    public static partial class DB
    {
        public static class EthereumProcs
        {
            public static string GetContractConfig(string sChain, string sContractName)
            {
                List<ChainLink> lcl = QuorumUtils.GetBBPDatabaseObjects<ChainLink>();
                lcl = lcl.Where(a => a.chain == sChain && a.symbol.ToUpper() == sContractName.ToUpper()).ToList();
                string sContract = lcl[0].contract;
                return sContract;
            }
            public static string GetContractABI(string sContractName)
            {
                string sFullPath = Path.Combine(StartupConfig.WebRootPath, sContractName + ".json");
                string sData = System.IO.File.ReadAllText(sFullPath);
                return sData;
            }
        }

        public static class OperationProcs
        {
            public async static Task<bool> SaveTimeline(Timeline t)
			{
				t.Time = UnixTimestamp();
				t.Version = 1;
				t.id = Guid.NewGuid().ToString();
				return await QuorumUtils.InsertObject<Timeline>(t);
			}

			public static List<Timeline> GetTimeline(bool fTestNet, string sParentID)
			{
                List<Timeline> t = QuorumUtils.GetBBPDatabaseObjects<Timeline>();
                t = t.Where(a => a.ParentID == sParentID).ToList();
                t = t.OrderByDescending(s => Convert.ToDateTime(s.Added)).ToList();
                return t;
			}

			public static async Task<bool> SaveVideo(Video n)
			{
				n.Time = UnixTimestamp();
                List<Video> lVideo = new List<Video>();
                lVideo.Add(n);
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<Video>(lVideo);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }

            public static List<VideoNew> GetVideos(bool fTestNet, string id)
			{
				List<VideoNew> l = QuorumUtils.GetDatabaseObjects<VideoNew>("videonew");
				if (id != String.Empty)
				{
					l = l.Where(s => s.id == id).ToList();
				}
				return l;
			}

            public static List<VerseMemorizer> GetVerseMemorizers()
            {
                return QuorumUtils.GetBBPDatabaseObjects<VerseMemorizer>();
            }

			public static List<BBPMenu> GetMenu()
			{
                return QuorumUtils.GetBBPDatabaseObjects<BBPMenu>();
			}

			public static async Task<bool> StoreAttachment(BMSCommon.Model.Attachment a)
            {
                List<Attachment> l = new List<Attachment>();
                l.Add(a);
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<Attachment>(l);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }


            /*
            public static DataTable GetChats(bool fTestNet)
            {
                // set up the contactlistitems:
                string sTable = fTestNet ? "options.tuser" : "options.user";
                string sSQL = "Select * from " + sTable;
                NpgsqlCommand m1 = new NpgsqlCommand(sSQL);
                DataTable dt = DB.GetDataTable(m1);
                return dt;
            }
            */


            /*
            public static bool PersistDatabaseChatItem(BMSCommon.Model.ChatItem ci)
            {
                string sql = "insert into chat (id,added,Recipient,Sender,body) values (gen_random_uuid(), now(), @recipient, @sender, @body);";
                NpgsqlCommand m1 = new NpgsqlCommand(sql);
                m1.Parameters.AddWithValue("@recipient", ci.To);
                m1.Parameters.AddWithValue("@sender", ci.From);
                m1.Parameters.AddWithValue("@body", ci.body);
                bool fIns = ExecuteNonQuery(m1);
                return fIns;
            }
            */


            public static List<ChatNotification> GetNotifications(bool fTestNet, string sUserID)
            {
                List<ChatNotification> lcn = QuorumUtils.GetBBPDatabaseObjects<ChatNotification>();
                lcn = lcn.Where(a => a.UID == sUserID).ToList();
                return lcn;
            }
            public static List<Article> GetArticles(string type)
            {
                List<Article> l = QuorumUtils.GetBBPDatabaseObjects<Article>();
                return l;
            }

            
            /*
            public static bool ProvisionNewEmailUser( string sEmailAddress, string sNewPassword, string sFirstName, string sLastName)
            {
                string sql = "Insert into hm_accounts(accountdomainid, accountadminlevel, accountaddress, accountpassword, accountactive, accountisAd, accountAdDomain, accountAdUsername, "
                       + "AccountMaxSize, accountvacationMessageon, accountvacationmessage, accountvacationsubject, accountPwEncryption, accountForwardEnabled, AccountForwardAddress,"
                       + "AccountForwardKeeporiginal, AccountEnableSignature, AccountSignaturePlainText, AccountSignatureHtml, AccountLastLogonTime, AccountVacationExpires, AccountVacationExpireDate,"
                       + "AccountPersonFirstName, Accountpersonlastname) Values (2, 0, "
                       + "@NewEmailAddress, lower(CONVERT(VARCHAR(32),HashBytes('MD5', '" + sNewPassword + "'),2)),"
                       + "1, 0, '', '', 0, 0, '', '', 2, 0, '', 0, 0, '', '', getdate(), 0, getdate(), @FirstName, @LastName);";

                SqlCommand s = new SqlCommand(sql);
                s.Parameters.AddWithValue("@NewEmailAddress", sEmailAddress);
                s.Parameters.AddWithValue("@NewPassword", sNewPassword);
                s.Parameters.AddWithValue("@FirstName", sFirstName);
                s.Parameters.AddWithValue("@LastName", sLastName);
                //SQLDatabaseBBPAPI.ExecuteNonQuery(s, "seven");
                return true;
            }
            */



			public static async Task<bool> UpdateUserEmailAddressVerified(string sUserID)
            {
                List<User> users = QuorumUtils.GetBBPDatabaseObjects<User>();
                users = users.Where(a => a.id == sUserID).ToList();
                if (users.Count > 0)
                {
                    users[0].EmailAddressVerified = 1;
                    List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<User>(users);
                    await CoreController.InsertChainObjectsInternal(co01);
                }
                return true;
            }


            public static List<RDPConnection> GetRDPConnections(RDPConnection r0)
            {
                List<RDPConnection> l00 = QuorumUtils.GetBBPDatabaseObjects<RDPConnection>();
                l00 = l00.Where(a => a.OwnerAddress == r0.OwnerAddress).ToList();
                string sDecPubKey = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(false, r0.Key);
                if (r0.OwnerAddress != sDecPubKey)
                {
                    return null;
                }
                return l00;
            }

            public static RDPAccount GetRDPAccount(RDPAccount r0, string sSearchPubKey)
            {
                // Retrieve rdp connections for this address
                List<RDPAccount> l00 = QuorumUtils.GetBBPDatabaseObjects<RDPAccount>();
                if (sSearchPubKey != String.Empty)
                {
                    l00 = l00.Where(a => a.PublicKey == sSearchPubKey).ToList();
                }
                else
                {
                    l00 = l00.Where(a => a.id == r0.id).ToList();
                }
                if (l00.Count < 1)
                {
                    return null;
                }
                return l00[0];
            }

            public static async Task<bool> InsertRDPConnection(RDPConnection r)
            {
                if (!String.IsNullOrEmpty(r.Status))
                {
                    bool f = await QuorumUtils.StoreDataByType3<RDPConnection>(r);
                    return f;
                }
                if (!WebRPC.ValidateAddress(false, r.OwnerAddress) || !WebRPC.ValidateAddress(false, r.Address))
                {
                    return false;
                }
                if (r.Nickname == "deleted")
                {
                    bool f2 = await QuorumUtils.StoreDataByType3<RDPConnection>(r);
                    return f2;
                }
                bool f3 = await QuorumUtils.StoreDataByType3<RDPConnection>(r);
                return f3;
            }

            public static async Task<bool> PersistUser(User u)
            {
                if (String.IsNullOrEmpty(u.BBPAddress))
                {
                    return false;
                }
                u.ERC20Address = u.BBPAddress;
                u.Updated = DateTime.Now;
                bool fSaved = await QuorumUtils.InsertObject<User>(u);
                return fSaved;
            }

            public static async Task<bool> UpsertRDPAccount(RDPAccount r)
            {
                bool f3 = await QuorumUtils.StoreDataByType3<RDPAccount>(r);
                return f3;
            }

            public static List<ExtensionObject> RetrieveExtensions()
            {
                List<ExtensionObject> l = QuorumUtils.GetBBPDatabaseObjects<ExtensionObject>();
                return l;
            }
            public static double GetUserCountByEmail(string sEmail)
            {
                List<User> u0 = QuorumUtils.GetBBPDatabaseObjects<User>();
                u0 = u0.Where(a => a.Email2 == sEmail).ToList();
                return u0.Count;
            }

            public static List<Well> GetWellsReport()
            {
                List<Well> l = QuorumUtils.GetDatabaseObjects<Well>("Well");
                return l;
            }

            public static List<Pin> GetWellsPinsReport()
            {
                List<Pin> pins = QuorumUtils.GetDatabaseObjects<Pin>("Pin");
                pins = pins.Where(a => a.URL.Contains("well")).ToList();
                return pins;
            }
        }
    }
}
