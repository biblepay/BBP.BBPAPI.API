using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Mvc;
using static bbp.core.api.Controllers.CoreController;
using static BBPAPI.DB;
using static BMSCommon.Encryption;

namespace bbp.core.api.Controllers;

[ApiController]
public class NFTController
{
    public NFTController()
    {
    }


    
    [HttpPost]
    [Route("api/nft/GetNFTs")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<NFT> GetNFTs([FromHeader] string body)
    {
        List<NFT> n = WebRPC.GetNFTs(false);
        return n;
    }

    [HttpPost]
    [Route("api/nft/SaveNFT")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<DACResult> SaveNFT([FromHeader] string body)
    {
        NFT n = Utils.Deserialize<NFT>(body);

        string sToAddress = String.Empty;
        double nAmt = 0;

        if (n.Action.ToLower()=="buy")
        {
            sToAddress = n.Signer; // the prior owner;
                                   // transfer ownership to buyer
                                   // if this is a buy, we need to push the whole amount
            n.Signer = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(false, n._PrivKey);
            nAmt = n.BuyItNowAmount + 10;
        }
        else
        {
            // Just 1 to cover the tx fee.
            nAmt = 1;
            sToAddress = n.Signer;
        }
        n.Message = BMSCommon.Common.UnixTimestamp().ToStr();
        n.Signature = ERCUtilities.SignMessage(false, n._PrivKey, n.Message);
        string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(n);
        string sData1 = "<msg>" + n.Message + "</msg><signer>" + n.Signer + "</signer><sig>" + n.Signature + "</sig><key>"
            + n.id + "</key><value>" + sJson + "</value>";
        string sXML = "<sc><objtype>NFT</objtype><url>" + sData1 + "</url></sc>";

        DACResult d = await WebRPC.SendBBPOutsideChain(false, "nft", sToAddress, n._PrivKey, nAmt, sXML);
        return d;
    }
}
