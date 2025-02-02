using bbp.core.api.Controllers;
using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using Npgsql;
using System.Data;
using System.Text;
using uplink.NET.Models;
using static BMSCommon.Common;
using static BMSCommon.Extensions;
using static BMSCommon.Model.BitcoinSyncModel;

namespace BBPAPI
{
    public static class QuantBilling
    {
        public static List<MasternodeListItem> gSanctuaryList = new List<MasternodeListItem>();
        
		private static int nLastBilling = UnixTimestamp();
        public static async void Looper()
        {
            
			System.Threading.Thread.Sleep(30000);

            bool fPrimary = IsPrimary();
            if (!fPrimary)
            {
                Log("SyncBBPPhoneCharges::NotPrimary");
                return;
            }

            Log("QuantBilling::Looper Starting");

            while (true)
            {
                try
                {
                    await ChargeMonthlyQuantSubscriptionFees(false);
                    await DB.PhoneProcs.SyncBBPPhoneCharges();
                    int nElapsedBill = UnixTimestamp() - nLastBilling;
                    if (nElapsedBill > (60 * 60 * 4))
                    {
                        nLastBilling = UnixTimestamp();
                        await PerformStorjBilling();
                        await PerformSanctuarySync();
						gSanctuaryList = QuorumUtils.GetDatabaseObjects<MasternodeListItem>("SanctuaryList");
                    }
                }
                catch (Exception ex)
                {
                    Log("Looper::" + ex.Message);
                }
                System.Threading.Thread.Sleep(30000);
                
            }
        }

        public static async Task<bool> ChargeMonthlyQuantSubscriptionFees(bool fTestNet)
        {
            bool fPrimary = IsPrimary();
            if (!fPrimary)
                return false;

            if (DateTime.UtcNow.Day != 1)
                return false;

            bool fLatch = NVRAM.IsGreaterThanElapsedTime("ChargeMonthlyQuantSubscriptionFees", 60 * 60 * 8);
            if (!fLatch)
                return false;

            DataTable dt = null;// DB.GetSubscriptionsLast15Days();
            string sPAKey = fTestNet ? "tPoolAddress" : "PoolAddress";
            string sPoolAddress = GetConfigKeyValue(sPAKey);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sID = dt.Rows[i]["id"].ToString() ?? String.Empty;
                string sERCAddress = dt.Rows[i]["UserID"].ToString() ?? String.Empty;   
                string sBBPPrivKey = dt.Rows[i]["BBPPrivKey"].ToString() ?? String.Empty;
                string sPubKey = ERCUtilities.GetPubKeyFromPrivKey(fTestNet, sBBPPrivKey);
                double nFee = await PricingService.ConvertUSDToBiblePay(dt.Rows[i]["MonthlyCost"].AsDouble()) / 100;
                double nBalance = ERCUtilities.QueryAddressBalance(fTestNet, sPubKey);
                string sResult = String.Empty;
                if (nBalance > nFee)
                {
                    DACResult r = await ERCUtilities.SendBBPFromSubscription(fTestNet, sBBPPrivKey, sPoolAddress, 
                        nFee, "QuantSubscription");
                    if (r.TXID != String.Empty)
                    {
                        string sDesc = "Monthly subscription fee for " + dt.Rows[i]["Description"].ToString() + " - " + sID;
                        //DB.OperationProcs.InsertTxHistory(sERCAddress, "SUBSCRIPTION", sDesc, nFee,r.TXID);
                    }
                    else
                    {
                        sResult = "Charge Failed.";
                    }
                }
                else
                {
                    sResult = "Balance too low";
                }
                if (sResult != String.Empty)
                {
                    //DB.SetChargeAsNotBilledForSubscription(sID, sResult);
                }
            }

            return true;
        }

        internal static string DecAES2(string sData)
        {
            string sPrivKey = SecureString.GetDBConfigurationKeyValue("charges");
            return Encryption.DecryptAES256(sData, sPrivKey);
        }

        private static string GetBBPPrivKeyForStorjCharges(string sPubKey)
        {
            List<User> l = QuorumUtils.GetBBPDatabaseObjects<User>();
            l = l.Where(a => a.BBPAddress == sPubKey).ToList();
            string sDec = DecAES2(l[0].BBPPK);
            return sDec;
        }


        private static async Task<bool> DoStorjCharging(bool fTestNet, string sPubKey, double nTotalSize, double nTotalItems)
        {
            // Derive the key here to make the charge.
            double nCharge = nTotalSize / 10000000 / 30; // bill daily
            if (nCharge < .05)
            {
                return true;
            }
            string sPayload = "<storage></storage>";
            DACResult r0 = new DACResult();
            string sPrivKey = GetBBPPrivKeyForStorjCharges(sPubKey);

            if (sPrivKey != String.Empty)
            {
                r0 = await WebRPC.SendBBPOutsideChain(fTestNet, "storage", 
                    BMSCommon.Model.Global.FoundationPublicKey, sPrivKey, nCharge, sPayload);
            }
            else
            {
                r0.Error = "Charging key is not on file.";
            }
            string sID = Guid.NewGuid().ToString();
            string sRow = sID + "<col>" + DateTime.Now.ToString() + "<col>storage<col>" + nTotalSize.ToString() + "<col>" + nTotalItems.ToString()
                + "<col>" + nCharge.ToString() + "<col>" + r0.TXID.ToString() + "<col>" + r0.Error + "<row>\r\n";
            await AppendDataToDocument("historicalstoragecharges3", sPubKey, sRow);
            return true;
        }

        private static async Task<bool> AppendDataToDocument(string sTable, string sKey, string sData)
        {
            string s1 = await StorjIO.UplinkGetDatabaseData(sTable, sKey);
            s1 += sData + "\r\n";
            return await StorjIO.InternalStoreDatabaseData(sTable, sKey, s1);
        }
		public static bool IsPrimary()
		{
			string sBindURL = GetConfigKeyValue("bindurlapi");
            string sMaster = GetConfigKeyValue("master");
			bool fPrimary = sMaster.ToLower().Contains("true") || sBindURL.Contains("209.145.56.214");
            // mission critical TODO - Voting system
            // Log("Master=" + fPrimary.ToString());
			return fPrimary;
		}

        public static async Task<bool> PerformSanctuarySync()
        {
            // Upserts the sancs into Sanctuaries
            List<MasternodeListItem> l = WebRPC.GetMasternodeList(false);
            for (int i = 0; i < l.Count; i++)
            {
                l[i].Added = DateTime.Now;
            }
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<MasternodeListItem>(l);
            await CoreController.InsertChainObjectsInternal(co01);
			return true;
		}
		public static async Task<bool> PerformStorjBilling()
        {
			bool fPrimary = IsPrimary();
			if (!fPrimary)
            {
                return false;
            }

            bool fDo = NVRAM.IsGreaterThanElapsedTime("charges", 60 * 60 * 24);
            // glean root level keys
            ObjectList l = await StorjIO.StorjGetObjects("bbp0", String.Empty, false);
            
            for (int i = 0; i < l.Items.Count; i++)
            {
                uplink.NET.Models.Object o = l.Items[i];
                // this is a root key
                if (o.Key.Length == 35 && o.Key.Contains("/"))
                {
                    long nTotalSize = 0;
                    int nTotalItems = 0;
                    StringBuilder sbKeys = new StringBuilder();
                    ObjectList lSubFolder = await StorjIO.StorjGetObjects("bbp0", o.Key, true);
                    for (int j = 0; j < lSubFolder.Items.Count; j++)
                    {
                        uplink.NET.Models.Object oSubItem = lSubFolder.Items[j];
                        nTotalSize += oSubItem.SystemMetadata.ContentLength;
                        nTotalItems++;
                        bool bMasked = false;
                        if (lSubFolder.Items[j].Key.EndsWith(".ts"))
                            bMasked = true;
                        if (lSubFolder.Items[j].Key.Contains("accesstoken"))
                            bMasked = true;
                        if (!bMasked)
                        {
                            string sRow = o.Key + "<col>" + lSubFolder.Items[j].Key + "<col>" + oSubItem.SystemMetadata.ContentLength.ToString() + "<row>\r\n";
                            sbKeys.Append(sRow);
                        }
                    }
                    string sKey = o.Key.Replace("/", "");
                    // Physical Charging here:
                    if (fDo)
                    {
                        // Physically charge the key.
                        try
                        {
                            await DoStorjCharging(false, sKey, nTotalSize, nTotalItems);
                        }
                        catch (Exception ex)
                        {
                            Log("DoBilling::" + ex.Message);
                        }
                    }
                    // End of Physical Charging
                    // bill them here; create invoice mmddyyy.dat
                    await StorjIO.BillForStorage(sKey, nTotalSize, nTotalItems);
                    // Keep track of keys here:
                    await StorjIO.WriteStorjIndex(sKey, sbKeys);
                }
            }
            return false;
        }
    }
}
