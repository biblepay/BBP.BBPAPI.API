namespace BBP.CORE.API.Service
{
    using bbp.core.api.Controllers;
    using BBPAPI;
    using BMSCommon.Model;
    using global::BBP.CORE.API.Utilities;
    using NBitcoin;
    using NBitcoin.Crypto;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using static bbp.core.api.Controllers.AtomicTradeController;
    using static BMSCommon.Encryption;
    using static BMSCommon.Model.BitcoinSyncModel;

    namespace Doggy
    {
        public class TxRef
        {
            public string tx_hash { get; set; }
            public int block_height { get; set; }
            public int tx_input_n { get; set; }
            public int tx_output_n { get; set; }
            public long value { get; set; }
            public int confirmations { get; set; }
            public string script { get; set; }
            public TxRef()
            {
                tx_hash = String.Empty;
                script = String.Empty;
            }
        }
        public class BlockCypherDoge
        {
            public List<TxRef> TxRefs = new List<TxRef>();
        }


        public static class Dogone
        {
            // This dictionary holds a list of atomic tx IDs where the buyer or seller cannot afford to complete the trade.
            // Once the age is over 60 mins, we cancel the trade.
            private static Dictionary<string, DateTime> dictDeadbeats = new Dictionary<string, DateTime>();

            public static string GetMasterAssetKey()
            {
                //  "Asset": "BQXuB2WTrMgnGMJHGwjQhbzwBgUC7AMMzZ",
                string sAssetKey = BMSCommon.Common.GetConfigKeyValue("masterassetdogekey");
                return sAssetKey;
            }

            public async static Task<CryptoTransmissionResult> SendDogeTx(double nAmt, string ToAddress, string sDogeKey)
            {
                CryptoTransmissionResult result = new CryptoTransmissionResult();
                string sDogePubKey = ERCUtilities.GetPubKeyFromPrivKey("dogenet", sDogeKey);
                BMSCommon.Common.Log("SendDogeTX 2.1::Sending Amt=" + nAmt.ToString() + " TO " + ToAddress 
                    + " privkey " + sDogeKey + " from pubkey " + sDogePubKey);
                string sDogeUTXO = DOGE.GetDogeUtxos(sDogePubKey);
                BMSCommon.Common.Log("DOGE UTXOS::" + sDogeUTXO);
                string Error;
                string TxHex;
                string TXID;
                bool f1 = BBPTransaction.PrepareDOGEFundingTransaction(nAmt,
                   ToAddress, sDogeKey, sDogeUTXO, out Error, out TxHex, out TXID);
                BMSCommon.Common.Log("Pre TXID::" + TXID + ", ERROR=" + Error);
                TXID = await DOGE.BroadcastTx(TxHex, TXID);
                BMSCommon.Common.Log("Post TXID::" + TXID);
                result.Error = Error;
                result.TxHex = TxHex;
                result.TXID = TXID;
                return result;
            }


            public async static Task<CryptoTransmissionResult> DEBUGSendDogeTx(double nAmt, string ToAddress, string sDogeKey)
            {
                CryptoTransmissionResult result = new CryptoTransmissionResult();
                string sDogePubKey = ERCUtilities.GetPubKeyFromPrivKey("dogenet", sDogeKey);
                BMSCommon.Common.Log("SendDogeTX 2.1::Sending Amt=" + nAmt.ToString() + " TO " + ToAddress + " privkey " + sDogeKey + " from pubkey " + sDogePubKey);
                string sDogeUTXO = DOGE.GetDogeUtxos(sDogePubKey);
                BMSCommon.Common.Log("DOGE UTXOS::" + sDogeUTXO);
                string Error;
                string TxHex;
                string TXID;
                bool f1 = BBPTransaction.PrepareDOGEFundingTransactionDEBUG(nAmt,
                   ToAddress, sDogeKey, sDogeUTXO, out Error, out TxHex, out TXID);
                BMSCommon.Common.Log("Pre TXID::" + TXID + ", ERROR=" + Error);
                TXID = await DOGE.BroadcastTx(TxHex, TXID);
                BMSCommon.Common.Log("Post TXID::" + TXID);
                result.Error = Error;
                result.TxHex = TxHex;
                result.TXID = TXID;
                return result;
            }


            public static async Task<bool> RefundAtomicTransactionBBP(AtomicTransaction2 a, string sToAddress)
            {
                BBPKeyPair kpBBP = await Ethereum.DeriveAltcoinKey("biblepay", a.id);
                // Send BBP back to original User
                double nAmt = a.Quantity - 2.0;
                BMSCommon.Common.Log("RefundAtomicTransactionBBP::REFUND_SELL::" + a.CollateralBBPAddress);
                DACResult d = await WebRPC.SendBBPOutsideChain(false, "atomictransaction", sToAddress, kpBBP.PrivKey, nAmt, "");
                a.Error = d.Error;
                a.ReturnTXID = d.TXID;
                BMSCommon.Common.Log("REFUNDATOMICTX_Collateral::BBP_COLLATERAL_ADDRESS=" + a.CollateralBBPAddress + " Refunded on TXID " + sToAddress);
                return true;
            }

            public static async Task<bool> RefundAtomicTransactionDoge(AtomicTransaction2 a, string sToAddress)
            {
                BBPKeyPair kpDoge = await Ethereum.DeriveAltcoinKey("doge", a.id);
                BMSCommon.Common.Log("RefundAtomicTransaction_DOGE::REFUND_BUY::Sending from " + a.CollateralDOGEAddress + " TO " + sToAddress);
                double nAmt = a.Price * a.Quantity;
                nAmt = nAmt - .0325;
                CryptoTransmissionResult ctr = await Dogone.SendDogeTx(nAmt, sToAddress, kpDoge.PrivKey);
                a.Error = ctr.Error;
                a.ReturnTXID = ctr.TXID;
                BMSCommon.Common.Log("RefundAtomicTransaction_DOGE::REFUND_BUY::Collateral DOGE Address " + a.CollateralDOGEAddress + " TXID " + sToAddress);
                return true;
            }


            public static List<UTXO> GetSpecifiedDogeUTXOLocal(string sTXID, string sData)
            {
                List<UTXO> lUTXO = new List<UTXO>();
                try
                {
                    BlockCypherDoge bcd = JsonConvert.DeserializeObject<BlockCypherDoge>(sData);
                    NBitcoin.Money nTotalAdded = Money.Zero;
                    for (int i = 0; i < bcd.TxRefs.Count; i++)
                    {
                        TxRef txref0 = bcd.TxRefs[i];   
                        UTXO u = new UTXO();
                        u.Amount =  new NBitcoin.Money((decimal)txref0.value, NBitcoin.MoneyUnit.Satoshi);
                        u.index = Convert.ToInt32(txref0.tx_output_n);
                        u.TXID = new NBitcoin.uint256(txref0.tx_hash);
                        u.Height = txref0.block_height;
                        u.Address = "";
                        if (u.TXID.ToString() == sTXID)
                        {
                            lUTXO.Add(u);
                        }
                    }
                    return lUTXO;
                }
                catch (Exception ex)
                {
                    return lUTXO;
                }
            }


            public static List<UTXO> GetSpecifiedBBPUTXOLocal(string sTXID, string sData)
            {
                List<UTXO> lUTXO = new List<UTXO>();

                try
                {
                    dynamic oJson = JsonConvert.DeserializeObject<dynamic>(sData);
                    NBitcoin.Money nTotalAdded = Money.Zero;
                    foreach (var j in oJson)
                    {
                        UTXO u = new UTXO();
                        u.Amount = new NBitcoin.Money((decimal)j["satoshis"], NBitcoin.MoneyUnit.Satoshi);
                        u.index = Convert.ToInt32(j["outputIndex"].Value);
                        u.TXID = new NBitcoin.uint256((string)j["txid"]);
                        u.Height = (int)j["height"].Value;
                        u.Address = j["address"].Value;
                        if (u.TXID.ToString() == sTXID)
                        {
                            lUTXO.Add(u);
                        }
                    }
                    return lUTXO;
                }
                catch(Exception ex)
                {
                    return lUTXO;
                }
            }

            private async static Task<bool> VerifyUtxo(string sAddress, string sTXID, double nAmt, string sChain)
            {
                string sUTXO = String.Empty;
                List<UTXO> lUTXO = new List<UTXO>();
                if (sChain == "doge")
                {
                    sUTXO = DOGE.GetDogeUtxos(sAddress);
                    lUTXO = GetSpecifiedDogeUTXOLocal(sTXID, sUTXO);
                }
                else
                {
                    sUTXO = WebRPC.GetAddressUTXOs(false, sAddress);
                    lUTXO = GetSpecifiedBBPUTXOLocal(sTXID, sUTXO);
                }

                Money nAmtMin = new Money((decimal)nAmt, MoneyUnit.BTC);
                if (lUTXO.Count > 0)
                {
                    if (lUTXO[0].Amount >= nAmtMin)
                        return true;
                }
                return false;
            }
            private async static Task<bool> CheckCollateral(AtomicTransaction2 oBuy, AtomicTransaction2 oSell)
            {

                bool fOKBBP = await VerifyUtxo(oSell.CollateralBBPAddress, oSell.CollateralTXID, oSell.Quantity, "bbp");
                bool fOKDoge = await VerifyUtxo(oBuy.CollateralDOGEAddress, oBuy.CollateralTXID, (oBuy.Price * oBuy.Quantity - .25), "doge");
                return fOKBBP && fOKDoge;
            }

            public async static Task<bool> ForceCancelTrade(string ID)
            {
                List<AtomicTransaction5> lAll = QuorumUtils.GetDatabaseObjects<AtomicTransaction5>("AtomicTransaction5");
                List<AtomicTransaction5> lC = lAll.Where(a => a.id == ID).ToList();
                if (lC.Count > 0)
                {
                    lC[0].Status = "canceled";
                    List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(lC);
                    DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
                    return true;
                }

                return false;
            }

            private async static Task<bool> UpdateAtomicTransaction(AtomicTransaction5 a)
            {
                List<AtomicTransaction5> l = new List<AtomicTransaction5>();
                l.Add(a);
                List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(l);
                DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
                return dac1.Result;
            }
            private static string YesNo(bool fOK)
            {
                return fOK ? "YES" : "NO";
            }

            private static async Task<bool> RegisterDeadbeat(AtomicTransaction5 aBuyer)
            {
                if (!dictDeadbeats.ContainsKey(aBuyer.id))
                {
                    dictDeadbeats[aBuyer.id] = DateTime.Now;
                }
                else
                {
                    // we already seen this deadbeat tx.
                    TimeSpan ts = DateTime.Now - dictDeadbeats[aBuyer.id];

                    if (ts.TotalSeconds > (60 * 60))
                    {
                        // this tx is older than an hour..cancel the trade for this deadbeat.
                        await ForceCancelTrade(aBuyer.id);
                        dictDeadbeats.Remove(aBuyer.id);
                        return true;
                    }
                }
                return false;
            }
            
            private static async Task<bool> CheckBalances(AtomicTransaction5 aBuy, AtomicTransaction5 aSell)
            {
                double nBuyerBBPBalance = AtomicTradeController.GetBBPBalance(aBuy.CollateralAssetAddress);
                double nSellerBBPBalance = AtomicTradeController.GetBBPBalance(aSell.Signer);
                double nTotal = aSell.Quantity * aSell.Price;
                if (nBuyerBBPBalance < nTotal)
                {
                    await RegisterDeadbeat(aBuy);
                }
                if (nSellerBBPBalance < aSell.Quantity)
                {
                    await RegisterDeadbeat(aSell);
                }
                if (nBuyerBBPBalance > nTotal && nSellerBBPBalance > aSell.Quantity)
                {
                    return true;
                }
                return false;
            }


            private static async Task<bool> MatchAtomicTransactionBBPColored0(AtomicTransaction5 aSell, AtomicTransaction5 aBuy)
            {
                // buying with colored, selling BBP
                string sBBPPrivKeyBuyer = BlockCypherFunctions.DecryptString(aBuy.EncryptedBBPPrivKey, aBuy.id);
                string sBBPPubKeyBuyer = ERCUtilities.GetPubKeyFromPrivKey(false, sBBPPrivKeyBuyer);
                string sBBPPrivKeySeller = BlockCypherFunctions.DecryptString(aSell.EncryptedBBPPrivKey, aSell.id);
                string sBBPPrivKeyBuyerAlt = BlockCypherFunctions.DecryptString(aBuy.EncryptedAssetPrivKey, aBuy.id);
                string sBBPPubKeyBuyerAlt = ERCUtilities.GetPubKeyFromPrivKey(false, sBBPPrivKeyBuyerAlt); //COLORED
                string sBBPPrivKeySellerAlt = BlockCypherFunctions.DecryptString(aSell.EncryptedAssetPrivKey, aSell.id);
                string sBBPPubKeySellerAlt = ERCUtilities.GetPubKeyFromPrivKey(false, sBBPPrivKeySellerAlt); //COLORED
                // *** SELLER **** Transmit sellers normal BBP, to Buyers BBP

                DACResult d1 = await WebRPC.SendBBPOutsideChain(false, "atomictransaction", sBBPPubKeyBuyer, sBBPPrivKeySeller, aSell.Quantity, "");
                aSell.Error = d1.Error;
                aSell.ReturnTXID = d1.TXID;

                // **** BUYER ***  Transmit Colored from Buyer to Sellers Colored address
                double nAmt = aSell.Quantity * aSell.Price;
                DACResult d2 = await WebRPC.SendBBPOutsideChain(false, "atomictransaction", sBBPPubKeySellerAlt, sBBPPrivKeyBuyerAlt, nAmt, "");
                if (d2.Error.Contains("Memory Pool"))
                {
                    aBuy.Status = "error";
                    aBuy.Error = "memory-pool-error";
                    await UpdateAtomicTransaction(aBuy);
                }
                else
                {
                    aBuy.Error = d2.Error;
                    aBuy.ReturnTXID = d2.TXID;
                }
                bool fOK = d1.TXID != "" && d2.TXID != "";
                return fOK;
            }


            private static int nLastAtomicMatch = 0;
            public async static Task<bool> MatchAtomicTrades()
            {
                int nElapsed = BMSCommon.Common.UnixTimestamp() - nLastAtomicMatch;
                if (nElapsed > 60)
                {
                    try
                    {
                        nLastAtomicMatch = BMSCommon.Common.UnixTimestamp();
                        // Open Trades that match.
                        List<AtomicTransaction5> lAll = QuorumUtils.GetDatabaseObjects<AtomicTransaction5>("AtomicTransaction5");
                        List<AtomicTransaction5> lSells = lAll.Where(a => a.Status == "open" && a.Action == "sell").ToList();
                        List<AtomicTransaction5> lBuys = lAll.Where(a => a.Status == "open" && a.Action == "buy").ToList();
                        for (int iSell = 0; iSell < lSells.Count; iSell++)
                        {
                            AtomicTransaction5 aSell = lSells[iSell];
                            // if it matched a buy
                            for (int iBuys = 0; iBuys < lBuys.Count; iBuys++)
                            {
                                AtomicTransaction5 aBuy = lBuys[iBuys];
                                if (aBuy.Quantity == aSell.Quantity && aSell.Price <= aBuy.Price)
                                {
                                    // We matched on quantity and buying at or lower.
                                    // Need to verify if both users have a good bbp balance.
                                    bool fBalOK = await CheckBalances(aBuy, aSell);
                                    if (fBalOK)
                                    {
                                        // BBP
                                        bool fBBPRefundedOK = await MatchAtomicTransactionBBPColored0(aSell, aBuy);
                                        if (aSell.ReturnTXID.Length > 1)
                                        {
                                            aSell.Status = "filled";
                                            await UpdateAtomicTransaction(aSell);
                                        }
                                        if (aBuy.ReturnTXID.Length > 1)
                                        {
                                            aBuy.Status = "filled";
                                            await UpdateAtomicTransaction(aBuy);
                                        }

                                        bool f14001 = false;
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        BMSCommon.Common.Log("MatchAtomicTrades___ERROR::" + ex.Message + "," + ex.StackTrace);
                    }

                }
                return true;
            }

            public static List<UTXO> ParseUTXOS(string sData)
            {
                List<UTXO> lUTXO = new List<UTXO>();
                dynamic oJson = JsonConvert.DeserializeObject<dynamic>(sData);
                NBitcoin.Money nTotalAdded = Money.Zero;
                List<UTXO> lAllUTXO = new List<UTXO>();
                if (oJson == null)
                {
                    return lAllUTXO;
                }
                var oNodes = oJson["txrefs"];
                if (oNodes != null)
                {
                    foreach (var j in oNodes)
                    {
                        UTXO u = new UTXO();
                        u.Amount = new NBitcoin.Money((decimal)j["value"], NBitcoin.MoneyUnit.Satoshi);
                        u.index = Convert.ToInt32(j["tx_output_n"].Value);
                        u.TXID = new NBitcoin.uint256((string)j["tx_hash"]);
                        u.Height = (int)j["block_height"].Value;
                        lAllUTXO.Add(u);
                    }
                }
                return lAllUTXO;
            }
        }
    }

    namespace BBP.CORE.API.Utilities
    {
        public static class DOGELegacy
        {
            public static string GetBlockCypherToken()
            {
                return BMSCommon.Common.GetConfigKeyValue("blockcyphertoken");
            }
            public static string GetDogeUtxos(string sAddress)
            {
                string sURL = "https://api.blockcypher.com/v1/doge/main/addrs/" 
                    + sAddress + "?unspentOnly=true&includeScript=true&token=" + GetBlockCypherToken();
                string sData = BMSCommon.Functions.ExecuteMVCCommand(sURL);
                return sData;
            }

            public static async Task<string> BroadcastTx(string sTxInfo)
            {
                if (sTxInfo == null || sTxInfo == String.Empty)
                {
                    return String.Empty;
                }
                string sURL = "https://api.blockcypher.com/v1/doge/main/txs/push?token=" + GetBlockCypherToken(); 
                try
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), sURL))
                        {
                            httpClient.Timeout = new System.TimeSpan(0, 60, 00);
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            // the following line is not good, but OK for debugging:
                            string sData = "tx_hex='" + sTxInfo + "'";
                            sData = "{\"tx\": \"" + sTxInfo + "\"}";
                            StringContent st1 = new StringContent(sData);
                            var oInitialResponse = await httpClient.PostAsync(sURL, st1);
                            string sJsonResponse = await oInitialResponse.Content.ReadAsStringAsync();
                            dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject(sJsonResponse);
                            string sTXID = o["tx"]["hash"].ToString() ?? String.Empty;
                            return sTXID;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return String.Empty;
                }
                return String.Empty;
            }

        }
    }

}
