using bbp.core.api.Controllers;
using BBP.CORE.API.Database;
using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using NBitcoin;
using Newtonsoft.Json;
using System.Data;
using Voximplant.API;
using Voximplant.API.Response;
using static BMSCommon.Common;
using static BMSCommon.Extensions;
using static BMSCommon.Model.BitcoinSyncModel;


namespace BBPAPI
{
    public static partial class DB
    {
        public static class PhoneProcs
        {
           
            public static PhoneUser GetPhoneUser(User u)
            {
                bool fProvisionMode = false;
                if (System.Diagnostics.Debugger.IsAttached && false)
                {
                    fProvisionMode = true;
                }

                PhoneUser pu = DB.PhoneProcs.GetDbPhoneUser(u);

                if (pu != null && !fProvisionMode)
                {
                    pu.bbpaddress = ERCUtilities.GetPubKeyFromPrivKey(false, pu.bbppk);
					phonevoicemailsettings vms = DB.PhoneProcs.GetVoicemailSettings(pu.id);
                    if (vms != null)
                    {
                        pu.Greeting = vms.greeting;
                        pu.AnswerDuration = (int)vms.answerduration.ToDouble();
                    }
                    if (String.IsNullOrEmpty(pu.Greeting))
                    {
                        pu.Greeting = DB.PhoneProcs.GetDefaultGreeting();
                    }
                    if (pu.AnswerDuration == 0)
                    {
                        pu.AnswerDuration = 45;
                    }

                }
                else
                {
                    pu.bbpaddress = ERCUtilities.GetPubKeyFromPrivKey(false, u.BBPPrivKeyMainNet);
                    pu.bbppk = u.BBPPrivKeyMainNet;
                }
                pu.WalletBalance = BBPAPI.WebRPC.QueryAddressBalanceCached(false, pu.bbpaddress, 120);
                pu.OutstandingOwed = DB.PhoneProcs.GetOutstandingOwed(pu.id);
                if (pu.WalletBalance < pu.OutstandingOwed)
                {
                    pu.Error = "Warning, your outstanding balance exceeds your wallet balance.  Please make a payment. ";
                    pu.UserPassword = String.Empty;
                }
                return pu;
            }

            private static VoximplantAPI GetPhoneImplant()
            {
                string s2 = BBPAPI.SecureString.GetDBConfigurationKeyValue("voximplant");
                if (s2 == String.Empty)
                {
                    return null;
                }
                string sConfigFN = Encryption.GetSha256String("vox").Substring(0,10) + ".dat";
                string sConfigFull = Path.Combine(Path.GetTempPath(), sConfigFN);
                if (!System.IO.File.Exists(sConfigFull))
                {
                    System.IO.File.WriteAllText(sConfigFull, s2);
                    System.IO.File.SetAttributes(sConfigFull, File.GetAttributes(sConfigFull) | FileAttributes.Hidden);
                }

                string sMyTempData = System.IO.File.ReadAllText(sConfigFull);
                
                var config = new ClientConfiguration
                {
                    KeyFile = sConfigFull
                };
                VoximplantAPI voximplant = new VoximplantAPI(config);
                return voximplant;
            }


            public static string GetDefaultGreeting()
            {
                string sGreeting = "Hello Compadre!  You have reached my Voice Mailbox!  Please leave a message after the tone.";
                return sGreeting;
            }
            public static PhoneUser GetPhoneUserRouting(PhoneCallerDestination dz)
            {
                if (dz.CallerID == String.Empty) dz.CallerID = "UNKNOWN";
                if (dz.Destination == String.Empty) dz.Destination = "UNKNOWN";
                dz.Destination  = dz.Destination.Replace("+", "");
                dz.CallerID = dz.CallerID.Replace("+", "");
                List<PhoneUser> lpuz = new List<PhoneUser>();
                lpuz = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                lpuz = lpuz.Where(a => a.PhoneNumber == dz.CallerID || a.PhoneNumber == dz.Destination || a.UserName == dz.Destination || a.UserName == dz.CallerID).ToList();
                if (lpuz.Count > 0)
                {
                    lpuz[0].WalletBalance = ERCUtilities.QueryAddressBalance(false, lpuz[0].bbpaddress);
                    lpuz[0].OutstandingOwed = DB.PhoneProcs.GetOutstandingOwed(lpuz[0].id);
                    lpuz[0].GoodStanding = lpuz[0].WalletBalance > lpuz[0].OutstandingOwed;
                    // voicemail stuff
                    phonevoicemailsettings vms = new phonevoicemailsettings();// DB.PhoneProcs.GetVoicemailSettings(lpuz[0].id);
                    if (vms != null)
                    {
                        lpuz[0].Greeting = vms.greeting;
                        lpuz[0].AnswerDuration = vms.answerduration;
                    }
                    if (String.IsNullOrEmpty(lpuz[0].Greeting))
                    {
                        lpuz[0].Greeting = GetDefaultGreeting();
                    }
                    if (lpuz[0].AnswerDuration == 0)
                    {
                        lpuz[0].AnswerDuration = 45;
                    }
                    return lpuz[0];
                }
                return null;
            }

            public static phonevoicemailsettings GetVoicemailSettings(int nUserID)
            {
                List<phonevoicemailsettings> lpvms = new List<phonevoicemailsettings>();
                lpvms = QuorumUtils.GetBBPDatabaseObjects<phonevoicemailsettings>();
                lpvms = lpvms.Where(a => a.userid == nUserID).ToList();
                return lpvms[0];
            }

            public static string GetPhoneUserNameBasedOnRecordCount(User u)
            {
                List<PhoneUser> lpu = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                lpu = lpu.Where(a => a.bbpaddress == u.BBPAddress).ToList();
                if (lpu == null || lpu.Count == 0)
                {
                    return u.BBPAddress;
                }
                string sNewAddress = u.BBPAddress + "-" + lpu.Count.ToString();
                return sNewAddress;
            }

            public static string ProcessVoicemail(PhoneCallerDestination pcd)
            {
                PhoneCallerDestination pcdCaller = new PhoneCallerDestination();
                pcdCaller.CallerID = pcd.CallerID;
                PhoneCallerDestination pcdDest = new PhoneCallerDestination();
                pcdDest.Destination = pcd.Destination;

                PhoneUser pCaller = GetPhoneUserRouting(pcdCaller);
                PhoneUser pDest = GetPhoneUserRouting(pcdDest);

                if (pDest != null)
                {
                    // User to PSTN
                    return "1";
                }
                else
                {
                    Log("4001.1::no destination phone");
                    return "-1";
                }
            }

            public static string GetRoute(PhoneCallerDestination d1)
            {
                // CallerID contains the bbp address if they are making a call
                PhoneCallerDestination dCaller = new PhoneCallerDestination();
                dCaller.CallerID = d1.CallerID;
                PhoneCallerDestination dRecipient = new PhoneCallerDestination();
                dRecipient.Destination = d1.Destination;
                PhoneUser pCaller = GetPhoneUserRouting(dCaller);
                PhoneUser pDest = GetPhoneUserRouting(dRecipient);
                string sRoute = String.Empty;
                string sRule = String.Empty;
                string sOutCallerID = String.Empty;
                string sOutBBPBalance = String.Empty;
                string sOutOutstandingOwed = String.Empty;
                string sOutGoodStanding = String.Empty;
                string sOutRate = String.Empty;
                string sGreeting = String.Empty;
                string sAnswerDuration = String.Empty;

                if (pCaller != null && pDest != null)
                {
                    // User to user call.  Route to user
                    sRoute = "USERTOUSER";
                    sRule = pCaller.bbpaddress + "|" + pDest.UserName;
                    sOutCallerID = pCaller.PhoneNumber;
                    sOutBBPBalance = pCaller.WalletBalance.ToString();
                    sOutOutstandingOwed = pCaller.OutstandingOwed.ToString();
                    sOutGoodStanding = pCaller.GoodStanding ? "1" : "0";
                    sGreeting  = pCaller.Greeting;
                    sAnswerDuration = pCaller.AnswerDuration.ToString();
                    sOutRate = "0";
                }
                else if (pCaller != null && pDest == null)
                {
                    // User to PSTN
                    sRoute = "USERTOPSTN";
                    sRule = pCaller.UserName + "|" + d1.Destination;
                    sOutCallerID = pCaller.PhoneNumber;
                    sOutBBPBalance = pCaller.WalletBalance.ToString();
                    sOutOutstandingOwed = pCaller.OutstandingOwed.ToString();
                    sOutGoodStanding = pCaller.GoodStanding ? "1" : "0";
                    sOutRate = GetLongDistanceRate(d1.Destination, pCaller.id).ToString();
                    sGreeting = pCaller.Greeting;
                    sAnswerDuration = pCaller.AnswerDuration.ToString();

                }
                else if (pDest != null && pCaller == null)
                {
                    // Someone from PSTN to User
                    sRoute = "PSTNTOUSER";
                    sRule = d1.Destination + "|" + pDest.UserName;
                    sOutCallerID = d1.CallerID;
                    sOutBBPBalance = pDest.WalletBalance.ToString();
                    sOutOutstandingOwed = pDest.OutstandingOwed.ToString();
                    sOutGoodStanding = pDest.GoodStanding ? "1" : "0";
                    sOutRate = "0.01";
                    sGreeting = pDest.Greeting;
                    sAnswerDuration = pDest.AnswerDuration.ToString();
                }
                else
                {
                    sRoute = "UNKNOWN";
                    sRule = "UNKNOWN|UNKNOWN";
                    sOutCallerID = "UNKNOWN";
                }

                string sVersion = "2.0";
                string sOut = sRoute + "|" + sRule + "|" + sOutCallerID + "|" + sOutBBPBalance + "|" + sOutOutstandingOwed
                    + "|" + sOutGoodStanding + "|" + sOutRate + "|" + sGreeting + "|" + sAnswerDuration + "|" + sVersion;
                return sOut;
            }

            public static double GetLongDistanceRate(string sPhoneNumber, int phoneuserid)
            {
                List<PhoneRate> lpr = QuorumUtils.GetBBPDatabaseObjects<PhoneRate>();
                string sPrefix = sPhoneNumber.Substring(1, sPhoneNumber.Length - 1);
                lpr = lpr.Where(a => a.prefix.Contains(sPhoneNumber)).ToList();
                if (lpr == null || lpr.Count == 0)
                {
                    return 0;
                }
                return lpr[0].rate;
            }


            public static DataTable GetRatesReport(double nBBPAmountUSD)
            {
                string sql = "select Country, Round(rate*1.3,2) as rate1,Round(rate*1.3*100*" + nBBPAmountUSD.ToString() 
                    + ",2)  Rate2,Description,max(substring(prefix, 1, 4)) Prefix "
                    + " from BMSCommon_Model_Phonerate where   "
                    + "  (rate < .05 or country='PH') and rate > 0.0001 group by country,rate,description order by country;";
                DataTable dt = Sqlite.GetDataTable(sql);
                return dt;
            }
            public static PhoneUser GetDbPhoneUser(User u)
            {
                List<PhoneUser> lpu = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                lpu = lpu.Where<PhoneUser>(a => a.bbppk == u.BBPPrivKeyMainNet).ToList();
                if (lpu.Count < 1)
                {
                    return null;
                }
                return lpu[0];
            }

            public static double GetOutstandingOwed(int nUserID)
            {
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where(a => a.userid == nUserID).ToList();
                double nSum = lpch.Sum(a => a.amountbbp);
                return nSum;
            }


            private static string GetFullUserName(string sUserName)
            {
				string sDomain = "@biblepay.robandrews.n2.voximplant.com";
				string sFullName = (sUserName.Contains("biblepay.robandrews")) ? sUserName : sUserName + sDomain;
                return sFullName;
			}

			public static async Task<bool> InsertPhoneUser(NewPhoneUser pu)
            {
                List<NewPhoneUser> l0 = new List<NewPhoneUser>();
                pu.NewUserName = pu.Address;
                l0.Add(pu);
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<NewPhoneUser>(l0);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }
            public static async Task<bool> SetPhoneUserPhoneNumber(NewPhoneUser n)
            {
                List<NewPhoneUser> lnpu = new List<NewPhoneUser>();
                lnpu.Add(n);
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<NewPhoneUser>(lnpu);
                // Store the new phone number in the new table
                PhoneNumber pn0 = new PhoneNumber();
                pn0.phonenumber = n.PhoneNumber;
                pn0.added = DateTime.Now;
                pn0.bbpaddress = n.Address;
                List<PhoneNumber> lpn000 = new List<PhoneNumber>();
                lpn000.Add(pn0);
                List<ChainObject> co02 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneNumber>(lpn000);
                await CoreController.InsertChainObjectsInternal(co01);
                await CoreController.InsertChainObjectsInternal(co02);
                return true;
            }
            public static void ProcessWebHookInner(string sData)
            {
                try
                {
                    string hash = Encryption.GetMd5String(sData);
                    dynamic oObj = JsonConvert.DeserializeObject(sData);
                    dynamic o = oObj["callbacks"];
                    string salt = "salt";
                    string accountId = account_id.ToString();
                    string apiKey = SecureString.GetDBConfigurationKeyValue("voximplantapikey");
                    for (int i = 0; i < o.Count; i++)
                    {
                        var oCallback = o[i];
                        string sHash = oCallback.hash;
                        string HashData = "salt" + accountId + apiKey + oCallback.callback_id;
                        string sOurMD5 = Encryption.GetMd5String(HashData);

                        bool bSecurityMatches = sOurMD5 == sHash;
                        Log("SecurityMatch::" + bSecurityMatches.ToString());

                        if (oCallback.type == "sms_inbound")
                        {
                            string sRecip = oCallback.sms_inbound.destination_number;
                            string sSender = oCallback.sms_inbound.source_number;
                            string sMsg = oCallback.sms_inbound.sms_body;
                            string sSendDate = oCallback.sms_inbound.receival_date;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log("Unable to process webhook " + ex.Message);
                    string sData1 = ex.Message;
                }
            }

            public static void ProcessWebHook(string body)
            {
                try
                {
                    string accountId = account_id.ToString();
                    string apiKey = SecureString.GetDBConfigurationKeyValue("voximplantapikey");
                    Log("VoxImplant::ProcessWebHook::Recvd::" + body);
                    ProcessWebHookInner(body);
                }
                catch (Exception ex)
                {
                    Log("PROCESS_WEB_HOOK_ERROR::" + ex.Message);
                }
            }
        

            public static async Task<long> SendSMS(SMSMessage msg)
            {
                var voximplant = GetPhoneImplant();

                try
                {
                    SendSmsMessageResponse s1 = await voximplant.SendSmsMessage(msg.From, msg.To, msg.Message);
                    return s1.Result;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("SMS disabled"))
                    {
                        var r1 = await voximplant.ControlSms(msg.From, "enable");
                        SendSmsMessageResponse s11 = await voximplant.SendSmsMessage(msg.From, msg.To, msg.Message);
                        return s11.Result;
                    }
                }
                return -1;
            }


            private static int account_id = 6410295;
            private static string msLastSMSBillingHash = String.Empty;
            private static async Task<bool> SyncSMSCharges()
            {
                try
                {
                    List<PhoneUser> ap = GetAccountPhone();
                    DateTime dtStartDate = DateTime.Now.AddDays(-3);
                    string sFromDate = dtStartDate.ToString("yyyy-MM-dd");
                    string voximplantapikey = SecureString.GetDBConfigurationKeyValue("voximplantapikey");
                    string sURL = "https://api.voximplant.com/platform_api/GetSmsHistory/?account_id=" + account_id.ToString() 
                        + "&destination_number=&row_limit=700&from_date=" + sFromDate
                        + "%2000%3A00%3A00&api_key=" + voximplantapikey;
                    string sData = ExecuteMVCCommand(sURL);
                    string sCurHash = BMSCommon.Encryption.GetSha256HashI(sData);
                    if (sCurHash == msLastSMSBillingHash)
                    {
                        return false;
                    }
                    msLastSMSBillingHash = sCurHash;
                    dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sData);
                    List<PhoneCallHistory> lAddCharges = new List<PhoneCallHistory>();
                    foreach (var j in o.result)
                    {
                        string sDest = j["destination_number"].Value;
                        string sSource = j["source_number"].Value;
                        double nCost = (j["cost"].Value);
                        long lTxId = j["transaction_id"].Value;
                        long lSMSID = j["sms_id"].Value;
                        string sProcessed = j["processed_date"].Value;
                        string sDirection = j["direction"].Value;
                        string sActiveNum = sDirection.ToLower() == "IN" ? sDest : sSource;
                        long nFragments = j["fragments"].Value;
                        PhoneUser ap0 = ap.FirstOrDefault(q => q.PhoneNumber == sActiveNum);
                        if (ap0 != null)
                        {
                            PhoneCallHistory pch = new PhoneCallHistory();
                            pch.id = lTxId;
                            pch.userid = ap0.id;
                            pch.duration = nFragments;
                            pch.fromnumber = sSource;
                            pch.tonumber = sDest;
                            pch.charge = nCost * 1.30;
                            double nAmtBBP = await PricingService.ConvertUSDToBiblePay((double)(nCost * 1.30));
                            pch.amountbbp = nAmtBBP;
                            pch.added = sProcessed.ToDateTime();
                            pch.direction = sDirection;
                            pch.calltype = "SMS";
                            pch.smsid = lSMSID.ToStr();
                            if (nCost > 0)
                            {
                                lAddCharges.Add(pch);
                            }
                        }

                    }
                    if (lAddCharges.Count > 0)
                    {
                        List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneCallHistory>(lAddCharges);
                        await CoreController.InsertChainObjectsInternal(co01);
                    }
                }
                catch (Exception ex)
                {
                    string sErr = ex.Message;
                    return false;
                }
                return true;
            }
            public class AccountPhone
            {
                [PrimaryKey]
                public int id { get; set; }
                public int Account { get; set; }
                public string PhoneNumber { get; set; }
            }
            private static List<PhoneUser> GetAccountPhone()
            {
                List<PhoneUser> lAP = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                lAP = lAP.Where(a => a.PhoneNumber != null && a.id > 0).ToList();
                return lAP;
            }

            private static long mnLastPhoneBillingID = 0;
            public static async Task<bool> SyncBBPPhoneCharges()
            {
                // PerformPhoneBilling
                try
                {
                    bool fPrimary = BBPAPI.QuantBilling.IsPrimary();

                    if (!fPrimary)
                    {
                        BMSCommon.Common.Log("Not the primary node");
                        return false;
                    }


                    bool fLatch = NVRAM.IsGreaterThanElapsedTime("SyncBBPPhoneCharges", 60 * 60 * 1);
                    if (!fLatch)
                        return false;

                    BMSCommon.Common.Log("Time to sync the phone charges");

                    // Then we sync the BBP phone charges

                    DateTime StartDate = DateTime.Now.AddDays(-7);
                    DateTime EndDate = DateTime.Now;
                    var voximplant = GetPhoneImplant();

                    //SMS BILLING
                    await SyncSMSCharges();

                    // PHONE BILLING

                    var m1 = await voximplant.GetCallHistory(StartDate, EndDate, null, null, null, null, null, null, null, null, true,
                        true, null, null, null, true, true, null, 10, 0, null, null);
                    long nCurID = m1.Result[0].Calls[0].CallId;
                    List<PhoneCallHistory> historicalCharges = QuorumUtils.GetDatabaseObjects<PhoneCallHistory>("PhoneCallHistory");
                    historicalCharges = historicalCharges.OrderByDescending(a => a.added).ToList();

                    if (mnLastPhoneBillingID == 0)
                    {
                        //pull the highest phone id
                        mnLastPhoneBillingID = historicalCharges[0].id;
                    }
                    if (nCurID == mnLastPhoneBillingID)
                    {
                        // same...
                        return false;
                    }


                    List<PhoneCallHistory> lNewCharges = new List<PhoneCallHistory>();


                    var me = await voximplant.GetCallHistory(StartDate, EndDate, null, null, null, null, null, null, null, null, true,
                        true, true, null, null, true, true, null, 2500, 0, null, null);
                    for (int i = 0; i < me.Result.Length; i++)
                    {
                        var h = me.Result[i];

                        if (h != null )
                        {
                            long nID = h.Calls[0].CallId;
                            if (h.Calls.Length > 1 && h.AccountId > 0)
                            {
                                long nSecs = h.Duration ?? 0;  //ms

                                string sTN = h.Calls[0].Incoming ? h.Calls[0].LocalNumber : h.Calls[0].RemoteNumber;
                                string sFN = h.Calls[1].Incoming ? h.Calls[1].RemoteNumber : h.Calls[1].LocalNumber;
                                DateTime dtAdded = h.Calls[0].StartTime;
                                double nCharge = 0;
                                for (int j = 0; j < h.Calls.Length; j++)
                                {
                                    nCharge += (double)h.Calls[j].Cost;
                                }

                                PhoneCallHistory pchn = new PhoneCallHistory();
                            
                                pchn.id = nID;
                                pchn.userid = h.UserId;
                                pchn.duration = nSecs;
                                pchn.added = dtAdded;
                                pchn.fromnumber = sFN;
                                pchn.tonumber = sTN;
                                pchn.charge = nCharge * 1.30;
                                double nAmtBBP = await PricingService.ConvertUSDToBiblePay((double)(nCharge * 1.30));
                                pchn.amountbbp = nAmtBBP;
                                
                                if (nCharge > 0)
                                {
                                    // verify non existence
                                    List<PhoneCallHistory> lhpc = historicalCharges.Where(a => a.id == nID).ToList();
                                    if (lhpc.Count == 0)
                                    {
                                        lNewCharges.Add(pchn);
                                    }
                                }
                            }
                            if (nCurID == mnLastPhoneBillingID)
                            {
                                // Weve worked our way back to the last billingID...
                                break;
                            }

                        }
                    }
                    if (lNewCharges.Count > 0)
                    {
                        List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneCallHistory>(lNewCharges);
                        await CoreController.InsertChainObjectsInternal(co01);
                    }

                    mnLastPhoneBillingID = nCurID;

                    await PerformPhoneCharges();

                    return true;
                }
                catch (Exception ex)
                {
                    Log("SyncBBPPhoneCharges::" + ex.Message);
                    return false;
                }
            }
            public static async Task<bool> PerformPhoneCharges()
            {
                // for each unbilled phone charge transaction in group, bill it and insert the TXID in the table.

                try
                {
                    string sBatchID = Guid.NewGuid().ToString();
                    await UpdatePhoneCallHistoryWithBatchGuid(sBatchID);
                    List<PhoneCallHistory> lCharges = GetPhoneCallHistoryForBatch(sBatchID);

                    for (int i = 0; i < lCharges.Count; i++)
                    {
                        PhoneCallHistory pcCharge = lCharges[i];
                        string sPayload = "<phone></phone>";
                        DACResult r0 = new DACResult();
                        if (pcCharge.batchowed > 25)
                        {
                            r0 = await WebRPC.SendBBPOutsideChain(false, "phone", Global.FoundationPublicKey, 
                                pcCharge.bbppk, pcCharge.batchowed, sPayload);
                            if (!String.IsNullOrEmpty(r0.TXID))
                            {
                                await UpdatePhoneCallHistoryNoError(pcCharge.userid, r0.TXID, pcCharge.batchowed, sBatchID);
                            }
                            else
                            {
                                await UpdatePhoneCallHistoryError(r0.Error, pcCharge.userid, pcCharge.batch);
                            }
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Log("PerformPhoneCharges::" + ex.Message);
                    return false;
                }
            }


            public static List<PhoneCallHistory> GetCallHistoryReport(long nUserID)
            {
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where( a => a.userid == nUserID ).ToList();
                lpch = lpch.OrderByDescending(a => a.added).ToList();
                return lpch;
            }
            public static async Task<bool> SetPhoneRulesCreated(long nNewUserID)
            {
                List<PhoneUser> lpu = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                lpu = lpu.Where(a => a.id == nNewUserID).ToList();
                if (lpu == null || lpu.Count < 1)
                {
                    return false;
                }
                lpu[0].rulescreated = 1;
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneUser>(lpu);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }

            // PHONE

            internal static async Task<bool> UpdatePhoneCallHistoryWithBatchGuid(string sBatchID)
            {
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where(a => String.IsNullOrEmpty(a.txid)).ToList();
                for (int i = 0; i < lpch.Count; i++)
                {
                    lpch[i].batch = sBatchID;
                }
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneCallHistory>(lpch);
                await CoreController.InsertChainObjectsInternal(co01);

                return true;
            }

            internal static List<PhoneCallHistory> GetPhoneCallHistoryForBatch(string sBatchID)
            {
                System.Threading.Thread.Sleep(10000);
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where(a => a.batch==sBatchID && (String.IsNullOrEmpty(a.txid)) && a.userid != 0).ToList();
                var lpchgbid = lpch.GroupBy(s => s.userid.ToStr());
                List<PhoneUser> lpu = QuorumUtils.GetBBPDatabaseObjects<PhoneUser>();
                List<PhoneCallHistory> rpch = new List<PhoneCallHistory>();
                foreach(var group in lpchgbid)
                {
                    
                    PhoneCallHistory pchnew = new PhoneCallHistory();
                    PhoneUser mypu = lpu.Where( a => a.id == group.FirstOrDefault().userid).FirstOrDefault();
                    pchnew.batchowed  = group.Sum(a => a.amountbbp);
                    pchnew.userid = group.FirstOrDefault().userid;
                    pchnew.batch = group.FirstOrDefault().batch;
                    pchnew.bbpaddress = mypu.bbpaddress;
                    pchnew.bbppk = mypu.bbppk;
                    rpch.Add(pchnew);
                }

                return rpch;
            }

            internal static async Task<bool> UpdatePhoneCallHistoryNoError(long nUserID, string TXID, double nAMTBBP, string sBatchID)
            {
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where(a => a.batch == sBatchID && a.userid == nUserID).ToList();
                for (int i = 0; i < lpch.Count; i++)
                {
                    lpch[i].updated = DateTime.Now;
                    lpch[i].billed = DateTime.Now;
                    lpch[i].amountbilled = nAMTBBP;
                    lpch[i].txid = TXID;
                }
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneCallHistory>(lpch);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }

            internal async static Task<bool> UpdatePhoneCallHistoryError(string sError, long nUserID, string sBatchID)
            {
                List<PhoneCallHistory> lpch = QuorumUtils.GetBBPDatabaseObjects<PhoneCallHistory>();
                lpch = lpch.Where(a => a.batch == sBatchID && a.userid == nUserID).ToList();
                for (int i = 0; i < lpch.Count; i++)
                {
                    lpch[i].updated = DateTime.Now;
                    lpch[i].error = sError;
                }
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<PhoneCallHistory>(lpch);
                await CoreController.InsertChainObjectsInternal(co01);
                return true;
            }
            // Phone Service procs
            internal static async Task<long> AddScenario(string sBBPAddress, string sScenarioScript)
            {
                var voximplant = GetPhoneImplant();
                var nOK3 = await voximplant.AddScenario(sBBPAddress.Substring(0, 16), sScenarioScript, null, null, false);
                return nOK3.Result;
            }

            public static async Task<List<ComboBoxItem>> GetRegionsInternal(string sState)
            {
				List<ComboBoxItem> ddRegions = new List<ComboBoxItem>();
				try
				{
                    var voximplant = GetPhoneImplant();
                    if (String.IsNullOrEmpty(sState))
                    {
                        sState = "PA";
                    }
                    var regions = await voximplant.GetPhoneNumberRegions("US", "GEOGRAPHIC", sState.Trim());
                    for (int i = 0; i < regions.Result.Length; i++)
                    {
                        ComboBoxItem di = new ComboBoxItem();
                        di.Value = regions.Result[i].PhoneRegionId.ToString();
                        di.Text = regions.Result[i].PhoneRegionName + " " + regions.Result[i].PhoneRegionCode.ToString();
						ddRegions.Add(di);
                    }
                    return ddRegions;
                }
                catch(Exception ex)
                {
                    Log(ex.Message);
                    Log(ex.StackTrace);
                    return ddRegions;
                }
            }
            public static async Task<List<DropDownItem>> GetPhoneNumbersForRegion(long nRegionID, string sState)
            {
                List<DropDownItem> ddPhoneNums = new List<DropDownItem>();
                try
                {
                    var voximplant = GetPhoneImplant();
                    var phonenums = await voximplant.GetNewPhoneNumbers("US", "GEOGRAPHIC", nRegionID, sState);
                    ddPhoneNums = new List<DropDownItem>();
                    for (int i = 0; i < phonenums.Result.Length; i++)
                    {
						DropDownItem di = new DropDownItem();
                        di.key0 = phonenums.Result[i].PhoneNumber.ToString();
                        di.text0 = phonenums.Result[i].PhoneNumber;
                        ddPhoneNums.Add(di);
                    }
                }
                catch (Exception ex)
                { 
                    Log("GPNFR::" + ex.Message);
                }

                return ddPhoneNums;
            }

            public static async Task<string> BuyAndGetNewPhoneNumberInternal(PhoneRegionCountryAddress pcr)
            {
                // Buy, attach and return new phone number
                // VIP NOTE: AS OF 4-17-2023, the api no longer allows us to search by phone #.  The following line PURCHASES any phone # in the nRegion:
                var voximplant = GetPhoneImplant();
                var nOK = await voximplant.AttachPhoneNumber("US", "GEOGRAPHIC", pcr.RegionID, 1,null, pcr.CountryState, null);

                if (nOK.Result == 0)
                    return "FAIL_0";

                // So right here we need to memorize our Available phone #s, and pick one that is free.
                string sPhoneNumber = String.Empty;
                try
                {

                    List<ComboBoxItem> ddRegions = await DB.PhoneProcs.GetRegionsInternal(pcr.CountryState.Trim());
                    string sRegCaptionInfo = String.Empty;
                    for (int i = 0; i < ddRegions.Count; i++)
                    {
                        if (GetDouble(ddRegions[i].Value.ToStr()) == GetDouble(pcr.RegionID.ToString().ToStr()))
                        {
                            sRegCaptionInfo = ddRegions[i].Text;
                            break;
                        }
                    }
                    var oAvailPhoneNums = await voximplant.GetPhoneNumbers(null, null, null, false);
                    for (int i = 0; i < oAvailPhoneNums.Count; i++)
                    {
						if (oAvailPhoneNums.Result[i].CanBeUsed &&
                          sRegCaptionInfo.Contains(  oAvailPhoneNums.Result[i].PhoneNumber.Substring(1, 3) ))
                        {
                            sPhoneNumber = oAvailPhoneNums.Result[i].PhoneNumber;
                            break;
                        }
                    }

                    if (sPhoneNumber == String.Empty)
                    {
                        return "FAIL_1";
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return "FAIL_2";
                }

                var nOK2 = await voximplant.BindPhoneNumberToApplication(null, sPhoneNumber, Global.mnPhoneApplicationID, null, null, null, true);
                if (nOK2.Result == 0)
                    return "FAIL_3";

                return sPhoneNumber;

                /* Since we are doing our own webhook routing, we dont need to create the scanario any more                */
            }
            
            public static async Task<long> AddNewPhoneUserInternal(BBPAddressKey k)
            {
                var voximplant = GetPhoneImplant();
                Voximplant.API.Response.AddUserResponse uNewUser = new Voximplant.API.Response.AddUserResponse();
                uNewUser = await voximplant.AddUser(k.Address, k.Address, k.PrivateKey,
                    Global.mnPhoneApplicationID, null, null, true, null);
                return uNewUser.UserId;
            }
        }
    }
}
