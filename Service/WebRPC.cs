using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using NBitcoin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static BMSCommon.Common;
using static BMSCommon.Model.BitcoinSyncModel;

namespace BBPAPI
{

    internal static class SecureString
    {
        private static string X509 = "-----BEGIN CERTIFICATE-----\r\nMIIHnTCCBoWgAwIBAgIQB3a13cqDpLnKWY9ddx+eRjANBgkqhkiG9w0BAQsFADB1\r\nMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3\r\nd3cuZGlnaWNlcnQuY29tMTQwMgYDVQQDEytEaWdpQ2VydCBTSEEyIEV4dGVuZGVk\r\nIFZhbGlkYXRpb24gU2VydmVyIENBMB4XDTE2MDMwOTAwMDAwMFoXDTE4MDMxNDEy\r\nMDAwMFowggEhMR0wGwYDVQQPDBRQcml2YXRlIE9yZ2FuaXphdGlvbjETMBEGCysG\r\nAQQBgjc8AgEDEwJVUzEZMBcGCysGAQQBgjc8AgECEwhEZWxhd2FyZTEQMA4GA1UE\r\nBRMHNDMzNzQ0NjESMBAGA1UECRMJU3VpdGUgOTAwMRcwFQYDVQQJEw4xMzU1IE1h\r\ncmtldCBTdDEOMAwGA1UEERMFOTQxMDMxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpD\r\nYWxpZm9ybmlhMRYwFAYDVQQHEw1TYW4gRnJhbmNpc2NvMRYwFAYDVQQKEw1Ud2l0\r\ndGVyLCBJbmMuMRkwFwYDVQQLExBUd2l0dGVyIFNlY3VyaXR5MRQwEgYDVQQDEwt0\r\nd2l0dGVyLmNvbTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMIImPpn\r\nAAVVtgthDhrXtYrBzAO+PBf7lPfZ+kyfRmCcaq19OuU0WhKwsguq7JbhWIEvrWCr\r\nR5Np44R1U8H5D7lGq57qqxiYjGhUCFFlQxphlydcXg8V6c0Wq91RW3Yv/NMRmZ3S\r\npj2HAnXmJJbiBD4UnPp+uHFCNwC1sIriM5WL2j/7Y003YtUcAuowftwNU9XUC7ij\r\nEBNtH4mUC2qURGcpgq3m1bBS/JVXBtbRImaE05IqAseUVt9VP8IT8nwWeDOhU/d3\r\nl1y3lgXVRPS/74MiXXrmj+Ss3zSetg8KU/Aa23E3aZL2FKkcdWVyRSQJOyxq17lp\r\npdzfbZxr/MaiWzECAwEAAaOCA3kwggN1MB8GA1UdIwQYMBaAFD3TUKXWoK3u80pg\r\nCmXTIdT4+NYPMB0GA1UdDgQWBBSfYnuyiA7uG3ngaSTluj9HpgsC8DAnBgNVHREE\r\nIDAeggt0d2l0dGVyLmNvbYIPd3d3LnR3aXR0ZXIuY29tMA4GA1UdDwEB/wQEAwIF\r\noDAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwdQYDVR0fBG4wbDA0oDKg\r\nMIYuaHR0cDovL2NybDMuZGlnaWNlcnQuY29tL3NoYTItZXYtc2VydmVyLWcxLmNy\r\nbDA0oDKgMIYuaHR0cDovL2NybDQuZGlnaWNlcnQuY29tL3NoYTItZXYtc2VydmVy\r\nLWcxLmNybDBLBgNVHSAERDBCMDcGCWCGSAGG/WwCATAqMCgGCCsGAQUFBwIBFhxo\r\ndHRwczovL3d3dy5kaWdpY2VydC5jb20vQ1BTMAcGBWeBDAEBMIGIBggrBgEFBQcB\r\nAQR8MHowJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBSBggr\r\nBgEFBQcwAoZGaHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0U0hB\r\nMkV4dGVuZGVkVmFsaWRhdGlvblNlcnZlckNBLmNydDAMBgNVHRMBAf8EAjAAMIIB\r\nfAYKKwYBBAHWeQIEAgSCAWwEggFoAWYAdgCkuQmQtBhYFIe7E6LMZ3AKPDWYBPkb\r\n37jjd80OyA3cEAAAAVNdgFLZAAAEAwBHMEUCICZCA9wZjkyHJRy3UTCYnwI21m/U\r\nXKRXWc7US9arx68qAiEAtK1UZMDl2wRt/o1OxInzFdQCQ+2QTIvLbHe5slXu6boA\r\ndQBo9pj4H2SCvjqM7rkoHUz8cVFdZ5PURNEKZ6y7T0/7xAAAAVNdgFKcAAAEAwBG\r\nMEQCIGF6AFQ8TKA8AqktUZ/45JJuKYHCIFIkqcPWIIDLWIZmAiA5PVUV5BBCM2AK\r\nce/CeXCyim1y140g/4RxghYW6sNCNwB1AFYUBpov18Ls0/XhvUSyPsdGdrm8mRFc\r\nwO+UmFXWidDdAAABU12AU6YAAAQDAEYwRAIgXUM1kBRW2bTGAqVvy/aDoYTrdKvM\r\nI6x5p0FF2S+jGmkCIFmAWDXHV/YBi4thS8HGZc3iVCh5wwaCGM3kztEaUYmQMA0G\r\nCSqGSIb3DQEBCwUAA4IBAQC7+PUbZaNQAx8YEMg1Uy+cih5Iar3l5ljJ0eih/KsD\r\nQo9Y8woYppEuwVC3cN0V2q0I8RXSRE105BgrZbYF2fn32CRs21/sbH0/v6VMonNo\r\nOEJBzeL20fjYidN1Sr39q02e7kjJNCPVg8yTlRREpSXlsfwXWFOnACSBwpRzmD43\r\nbRKVH6zjIPiy2wmxXP6ibb3p0ITHnosxLsf3pWXjL/YeWqQq6mUDMRKmeCRR3k1E\r\n03kXQyxV4AD4hccLqP4K6m17dOkpWbKWNN+/wxWy/ApMuP0hNPgoZSLQBaMidNzh\r\nb92204f4\r\nb031\r\n472d\r\n9041\r\n8be8a6231a7b\r\n"
            + "Y63izHj1KcOdLNg8VVCCEPoEX8IlbLMIY/YTfN5XAFjs\r\n-----END CERTIFICATE-----";

        internal static string GetCSC()
        {
            string[] vKey = X509.Split("\r\n");
            string s10 = vKey[41];
            string s11 = vKey[42];
            string s12 = vKey[43];
            string s14 = vKey[44];
            string s15 = vKey[45];
            string sTest0 = s10 + s11 + s12 + s14 + s15;
            return sTest0;
        }
    
        internal static string GetDBConfigurationKeyValue(string sKeyName)
        {
            List<Config> sk = QuorumUtils.GetBBPDatabaseObjects<Config>();
            sk = sk.Where(a => a.systemkey == sKeyName).ToList();
            if (sk.Count == 0)
            {
                return String.Empty;
            }
            string data = Encryption.DecryptAES256(sk[0].value, GetCSC());
            return data;
        }
    }

    public static class Sanctuary
    {
        public static async Task<bool> SDCK(string sKeyName, string sKeyValue, string sKeyPass)
        {
            Config g = new Config();
            g.systemkey = sKeyName;
            g.value = BMSCommon.Encryption.EncryptAES256(sKeyValue, SecureString.GetCSC());
            g.added = DateTime.Now;
            await QuorumUtils.InsertObject<Config>(g);
            return true;
        }

        public static SupplyType GetSupply(bool fTestNet)
        {
            return WebRPC.GetSupply(fTestNet);
        }

        public static string GetFDPubKey(bool fTestNet)
        {
            string sPubKey = fTestNet ? "yTrEKf8XQ7y7tychC2gWuGw1hsLqBybnEN" : "BB2BwSbDCqCqNsfc7FgWFJn4sRgnUt4tsM";
            return sPubKey;
        }

        public async static Task<DACResult> SendMoney(SendMoneyRequest re)
        {
            string sPubKey = ERCUtilities.GetPubKeyFromPrivKey(re.TestNet,re.PrivateKey);
            string sData = WebRPC.GetAddressUTXOs(re.TestNet, sPubKey);
            string sErr = String.Empty;
            string sHex = String.Empty;
            DACResult r = new DACResult();
            string sTXID = String.Empty;

            NBitcoin.Crypto.BBPTransaction.PrepareFundingTransaction(re.TestNet, re.nAmount, re.sToAddress, re.PrivateKey, re.sOptPayload,
                sData, out sErr, out sHex, out sTXID);
            
            if (sErr != String.Empty)
            {
                r.Error = sErr;
                return r;
            }
            string sPostTXID = String.Empty;

            r = await WebRPC.SendRawTx(re.TestNet, sHex, sTXID);
            
            return r;
        }
        public static bool ValidateBiblePayAddress(bool fTestNet, string sAddress)
        {
            return WebRPC.ValidateBiblepayAddress(fTestNet, sAddress);
        }

        public static string SignMessage(bool fTestNet, string sPrivKey, string sMessage)
        {
            try
            {
                if (sPrivKey == null || sMessage == String.Empty || sMessage == null)
                    return string.Empty;

                BitcoinSecret bsSec;
                if (!fTestNet)
                {
                    bsSec = Network.Main.CreateBitcoinSecret(sPrivKey);
                }
                else
                {
                    bsSec = Network.TestNet.CreateBitcoinSecret(sPrivKey);
                }
                string sSig = bsSec.PrivateKey.SignMessage(sMessage);
                string sPK = bsSec.GetAddress().ToString();
                var fSuc = VerifySignature(fTestNet, sPK, sMessage, sSig);
                return sSig;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static bool VerifySignature(bool fTestNet, string BBPAddress, string sMessage, string sSig)
        {
            if (BBPAddress == null || sSig == String.Empty || BBPAddress == "" || BBPAddress == null || sSig == null || BBPAddress.Length < 20)
                return false;
            try
            {
                BitcoinPubKeyAddress bpk;
                if (fTestNet)
                {
                    bpk = new BitcoinPubKeyAddress(BBPAddress, Network.TestNet);
                }
                else
                {
                    bpk = new BitcoinPubKeyAddress(BBPAddress, Network.Main);
                }

                bool b1 = bpk.VerifyMessage(sMessage, sSig, true);
                return b1;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal static class WebRPC
    {

        internal static double GetCoreWalletBalance(bool fTestNet)
        {
            try
            {
                NBitcoin.RPC.RPCClient n1 = GetRPCClient(fTestNet);
                NBitcoin.Money m1 = n1.GetBalance();
                double m2 = GetDouble(m1.ToString());
                return m2;
            }
            catch (Exception ex)
            {
                Log("GetBalanceRPC::" + ex.Message);
                return 0;
            }
        }

        internal static bool ValidateBiblepayAddress(bool fTestNet, string sAddress)
        {
            try
            {
                object[] oParams = new object[1];
                oParams[0] = sAddress;
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("validateaddress", oParams);
                string sResult = oOut.Result["isvalid"].ToString();
                if (sResult.ToLower().Contains("true")) return true;
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetFoundationPublicKey(bool fTestNet)
        {
            string s = fTestNet ? "yTrEKf8XQ7y7tychC2gWuGw1hsLqBybnEN" : "BB2BwSbDCqCqNsfc7FgWFJn4sRgnUt4tsM";
            return s;
		}

        public static double GetBalanceFromUtxos(string myUTXOS)
        {
			List<SimpleUTXO> sUTXO = new List<SimpleUTXO>();
			List<NBitcoin.Crypto.UTXO> l = NBitcoin.Crypto.BBPTransaction.GetBBPUTXOs(myUTXOS);
            double nTotal = 0;
			for (int i = 0; i < l.Count; i++)
			{
				SimpleUTXO s = new SimpleUTXO();
                s.nAmount = (double)l[i].Amount.ToDecimal(MoneyUnit.BTC);
				s.TXID = l[i].TXID.ToString();
				sUTXO.Add(s);
                nTotal += s.nAmount;
			}
            return nTotal;
		}

        private static List<SimpleUTXO> GetBBPUTXOs2(bool fTestNet, string sAddress)
        {
            string sUTXOData = GetAddressUTXOs(fTestNet, sAddress);
            // Used by Portfolio Builder.
            List<SimpleUTXO> sUTXO = new List<SimpleUTXO>();
            List<NBitcoin.Crypto.UTXO> l = NBitcoin.Crypto.BBPTransaction.GetBBPUTXOs(sUTXOData);
            for (int i = 0; i < l.Count; i++)
            {
                SimpleUTXO s = new SimpleUTXO();
                s.nAmount = l[i].Amount.Satoshi / 100000000;
                s.Address = sAddress;
                s.TXID = l[i].TXID.ToString();
                s.Ticker = "BBP";
                sUTXO.Add(s);
            }
            return sUTXO;
        }


        internal static string GetRawTransaction(string sTxid, bool fTestNet)
        {
            try
            {
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                object[] oParams = new object[2];
                oParams[0] = sTxid;
                oParams[1] = 1;
                dynamic oOut = n.SendCommand("getrawtransaction", oParams);
                // Loop Through the Vouts and get the recip ids and the amounts
                string sOut = "";
                double locktime = oOut.Result["locktime"] == null ? 0 : GetDouble(oOut.Result["locktime"].ToString());
                double height1 = oOut.Result["height"] == null ? 0 : GetDouble(oOut.Result["height"].ToString());
                double height = 0;
                height = height1 > 0 ? height1 : locktime;
                for (int y = 0; y < oOut.Result["vout"].Count; y++)
                {
                    string sPtr = String.Empty;
                    try
                    {
                        sPtr = (oOut.Result["vout"][y] ?? "").ToString();
                    }
                    catch (Exception ey)
                    {
                        Log("Strange error in GetRawTransaction=" + ey.Message);
                    }

                    if (sPtr != String.Empty)
                    {
                        string sAmount = oOut.Result["vout"][y]["value"].ToString();
                        string sAddress = String.Empty;
                        if (oOut.Result["vout"][y]["scriptPubKey"]["addresses"] != null)
                        {
                            sAddress = oOut.Result["vout"][y]["scriptPubKey"]["addresses"][0].ToString();
                        }
                        else
                        {
                            sAddress = "?";
                        }
                        //Happens when pool pays itself
                        sOut += sAmount + "," + sAddress + "," + height + "|";
                    }
                    else
                    {
                        break;
                    }
                }
                return sOut;
            }
            catch (Exception ex)
            {
                Log("GetRawTransaction1: for " + sTxid + " " + ex.Message);
                return "";
            }
        }

        public class BitcoinTransactionOverview
        {
            public string Payload;
            public string Recipients;
            public string Amounts;
            public double TotalAmount = 0;
            public BitcoinTransactionOverview()
            {
                Payload = String.Empty;
                Recipients = String.Empty;
                Amounts = String.Empty; 
                TotalAmount = 0;    
            }
        }
        internal static BitcoinTransactionOverview GetRawTransactionXML(string sTxid, bool fTestNet)
        {
            BitcoinTransactionOverview bto = new BitcoinTransactionOverview();
            try
            {
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                object[] oParams = new object[2];
                oParams[0] = sTxid;
                oParams[1] = 1;
                dynamic oOut = n.SendCommand("getrawtransaction", oParams);
                // Loop Through the Vouts and get the recip ids and the amounts
                string sOut = String.Empty;
                double locktime = oOut.Result["locktime"] == null ? 0 : GetDouble(oOut.Result["locktime"].ToString());
                double height1 = oOut.Result["height"] == null ? 0 : GetDouble(oOut.Result["height"].ToString());
                double height = 0;
                height = height1 > 0 ? height1 : locktime;
                for (int y = 0; y < oOut.Result["vout"].Count; y++)
                {
                    string sPtr = String.Empty;
                    try
                    {
                        sPtr = (oOut.Result["vout"][y] ?? "").ToString();
                    }
                    catch (Exception)
                    {
                    }

                    if (sPtr != String.Empty)
                    {
                        string sAmount = oOut.Result["vout"][y]["value"].ToString();
                        string sData = oOut.Result["vout"][y]["txoutmessage"];
                        string sAddress = "";
                        if (oOut.Result["vout"][y]["scriptPubKey"]["addresses"] != null)
                        {
                            sAddress = oOut.Result["vout"][y]["scriptPubKey"]["addresses"][0].ToString();
                        }
                        bto.Payload += sData;
                        bto.Amounts += sAmount + ",";
                        bto.Recipients += sAddress + ",";
                        bto.TotalAmount += Convert.ToDouble(sAmount);
                    }
                    else
                    {
                        break;
                    }
                }
                return bto;
            }
            catch (Exception ex)
            {
                Log("GetRawTransaction2: for " + sTxid + " " + ex.Message);
                return new BitcoinTransactionOverview();
            }
        }

        internal static double GetAmtFromRawTx(string sRaw, string sAddress, out int nHeight)
        {
            string[] vData = sRaw.Split(new string[] { "|" }, StringSplitOptions.None);
            for (int i = 0; i < vData.Length; i++)
            {
                string d = vData[i];
                if (d.Length > 1)
                {
                    string[] vRow = d.Split(new string[] { "," }, StringSplitOptions.None);
                    if (vRow.Length > 1)
                    {
                        string sAddr = vRow[1];
                        string sAmt = vRow[0];
                        string sHeight = vRow[2];
                        nHeight = (int)GetDouble(sHeight);

                        if (sAddr == sAddress && nHeight > 0)
                        {
                            return Convert.ToDouble(sAmt);
                        }
                    }
                }
            }
            nHeight = 0;
            return 0;
        }

        internal static bool SubmitBlock(bool fTestNet, string hex)
        {
            try
            {
                object[] oParams = new object[1];
                oParams[0] = hex;
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("submitblock", oParams);
                string result = oOut.Result.Value;
                // To do return binary response code here; check response for fail and success
                if (result == null)
                    return true;
                if (result == "high-hash")
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Log("SB " + ex.Message);
            }
            return false;
        }

        internal static void GetSubsidy(bool fTestNet, int nHeight, ref string sRecipient, ref double nSubsidy)
        {
            try
            {
                object[] oParams = new object[2];
                oParams[0] = "subsidy";
                oParams[1] = nHeight.ToString();
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("exec", oParams);
                nSubsidy = GetDouble(oOut.Result["subsidy"]);
                sRecipient = oOut.Result["recipient"];
                return;
            }
            catch (Exception ex)
            {
                Log("GS " + ex.Message);
            }
            sRecipient = String.Empty;
            nSubsidy = 0;
        }

        internal static SupplyType GetSupply(bool fTestNet)
        {
            SupplyType s = new SupplyType();
            try
            {
                object[] oParams = new object[0];
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("gettxoutsetinfo", oParams);
                s.CirculatingSupply = oOut.Result["total_circulating_money_supply"];
                s.TotalSupply = 5121307024.00; //max supply
                s.TotalBurned = oOut.Result["total_burned"];
                return s;
            }
            catch (Exception ex)
            {
                Log("GS " + ex.Message);
            }
            return s;
        }

        internal static List<MasternodeListItem> GetMasternodeList(bool fTestNet)
        {
            List<MasternodeListItem> mli = new List<MasternodeListItem>();
            try
            {
                object[] oParams = new object[0];
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic l1 = n.SendCommand("masternodelist", oParams);
                string json = l1.ResultString;
                foreach (JToken j in l1.Result)
                {
                    List<JToken> tokens = j.Children().ToList();
                    string sJson = tokens[0].ToString();
                    string sOutpoint = tokens[0].Path;
                    sOutpoint = sOutpoint.Replace("result.", String.Empty);
                    MasternodeListItem m = Newtonsoft.Json.JsonConvert.DeserializeObject<MasternodeListItem>(sJson);
                    m.Outpoint = sOutpoint;
                    mli.Add(m);
                    string sHi = "";
                }
            }
            catch (Exception ex)
            {
                Log("GetMasternodelist " + ex.Message);
            }

            return mli;
        }


        internal static List<NFT> GetNFTs(bool fTestNet)
        {
            List<NFT> mli = new List<NFT>();
            try
            {
                object[] oParams = new object[1];
                oParams[0] = "listnfts";

                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic l1 = n.SendCommand("exec", oParams);
                string json = l1.ResultString;
                foreach (JToken j in l1.Result)
                {
                    List<JToken> tokens = j.Children().ToList();
                    string sJson = tokens[0].ToString();
                    string sID = tokens[0].Path;
                    if (sJson.Length > 10)
                    {
                        NFT n0 = Newtonsoft.Json.JsonConvert.DeserializeObject<NFT>(sJson);
                        mli.Add(n0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Get NFTS " + ex.Message);
            }

            return mli;
        }


        internal static string PackageBBPChainDataMessage(bool fTestNet, string sType, string sData, string sPrivKey)
        {
            string sSignMessage = Guid.NewGuid().ToString();
            string sSignature = Sanctuary.SignMessage(fTestNet, sPrivKey, sSignMessage);
            string sPubKey = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(fTestNet, sPrivKey);
            if (sSignature == null || sSignature == String.Empty)
                throw new Exception("Unable to sign.");
            string sXML = "<MK>" + sType + "</MK><MV>" + sData + "</MV><BOMSG>" + sSignMessage + "</BOMSG><BOSIG>" + sSignature
                + "</BOSIG><BOSIGNER>" + sPubKey + "</BOSIGNER>";
            return sXML;
        }

        public static string PushChainData2(bool fTestNet, string sType, string sData, string sPrivKey)
        {
            try
            {
                object[] oParams = new object[2];
                string sPackaged = PackageBBPChainDataMessage(fTestNet, sType, sData, sPrivKey);
                if (sPackaged.Length > 2000 && false)
                {
                    oParams = new object[3];
                    oParams[1] = sPackaged.Substring(0, 2000);
                    oParams[2] = sPackaged.Substring(2000, sPackaged.Length - 2000);
                }
                else
                {
                    oParams[1] = sPackaged;
                }
                oParams[0] = "bmstransaction";

                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("exec", oParams);
                string sTXID = oOut.Result["txid"];
                return sTXID;
            }
            catch (Exception ex)
            {
                Log("PCD " + ex.Message);
                return String.Empty;
            }
        }

        internal static BitcoinSyncBlock GetBlock(bool fTestNet, int nHeight)
        {
            BitcoinSyncBlock b = new BitcoinSyncBlock();

            try
            {
                object[] oParams = new object[1];
                oParams[0] = nHeight.ToString();
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("getblock", oParams);
                b.Hash = oOut.Result["hash"];
                b.MerkleRoot = oOut.Result["merkleroot"];
                b.Difficulty = oOut.Result["difficulty"];
                b.PreviousBlockHash = oOut.Result["previousblockhash"];
                b.NextBlockHash = oOut.Result["nextblockhash"];
                b.BlockNumber = nHeight;
                b.Time = oOut.Result["time"];
                for (int y = 0; y < oOut.Result["tx"].Count; y++)
                {
					BitcoinSyncTransaction t = new BitcoinSyncTransaction();
					t.TXID = oOut.Result["tx"][y];
                    t.Sequence = (short)y;
                    t.BlockHash = b.Hash;
                    t.Time = b.Time;
                    t.Height = b.BlockNumber;
                    t.BlockNumber = b.BlockNumber;
					BitcoinTransactionOverview bto = GetRawTransactionXML(t.TXID, fTestNet);
                    t.Data = bto.Payload;
                    t.Amounts = bto.Amounts;
                    t.Recipients = bto.Recipients;
                    t.Amount = bto.TotalAmount;
					b.Transactions.Add(t);
                }
            }
            catch (Exception ex)
            {
                Log("GBFS " + ex.Message);
            }
            return b;
        }


        internal static double QueryAddressBalanceCached(bool fTestNet, string sAddress, int nMaxAgeInSeconds)
        {
            string sCached = BMSCommon.MemoryCache.GetKeyValue("utxo_" + sAddress, nMaxAgeInSeconds);
            if (String.IsNullOrEmpty(sCached))
            {
                sCached = GetAddressUTXOs(fTestNet, sAddress);
                BMSCommon.MemoryCache.SetKeyValue("utxo_" + sAddress, sCached);
            }
            double nBal = QueryAddressBalanceNewMethod(fTestNet, sAddress, sCached);
            return nBal;
        }


        internal static double QueryAddressBalanceNewMethod(bool fTestNet, string sAddress)
        {
            if (sAddress==null)
            {
                return -1;
            }
            string sUTXOData = GetAddressUTXOs(fTestNet, sAddress);
            double nBal = QueryAddressBalanceNewMethod(fTestNet, sAddress, sUTXOData);
            return nBal;
        }
        internal static double QueryAddressBalanceNewMethod(bool fTestNet, string sAddress, string sData)
        {
            try
            {
                dynamic oJson = JsonConvert.DeserializeObject<dynamic>(sData);
                double nTotal = 0;
                foreach (var j in oJson)
                {
                    BalanceUTXOLegacy u = new BalanceUTXOLegacy();
                    u.Amount = new NBitcoin.Money((decimal)j["satoshis"], NBitcoin.MoneyUnit.Satoshi);
                    u.index = Convert.ToInt32(j["outputIndex"].Value);
                    u.TXID = new NBitcoin.uint256((string)j["txid"]);
                    u.Height = (int)j["height"].Value;
                    u.Address = j["address"].Value;
                    nTotal += (double)u.Amount.ToDecimal(MoneyUnit.BTC);
                }
                return nTotal;
            }
            catch (Exception)
            {
                // Wrong chain?
                return -1;
            }
        }
        internal static int GetMasternodeCount(bool fTestNet)
        {
            try
            {
                object[] oParams = new object[0];
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("masternodelist", oParams);
                string sData = oOut.Result.ToString();
                string[] vNodes = sData.Split("proTxHash");
                return vNodes.Length;

            }
            catch (Exception ex)
            {
                Log("GBFS " + ex.Message);
            }
            return 1;
        }

        internal static string GetBlockForStratumHex(bool fTestNet, string poolAddress, string rxkey, string rxheader)
        {
            try
            {
                object[] oParams = new object[3];
                oParams[0] = poolAddress;
                oParams[1] = rxkey;
                oParams[2] = rxheader;
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("getblockforstratum", oParams);
                string result = oOut.Result["hex"];
                return result;
            }
            catch (Exception ex)
            {
                Log("GBFS " + ex.Message);
            }
            return String.Empty;
        }

        internal static string SendMany(bool fTestNet, List<ChainPayment> p, string sFromAccount, string sComment)
        {
            string sPack = String.Empty;
            for (int i = 0; i < p.Count; i++)
            {
                string sAmount = string.Format("{0:#.00}", p[i].amount);
                string sRowOld = "\"" + p[i].bbpaddress + "\"" + ":" + sAmount;
                string sRow = "<RECIPIENT>" + p[i].bbpaddress + "</RECIPIENT><AMOUNT>" + sAmount + "</AMOUNT><ROW>";
                sPack += sRow;
            }
            string sXML = "<RECIPIENTS>" + sPack + "</RECIPIENTS>";
            try
            {
                object[] oParams = new object[4];
                oParams[0] = "sendmanyxml";
                oParams[1] = sFromAccount;
                oParams[2] = sXML;
                oParams[3] = sComment;
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("exec", oParams);
                string sTX = oOut.Result["txid"].ToString();
                return sTX;
            }
            catch (Exception ex)
            {
                string test = ex.Message;
                Log(" Error while transmitting : " + ex.Message);
                return String.Empty;
            }
        }

		public struct BalanceUTXOLegacy
		{
			public string Address;
			public NBitcoin.Money Amount;
			public NBitcoin.Money satoshis;
			public NBitcoin.uint256 TXID;
			public NBitcoin.uint256 prevtxid;
			public int index;
			public int Height;
		};


		internal static string GetAddressUTXOs(bool fTestNet, string address)
        {
            try
            {
                if (String.IsNullOrEmpty(address))
                    return String.Empty;
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                NBitcoin.BitcoinAddress[] a = new NBitcoin.BitcoinAddress[1];
                a[0] = NBitcoin.BitcoinAddress.Create(address, fTestNet ? NBitcoin.Network.TestNet : NBitcoin.Network.Main);
                dynamic o11 = n.GetAddressUTXOs(a);
                dynamic o12 = n.GetAddressMempool(a);
                List<BalanceUTXOLegacy> l1 = new List<BalanceUTXOLegacy>();
                dynamic oJson = JsonConvert.DeserializeObject<dynamic>(o11.Result.ToString());
                dynamic oJson2 = JsonConvert.DeserializeObject<dynamic>(o12.Result.ToString());
                foreach (var j in oJson)
                {
                    BalanceUTXOLegacy u = new BalanceUTXOLegacy();
                    u.Amount = new NBitcoin.Money((decimal)j["satoshis"], NBitcoin.MoneyUnit.Satoshi);
                    u.satoshis = u.Amount;
                    u.index = Convert.ToInt32(j["outputIndex"].Value);
                    u.TXID = new NBitcoin.uint256((string)j["txid"]);
                    u.Height = (int)j["height"].Value;
                    u.Address = j["address"].Value;
                    l1.Add(u);
                }
                foreach (var j in oJson2)
                {
                    BalanceUTXOLegacy u = new BalanceUTXOLegacy();
                    u.Amount = new NBitcoin.Money((decimal)j["satoshis"], NBitcoin.MoneyUnit.Satoshi);
                    u.satoshis = u.Amount;
                    u.index = Convert.ToInt32(j["index"].Value);
                    u.TXID = new NBitcoin.uint256((string)j["txid"]);
                    //u.Height = (int)j["height"].Value;
                    u.Address = j["address"].Value;
                    if (j["prevtxid"] != null)
                    {
                        u.prevtxid = new NBitcoin.uint256((string)j["prevtxid"]);
                    }

                    if (u.Amount > Money.Zero)
                    {
                        l1.Add(u);
                    }
                    else if (u.Amount < Money.Zero)
                    {
                        for (int i = 0; i < l1.Count; i++)
                        {
                            if (l1[i].TXID == u.prevtxid)
                            {
                                l1.RemoveAt(i);
                            }
                        }
                    }
                }
                List<BalanceUTXO2> l2 = new List<BalanceUTXO2>();
                for (int i = 0; i < l1.Count; i++)
                {
                    BalanceUTXO2 b = new BalanceUTXO2();
                    b.address = l1[i].Address;
                    b.satoshis = l1[i].satoshis.Satoshi.ToString();
                    b.height = l1[i].Height;
                    b.txid = l1[i].TXID.ToString();
                    b.index = l1[i].index;
                    b.outputIndex = b.index;
                    l2.Add(b);
                }
                string sNewResult = JsonConvert.SerializeObject(l2, Formatting.Indented);
                return sNewResult;
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }


        internal static async Task<DACResult> SendRawTx(bool fTestNet, string hex, string sPRETXID)
        {
            DACResult r0 = new DACResult();
            try
            {
                object[] oParams = new object[2];
                oParams[0] = hex;
                oParams[1] = "0"; //0=Bypass txfee limits
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                var ct = new CancellationTokenSource(12500).Token;
                dynamic oOut = await n.SendCommand2("sendrawtransaction", ct, oParams);
                if (r0.TXID == null)
                {
                    r0.Error = "Unable to push.";
                    return r0;
                }
                else
                {
                    r0.TXID = sPRETXID;
                }
                return r0;
            }
            catch (Exception ex)
            {
                Log("SendRawTx:: " + ex.Message);
                r0.Error = ex.Message;
                return r0;
            }
        }

        internal static List<string> listRPCErrors = new List<string>();

        internal static void LogRPCError(string sError)
        {
            if (listRPCErrors.Contains(sError))
                return;

            listRPCErrors.Add(sError);
            if (listRPCErrors.Count > 50)
            {
                listRPCErrors.RemoveAt(0);
            }
        }

        private static int nMyCounter = 0;
        internal static NBitcoin.RPC.RPCClient GetRPCClient(bool fTestNet)
        {
            NBitcoin.RPC.RPCClient n = null;
            string sUser = String.Empty;
            string sPass = String.Empty;
            string sHost = String.Empty;
            string sTheUser = String.Empty;
            try
            {
                NBitcoin.RPC.RPCCredentialString r = new NBitcoin.RPC.RPCCredentialString();
                sUser = fTestNet ? "testnetrpcuser" : "rpcuser";
                sPass = fTestNet ? "testnetrpcpassword" : "rpcpassword";
                string sH = fTestNet ? "testnetrpchost" : "rpchost";
                sHost = SecureString.GetDBConfigurationKeyValue(sH);
                System.Net.NetworkCredential t = new System.Net.NetworkCredential(SecureString.GetDBConfigurationKeyValue(sUser),
                    SecureString.GetDBConfigurationKeyValue(sPass));
               
                if (fTestNet)
                {
                    sHost = "sanc4.biblepay.org:20001";
                    sUser = "biblepay";
                    t = new System.Net.NetworkCredential(sUser,sPass);
                }

                if (false)
                {
                    sHost = "seven.biblepay.org:20000";
                    sUser = "bbpusername";
                    t = new System.Net.NetworkCredential(sUser, "bbp_debug_password");
                }

                r.UserPassword = t;

                n = new NBitcoin.RPC.RPCClient(r, sHost, fTestNet ? NBitcoin.Network.TestNet : NBitcoin.Network.Main);
                if (nMyCounter == 0)
                {
                    string sTNNarr = fTestNet ? "TESTNET" : "MAINNET";
                    string sNarr = "UNKNOWN IF::RPCCLIENT FOR " + sTNNarr + " for host [" + sHost + "] using user [" + sUser + "] with a password length of " + sPass.Length.ToString();
                    LogRPCError(sNarr);
                }
                nMyCounter++;
                return n;
            }
            catch (Exception ex)
            {
                string sTNNarr = fTestNet ? "TESTNET" : "MAINNET";
                string sNarr = "UNABLE TO GET RPCCLIENT FOR " + sTNNarr + " for host [" + sHost + "] using user [" + sUser + " " + sTheUser + "] with a password length of " + sPass.Length.ToString() + ". (" + ex.Message + ")";
                LogRPCError(sNarr);
                Log(sNarr);
                return n;
            }
        }

        internal static int GetHeight(bool fTestNet)
        {
            try
            {
                object[] oParams = new object[1];
                NBitcoin.RPC.RPCClient n = GetRPCClient(fTestNet);
                dynamic oOut = n.SendCommand("getmininginfo");
                int nBlocks = (int)GetDouble(oOut.Result["blocks"]);
                return nBlocks;
            }
            catch (Exception zx)
            {
                return -1;
            }
        }

        internal static bool ValidateAddress(bool fTestNet, string sAddress)
        {
            try
            {
                NBitcoin.RPC.RPCClient nClient = GetRPCClient(fTestNet);

                object[] oParams = new object[1];
                oParams[0] = sAddress;
                dynamic oOut = nClient.SendCommand("validateaddress", oParams);
                bool fValid = oOut.Result["isvalid"];
                return fValid;
            }
            catch (Exception ex)
            {
                Log("Unable to validate address::" + ex.Message);
                return false;
            }
        }

        internal static async Task<DACResult> SendBBPOutsideChain(bool fTestNet, string sType, string sToAddress, string sPrivKey,
            double nAmount, string sPayload)
        {

            string sSpendingPublicKey = ERCUtilities.GetPubKeyFromPrivKey(fTestNet, sPrivKey);
            string sUnspentData = GetAddressUTXOs(fTestNet, sSpendingPublicKey);
            string sErr = String.Empty;
            string sTXID = String.Empty;
            string sHex = String.Empty;
            NBitcoin.Crypto.BBPTransaction.PrepareFundingTransaction(fTestNet, nAmount, sToAddress, sPrivKey, sPayload, 
                sUnspentData, out sErr, out sHex, out sTXID);
            DACResult r = new DACResult();
            if (sErr != String.Empty)
            {
                r.Error = sErr;
                return r;
            }
            for (int i = 0; i < 3; i++)
            {
                r = await SendRawTx(fTestNet, sHex, sTXID);
                if (r.Error == String.Empty) break;
            }
            if (r.Error == "Valid") r.Error = "Memory Pool Invalidation Error (ASSET)";

            return r;
        }
    }
}
