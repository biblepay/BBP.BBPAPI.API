using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using static BMSCommon.Common;

namespace BBPAPI
{
	public static class GovernanceProposal
    {

        public static void SetScratch(string id,string sScratchValue)
        {
            NVRAM.SetNVRamValue("scratch_" + id, sScratchValue); 
        }
        public static string GetScratch(string id)
        {
            return NVRAM.GetNVRamValue("scratch_" + id);
        }
        private static string GJE(string sKey, string sValue, bool bIncludeDelimiter, bool bQuoteValue)
        {
            // This is a helper for the Governance gobject create method
            string sQ = "\"";
            string sOut = sQ + sKey + sQ + ":";
            if (bQuoteValue)
            {
                sOut += sQ + sValue + sQ;
            }
            else
            {
                sOut += sValue;
            }
            if (bIncludeDelimiter) sOut += ",";
            return sOut;
        }

        public static string gobject_serialize_internal(Proposal p)
        {

            // gobject prepare 0 1 EPOCH_TIME HEX
            string sType = "1"; //Proposal
            string sQ = "\"";
            string sJson = "[[" + sQ + "proposal" + sQ + ",{";
            sJson += GJE("start_epoch", p.nStartTime.ToString(), true, false);
            sJson += GJE("end_epoch", p.nEndTime.ToString(), true, false);
            sJson += GJE("name", p.Name, true, true);
            sJson += GJE("payment_address", p.BBPAddress, true, true);
            sJson += GJE("payment_amount", p.Amount.ToString(), true, false);
            sJson += GJE("type", sType, true, false);
            sJson += GJE("expensetype", p.ExpenseType, true, true);
            sJson += GJE("url", p.URL, false, true);
            sJson += "}]]";
			sJson = "{";
			sJson += GJE("start_epoch", p.nStartTime.ToString(), true, false);
			sJson += GJE("end_epoch", p.nEndTime.ToString(), true, false);
			sJson += GJE("name", p.Name, true, true);
			sJson += GJE("payment_address", p.BBPAddress, true, true);
			sJson += GJE("payment_amount", p.Amount.ToString(), true, false);
			sJson += GJE("type", sType, true, false);
			sJson += GJE("expensetype", p.ExpenseType, true, true);
			sJson += GJE("url", p.URL, false, true);
			sJson += "}";
			// make into hex
			string Hex = HexadecimalEncoding.StringToHex(sJson);
            return Hex;
        }
        public async static Task<bool> gobject_serialize(Proposal p)
        {
            try
            {
                p.nStartTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                p.nEndTime = p.nStartTime + (60 * 60 * 24 * 7);
                p.Hex = gobject_serialize_internal(p);
               
                bool f23 = await QuorumUtils.InsertObject<BMSCommon.Model.Proposal>(p);
                gobject_prepare(p);
                return true;
            }
            catch (Exception ex)
            {
                Log("Issue with Proposal Submit:: " + ex.Message);
                return false;
            }
        }

        public async static Task<bool> gobject_prepare(Proposal p)
        {
            try
            {
                p.TestNet = p.Chain == "test";
                // gobject prepare
                string sArgs = "0 1 " + p.nStartTime.ToString() + " " + p.Hex;
                string sCmd1 = "gobject prepare " + sArgs;
                object[] oParams = new object[5];
                oParams[0] = "prepare";
                oParams[1] = "0";
                oParams[2] = "1";
                oParams[3] = p.nStartTime.ToString();
                oParams[4] = p.Hex;
                NBitcoin.RPC.RPCClient n = WebRPC.GetRPCClient(p.TestNet);
                dynamic oOut = n.SendCommand("gobject", oParams);
                string sPrepareTXID = oOut.Result.ToString();
                p.PrepareTXID = sPrepareTXID;
                p.Updated = DateTime.Now;
                string sSerial = Newtonsoft.Json.JsonConvert.SerializeObject(p, Newtonsoft.Json.Formatting.Indented);
                return await QuorumUtils.InsertObject<BMSCommon.Model.Proposal>(p);
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> gobject_submit(Proposal p)
        {
            try
            {
                if (p.SubmitTXID != null && p.SubmitTXID.Length > 1)
                {
                    return false;
                }

                // Submit the gobject to the network - gobject submit parenthash revision time datahex collateraltxid
                string sArgs = "0 1 " + p.nStartTime.ToString() + " " + p.Hex + " " + p.PrepareTXID;
                string sCmd1 = "gobject submit " + sArgs;
                object[] oParams = new object[6];
                oParams[0] = "submit";
                oParams[1] = "0";
                oParams[2] = "1";
                oParams[3] = p.nStartTime.ToString();
                oParams[4] = p.Hex;
                oParams[5] = p.PrepareTXID;
                NBitcoin.RPC.RPCClient n = WebRPC.GetRPCClient(p.TestNet);
                dynamic oOut = n.SendCommand("gobject", oParams);
                string sSubmitTXID = oOut.Result.ToString();
                if (sSubmitTXID.Length > 20)
                {
                    // Update the record allowing us to know this has been submitted
                    p.Submitted = DateTime.Now;
                    p.SubmitTXID = sSubmitTXID;
                    return await QuorumUtils.InsertObject<Proposal>(p);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> SubmitProposals(User u, bool fTestNet)
        {
            string sChain = fTestNet ? "test" : "main";
            List<Proposal> dt = QuorumUtils.GetDatabaseObjects<Proposal>("proposal");
            dt = dt.Where(s => s.Chain == sChain && s.SubmitTXID == null).ToList();
            for (int y = 0; y < dt.Count; y++)
            {
                Proposal p = dt[y];
                p.User = u;
                p.TestNet = fTestNet;
                bool fSubmitted = await gobject_submit(p);
            }
            return true;
        }
    }
}
