using BBP.CORE.API.Service.BBP.CORE.API.Utilities;
using BBP.CORE.API.Service.Doggy;
using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.Crypto;
using static BMSCommon.Encryption;
using static BMSCommon.Model.BitcoinSyncModel;

namespace bbp.core.api.Controllers;

[ApiController]
public class AtomicTradeController 
{
    public AtomicTradeController()
    {
    }

    public static double GetDogeBalance(string sAddress)
    {
        string sDogeUTXO = DOGE.GetDogeUtxos(sAddress);
        BlockCypherUTXO b = Utils.Deserialize<BlockCypherUTXO>(sDogeUTXO);
        double nBalance = Convert.ToDouble(b.final_balance) / 100000000;
        return nBalance;
    }

    public static double GetBBPBalance(string sAddress)
    {
        string sBBPUTXO = WebRPC.GetAddressUTXOs(false, sAddress);
        double nBal = WebRPC.GetBalanceFromUtxos(sBBPUTXO);
        return nBal;
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [Route("api/AtomicTrading/GetDOGEBalance")]
    public string GetBalance([FromHeader] string Address)
    {
        try
        {
            double nBalance = GetDogeBalance(Address);
            string sOut = "<BALANCE>" + nBalance.ToString() + "</BALANCE><EOF></EOF>\r\n";
            return sOut;
        }
        catch(Exception ex)
        {
            BMSCommon.Common.Log("GetBalance::" + ex.Message);
            return "<BALANCE>-1</BALANCE><EOF></EOF>\r\n";
        }
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [Route("api/AtomicTrading/GetDOGETXID")]
    public string GetDOGETXID([FromHeader] string s)
    {
        try
        {
            AtomicTransaction2 sTX = Utils.Deserialize<AtomicTransaction2>(s);
            int nHeight = 0;
            if (sTX != null && sTX.Status == "processed")
            {
                string sDogeUTXO = DOGE.GetDogeUtxos(sTX.AltAddress);
                List<UTXO> lUTXO = Dogone.ParseUTXOS(sDogeUTXO);
                foreach (UTXO utxo in lUTXO)
                {
                    decimal nAmount = (decimal)(sTX.Quantity * sTX.Price);
                    Money mAmount = new Money(nAmount, MoneyUnit.BTC);
                    string sTXID = utxo.TXID.ToString();
                    if ( sTXID == sTX.TXID &&  utxo.Height >= sTX.Height && utxo.Amount >= mAmount)
                    {
                        nHeight = utxo.Height;
                    }
                }
            }
            string sOut = "<UTXO><height>" + nHeight.ToStr() + "</height></UTXO><EOF></EOF>\r\n";
            return sOut;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("GetDOGETXID::" + ex.Message);
            string s1= "<UTXO><height>-1</height></UTXO><EOF></EOF>\r\n";
            return s1;
        }
    }
    public class CryptoTransmissionResult
    {
        public string TXID { get; set; }
        public string TxHex { get; set; }
        public string Error { get; set; }
        public CryptoTransmissionResult()
        {
            TXID = String.Empty;
            TxHex = String.Empty;   
            Error = String.Empty;   
        }
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [Route("api/AtomicTrading/SendDOGE")]
    public async Task<string> SendDOGE([FromHeader] string EncryptedKey, [FromHeader] string Password, [FromHeader] string ToAddress, [FromHeader] string Amount)
    {
        try
        {
            string sDogeKey = BlockCypherFunctions.DecryptString(EncryptedKey, Password);
            string sDogePubKey = ERCUtilities.GetPubKeyFromPrivKey("dogenet", sDogeKey);
            double nAmt = BMSCommon.Common.GetDouble(Amount);

            if (sDogePubKey == String.Empty)
            {
                return "<ERROR>Invalid Doge Key</ERROR></EOF>\r\n";
            }
            if (ToAddress == String.Empty)
            {
                return "<ERROR>Invalid To Address</ERROR></EOF>\r\n";
            }
            if (Amount == String.Empty || nAmt == 0)
            {
                return "<ERROR>Amount must be populated</ERROR></EOF>\r\n";
            }
            CryptoTransmissionResult ctr = await Dogone.SendDogeTx(nAmt, ToAddress, sDogeKey);
        
            if (ctr.Error != String.Empty)
            {
                return "<ERROR>" + ctr.Error + "</ERROR></EOF>\r\n";
            }
            if (ctr.TXID == String.Empty)
            {
                return "<ERROR>Unable to fully sign tx.</ERROR></EOF>\r\n";
            }
            // broadcast
            if (ctr.TXID == String.Empty || ctr.TXID.Length != 64)
            {
                return "<ERROR>Unable to broadcast tx.</ERROR></EOF>\r\n";
            }
            return "<TXID>" + ctr.TXID + "</TXID></EOF>\r\n";
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("GetBalance::" + ex.Message);
            return "<BALANCE>-1</BALANCE><EOF></EOF>\r\n";
        }
    }


    private static int iIterator = 0;

    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [Route("api/AtomicTrading/GetPrimaryKey")]
    public string GetPrimaryKey([FromHeader] string EncryptedTrade, [FromHeader] string Password)
    {
        try
        {
            return "<EOF>RETIRED</EOF>\r\n";
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("GetPrimaryKey::" + ex.Message + " " + ex.StackTrace);
            return "<ERROR>Unable to derive collateral address.</ERROR><EOF></EOF>\r\n";
        }
    }

    private string Sz3(AtomicTransaction5 a)
    {
        string sResult = "<ATOMIC>" + Utils.Serialize(a) + "</ATOMIC>\r\n<EOF></EOF>\r\n";
        return sResult;
    }


    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [DisableRequestSizeLimit]
    [Route("api/AtomicTrading/CancelAtomicTransaction")]

    public string CancelAtomicTransaction([FromHeader] string EncryptedBBPKey, [FromHeader] string EncryptedDogeKey, 
        [FromHeader] string Password, [FromHeader] string AtomicTrade)
    {
        return "<EOF>RETIRED</EOF>\r\n";
    }


    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [Route("api/AtomicTrading/GetOrderBookV2")]
    public string GetOrderBookV2()
    {
        List<AtomicTransaction5> l = QuorumUtils.GetDatabaseObjects<AtomicTransaction5>("AtomicTransaction5");
        l = l.Where(a => a.Status == "open").ToList();
        string sData = "<TRADES>";
        BMSCommon.Common.Log("GetorderbookData count " + l.Count.ToString());
        for (int i = 0; i < l.Count; i++)
        {
            string sRow = Newtonsoft.Json.JsonConvert.SerializeObject(l[i]);
            sData += sRow + "<ATOMICTRANSACTION>";
        }
        sData += "</TRADES><EOF></EOF>\r\n";
        return sData;
    }



    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [DisableRequestSizeLimit]
    [Route("api/AtomicTrading/CancelAtomicTransactionV2")]
    public async Task<string> CancelAtomicTransactionV2([FromHeader] string EncryptedBBPKey, [FromHeader] string EncryptedDogeKey,
    [FromHeader] string Password, [FromHeader] string AtomicTrade)
    {
        AtomicTransaction5 a = new AtomicTransaction5();
        try
        {
            string sAtomicTrade = BlockCypherFunctions.DecryptString(AtomicTrade, Password);
            a = Utils.Deserialize<AtomicTransaction5>(sAtomicTrade);
            BMSCommon.Common.Log("v3.1::CancelAtomic::" + AtomicTrade + " " + a.id);
            List<AtomicTransaction5> l = QuorumUtils.GetDatabaseObjects<AtomicTransaction5>("AtomicTransaction5");
            l = l.Where(b => b.id == a.id).ToList();
            if (l.Count == 0)
            {
                throw new Exception("Unable to find atomic transaction.");
            }
            a = l[0];

            string sDBAtomic = Newtonsoft.Json.JsonConvert.SerializeObject(a);
            BMSCommon.Common.Log(sDBAtomic);
            string sRoom = a.SymbolSell + "/" + a.SymbolBuy;
            if (sRoom != "bbp/doge" && sRoom != "doge/bbp")
            {
                throw new Exception("Invalid room");
            }
            string sBBPPrivKey = BlockCypherFunctions.DecryptString(EncryptedBBPKey, Password);
            string sBBPPubKey = ERCUtilities.GetPubKeyFromPrivKey(false, sBBPPrivKey);
            if (sBBPPubKey != a.Signer)
            {
                throw new Exception("You must own the transaction to cancel it.");
            }
            a.Status = "canceled";
            List<AtomicTransaction5> lAT = new List<AtomicTransaction5>();
            lAT.Add(a);
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(lAT);
            DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
            // return the collateral TXID in the response.
            string sResult = Sz3(a);
            return sResult;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("CancelAtomic::" + ex.Message + "\r\n" + ex.StackTrace);
            a.Error = "Unable to cancel atomic. " + ex.Message;
            return Sz3(a);
        }
    }


    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [DisableRequestSizeLimit]
    [Route("api/AtomicTrading/TransmitAtomicTransactionV2")]

    public async Task<string> TransmitAtomicTransactionV2(
        [FromHeader] string EncryptedBBPKey, [FromHeader] string EncryptedDogeKey,
        [FromHeader] string Password, [FromHeader] string AtomicTrade, [FromHeader] string EncryptedAssetKey)
    {
        AtomicTransaction5 a = new AtomicTransaction5();

        try
        {
            iIterator++;
            string sAtomicTrade = BlockCypherFunctions.DecryptString(AtomicTrade, Password);
            a = Utils.Deserialize<AtomicTransaction5>(sAtomicTrade);
            a.id = BMSCommon.Common.UnixTimestamp().ToStr() + "-" + iIterator.ToStr();
            BMSCommon.Common.Log("v3.0::" + AtomicTrade);
            string sRoom = a.SymbolSell + "/" + a.SymbolBuy;

            if (sRoom != "bbp/doge" && sRoom != "doge/bbp")
            {
                throw new Exception("Invalid room");
            }
            if (a.Quantity < 1000 && a.Action.ToLower() == "sell")
            {
                throw new Exception("Sell quantity too low.");
            }

            double nTotalBuy = a.Quantity * a.Price;
            if (a.Action.ToLower() == "buy" && nTotalBuy < .10)
            {
                throw new Exception("Buy Amount must be at least .10 DOGE");
            }
            
            if (a.id.Length < 4)
            {
                throw new Exception("ID must be longer.");
            }
            string sBBPPrivKey = BlockCypherFunctions.DecryptString(EncryptedBBPKey, Password);
            string sAltPrivKey = BlockCypherFunctions.DecryptString(EncryptedDogeKey, Password); 
            string sAssetPrivKey = BlockCypherFunctions.DecryptString(EncryptedAssetKey, Password);

            a.EncryptedBBPPrivKey = BlockCypherFunctions.EncryptString(sBBPPrivKey, a.id);
            a.EncryptedALTPrivKey = BlockCypherFunctions.EncryptString(sAltPrivKey, a.id);
            a.EncryptedAssetPrivKey = BlockCypherFunctions.EncryptString(sAssetPrivKey, a.id);

            // Store the encrypted key somehow in the data
            
            a.CollateralALTAddress = ERCUtilities.GetPubKeyFromPrivKey(false, sAltPrivKey);
            a.CollateralBBPAddress = ERCUtilities.GetPubKeyFromPrivKey(false, sBBPPrivKey);
            a.CollateralAssetAddress = ERCUtilities.GetPubKeyFromPrivKey(false, sAssetPrivKey);

            a.Height = WebRPC.GetHeight(false);

            if (a.Action == "sell")
            {
            }
            else if (a.Action == "buy")
            {
            }
            else
            {
                throw new Exception("Unknown action.");
            }
            
            List<AtomicTransaction5> lAT = new List<AtomicTransaction5>();
            lAT.Add(a);
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(lAT);
            DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
            string sResult = Sz3(a);
            return sResult;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log(ex.Message + "\r\n" + ex.StackTrace);
            string sErr = "Unable to create::" + ex.Message;
            a.Error = sErr;
            return Sz3(a);
        }
    }

    private bool IsColored(string sAddress, string sSuffix)
    {
        sAddress = sAddress.ToUpper();
        sSuffix = sSuffix.ToUpper();
        bool fContains = sAddress.EndsWith(sSuffix);
        return fContains;
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [DisableRequestSizeLimit]
    [Route("api/AtomicTrading/TransmitIngateTransactionV2")]
    public async Task<string> TransmitIngateTransactionV2(
    [FromHeader] string EncryptedBBPKey, [FromHeader] string EncryptedDogeKey,
    [FromHeader] string Password, [FromHeader] string EncryptedAssetKey, [FromHeader] string AtomicTrade)
    {

        // If they have included a symbol that is valid, and an amount, and a valid alt address.
        // Step 1 - Send the amount from their alt to our alt address.
        // Step 2 - Send BBP from our MMZZ to their colored address.

        AtomicTransaction5 a = new AtomicTransaction5();

        try
        {
            iIterator++;
            string sAtomicTrade = BlockCypherFunctions.DecryptString(AtomicTrade, Password);
            a = Utils.Deserialize<AtomicTransaction5>(sAtomicTrade);
            a.id = BMSCommon.Common.UnixTimestamp().ToStr() + "-" + iIterator.ToStr();
            BMSCommon.Common.Log("v3.0::" + AtomicTrade);

            BMSCommon.Common.Log("Trade id " + a.id);

            string sRoom = a.SymbolSell + "/" + a.SymbolBuy;

            if (sRoom != "bbp/doge" && sRoom != "doge/bbp")
            {
                throw new Exception("Invalid room");
            }
            if (a.Quantity < .10)
            {
                throw new Exception("Ingate quantity too low.");
            }

            if (a.Action != "ingate" || a.Status != "ingate")
            {
                throw new Exception("Action is wrong.");
            }
            if (a.id.Length < 4)
            {
                throw new Exception("ID must be longer.");
            }
            string sBBPPrivKey = BlockCypherFunctions.DecryptString(EncryptedBBPKey, Password); // This is the signing coin
            string sAltPrivKey = BlockCypherFunctions.DecryptString(EncryptedDogeKey, Password); // This is the DOGE escrow
            string sAssetPrivKey = BlockCypherFunctions.DecryptString(EncryptedAssetKey, Password); // This is the colored coin

            a.EncryptedBBPPrivKey = BlockCypherFunctions.EncryptString(sAssetPrivKey, a.id);
            a.EncryptedALTPrivKey = BlockCypherFunctions.EncryptString(sAltPrivKey, a.id);

            a.CollateralBBPAddress = ERCUtilities.GetPubKeyFromPrivKey(false, sAssetPrivKey);
            string sDogeSenderPubKey = ERCUtilities.GetPubKeyFromPrivKey("dogenet", sAltPrivKey);
            a.CollateralALTAddress = sDogeSenderPubKey;
            a.Height = WebRPC.GetHeight(false);
            BMSCommon.Common.Log("INGATE::DOGE SENDER ADDRS=" + a.CollateralALTAddress + ", BBP SIGNER = " + a.CollateralBBPAddress);

            if (!IsColored(a.CollateralBBPAddress,"dgzz"))
            {
                throw new Exception("Destination must be colored.");
            }
            // STEP 1 - Send the DOGE from user -> Sanc

            string sDogeSeed = BMSCommon.Common.GetConfigKeyValue("dogesancseed");
            BBPKeyPair kpDogeSanc = await Ethereum.DeriveAltcoinKey("doge", sDogeSeed);
            string sMPrivK = Dogone.GetMasterAssetKey();
            string sMPubK = ERCUtilities.GetPubKeyFromPrivKey(false, sMPrivK);
            
            double nDogeBalance = GetDogeBalance(sDogeSenderPubKey);
            // Get BBP BALANCE, AND DOGE BALANCE FIRST
            double nBBPBalance = GetBBPBalance(sMPubK);
            BMSCommon.Common.Log("INGATE::DOGE_USER_BALANCE=" + nDogeBalance.ToStr() + ", BBP_MASTER_ASSET_BAL=" + nBBPBalance.ToStr());


            if (nDogeBalance < a.Quantity + .02)
            {
                throw new Exception("Doge balance too low.");
            }
            if (nBBPBalance < a.Quantity  + .02)
            {
                throw new Exception("INGATE_ERROR_01262025");
            }

            CryptoTransmissionResult ctr = await Dogone.SendDogeTx(a.Quantity, kpDogeSanc.PubKey, sAltPrivKey);
            a.CollateralALTAddress += kpDogeSanc.PubKey;
            a.CollateralTXID = ctr.TXID;
            if (ctr.Error != "")
            {
                a.Error = ctr.Error;
                throw new Exception(a.Error);
            }

            // Step 2 - Send the Colored Coin from Sanc -> User
            BMSCommon.Common.Log("INGATE->Colored BBP_ADDRESS=" + a.CollateralBBPAddress);

            DACResult d = await WebRPC.SendBBPOutsideChain(false, "atomictransaction", a.CollateralBBPAddress,
                sMPrivK, a.Quantity, "");
            a.Error = d.Error;
            a.ReturnTXID = d.TXID;
            BMSCommon.Common.Log("Ingate::Collateral BBP " + a.CollateralBBPAddress + " DOGE_IN_TXID " + a.CollateralTXID + ", RETURNTXID=" + a.ReturnTXID);

            List<AtomicTransaction5> lAT = new List<AtomicTransaction5>();
            lAT.Add(a);
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(lAT);
            DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
            // return the collateral TXID in the response.
            string sResult = Sz3(a);
            return sResult;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log(ex.Message + "\r\n" + ex.StackTrace);
            string sErr = "Unable to create::" + ex.Message;
            a.Error = sErr;
            return Sz3(a);
        }
    }

    public static string GetBurnAddress0(bool fTestNet)
    {
        // These are hardcoded in the biblepaycore wallet:
        string sBurnAddress = !fTestNet ? "B4T5ciTCkWauSqVAcVKy88ofjcSasUkSYU" : "yLKSrCjLQFsfVgX8RjdctZ797d54atPjnV";
        return sBurnAddress;
    }


    [HttpPost]
    [ResponseCache(NoStore = true, Duration = 0)]
    [DisableRequestSizeLimit]
    [Route("api/AtomicTrading/TransmitOutgateTransactionV2")]

    public async Task<string> TransmitOutgateTransactionV2(
        [FromHeader] string EncryptedBBPKey, [FromHeader] string EncryptedDogeKey,
        [FromHeader] string Password, [FromHeader] string EncryptedAssetKey, [FromHeader] string AtomicTrade)
    {

        // If they have included a symbol that is valid, and an amount, and a valid alt address.
        // Step 1 - Send the colored amount from their colored wallet to the BURN address.
        // Step 2 - Send DOGE from our Sancs' DOGE wallet to their internal DOGE wallet.
        AtomicTransaction5 a = new AtomicTransaction5();
        try
        {
            iIterator++;
            string sAtomicTrade = BlockCypherFunctions.DecryptString(AtomicTrade, Password);
            a = Utils.Deserialize<AtomicTransaction5>(sAtomicTrade);
            a.id = BMSCommon.Common.UnixTimestamp().ToStr() + "-" + iIterator.ToStr();
            BMSCommon.Common.Log("v3.0::" + AtomicTrade);
            string sRoom = a.SymbolSell + "/" + a.SymbolBuy;
            if (sRoom != "bbp/doge" && sRoom != "doge/bbp")
            {
                throw new Exception("Invalid room");
            }
            if (a.Quantity < .10)
            {
                throw new Exception("Outgate quantity too low.");
            }

            if (a.Action != "outgate" || a.Status != "outgate")
            {
                throw new Exception("Action is wrong.");
            }
            if (a.id.Length < 4)
            {
                throw new Exception("ID must be longer.");
            }
            string sBBPPrivKey = BlockCypherFunctions.DecryptString(EncryptedBBPKey, Password); // This is the signing coin
            string sAltPrivKey = BlockCypherFunctions.DecryptString(EncryptedDogeKey, Password); // This is the DOGE escrow
            string sAssetPrivKey = BlockCypherFunctions.DecryptString(EncryptedAssetKey, Password); // This is the colored coin
            a.EncryptedBBPPrivKey = BlockCypherFunctions.EncryptString(sAssetPrivKey, a.id);
            a.EncryptedALTPrivKey = BlockCypherFunctions.EncryptString(sAltPrivKey, a.id);
            a.CollateralBBPAddress = ERCUtilities.GetPubKeyFromPrivKey(false, sAssetPrivKey);
            string sDogeRecvrPubKey = ERCUtilities.GetPubKeyFromPrivKey("dogenet", sAltPrivKey);
            a.CollateralALTAddress = sDogeRecvrPubKey;
            a.Height = WebRPC.GetHeight(false);
            BMSCommon.Common.Log("OUTGATE::DOGE SENDER ADDRS=" + a.CollateralALTAddress + ", BBP SIGNER = " + a.CollateralBBPAddress);
            if (!IsColored(a.CollateralBBPAddress, "dgzz"))
            {
                throw new Exception("Sender address must be colored.");
            }
            // STEP 1 - Send the DOGE from Sanc -> User
            string sDogeSeed = BMSCommon.Common.GetConfigKeyValue("dogesancseed");

            BBPKeyPair kpDogeSanc = await Ethereum.DeriveAltcoinKey("doge", sDogeSeed);
            string sMPrivK = Dogone.GetMasterAssetKey();
            string sMPubK = ERCUtilities.GetPubKeyFromPrivKey(false, sMPrivK);
            double nDogeBalance = GetDogeBalance(kpDogeSanc.PubKey);
            // Get BBP BALANCE, AND DOGE BALANCE FIRST
            double nBBPBalance = GetBBPBalance(a.CollateralBBPAddress);
            BMSCommon.Common.Log("OUTGATE::SANC_DOGE_BALANCE=" + nDogeBalance.ToStr() + ", USER_ASSET_COLORED_PUBKEY=" + a.CollateralBBPAddress 
                + ", USER_ASSET_COLORED_BALANCE=" + nBBPBalance.ToStr());

            if (nBBPBalance < a.Quantity + .02)
            {
                throw new Exception("Asset Balance Too Low");
            }

            if (nDogeBalance < a.Quantity + .02)
            {
                throw new Exception("OUTGATE_ERROR_01262025");
            }

            CryptoTransmissionResult ctr = await Dogone.SendDogeTx(a.Quantity, sDogeRecvrPubKey, kpDogeSanc.PrivKey);
            a.CollateralALTAddress += kpDogeSanc.PubKey;
            a.CollateralTXID = ctr.TXID;
            if (ctr.Error != "")
            {
                a.Error = ctr.Error;
                throw new Exception(a.Error);
            }

            // Step 2 - Send the Colored Coin from User -> BURN
            BMSCommon.Common.Log("OUTGATE->Colored BBP_ADDRESS=" + a.CollateralBBPAddress);
            string sBurn = GetBurnAddress0(false);

            DACResult d = await WebRPC.SendBBPOutsideChain(false, "atomictransaction", sBurn, sAssetPrivKey , a.Quantity, "");
            a.Error = d.Error;
            a.ReturnTXID = d.TXID;
            BMSCommon.Common.Log("OUTGATE::Collateral BBP " + a.CollateralBBPAddress + " DOGE_IN_TXID " + a.CollateralTXID + ", RETURNTXID=" + a.ReturnTXID);

            List<AtomicTransaction5> lAT = new List<AtomicTransaction5>();
            lAT.Add(a);
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<AtomicTransaction5>(lAT);
            DACResult dac1 = await CoreController.InsertChainObjectsInternal(co01);
            string sResult = Sz3(a);
            return sResult;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log(ex.Message + "\r\n" + ex.StackTrace);
            string sErr = "Unable to create::" + ex.Message;
            a.Error = sErr;
            return Sz3(a);
        }
    }
    [HttpGet]
    [Route("api/AtomicTrading/DogeTest1")]
    public async Task<string> DogeTest1()
    {
        return "1";
    }
}

