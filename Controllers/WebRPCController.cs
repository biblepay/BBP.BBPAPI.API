using BBPAPI;
using BMSCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using static BBPAPI.DB;
using static BMSCommon.Model.BitcoinSyncModel;

namespace bbp.core.api.Controllers;

public static class Utils
{
    public static T Deserialize<T>(string sBody)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        var o = JsonConvert.DeserializeObject<T>(sBody, settings);
        return o;
    }

    public static string Serialize(object o)
    {
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(o);
        return sResp;
    }
    public static string GetMimeType(string fileName)
    {
        string contentType;
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
        string sCT = contentType ?? "application/octet-stream";
        if (fileName.Contains("nft/"))
        {
            sCT = "application/json";
        }
        if (fileName.Contains("png/"))
        {
            sCT = "image/x-png";
        }
        return sCT;
    }

}


[ApiController]
[Authorize]
public class WebRPCController
{
    public WebRPCController()
    {
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("api/BMS/RDPAccountCreationRequest")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<RDPAccount> RDPAccountCreationRequest([FromHeader] string body)
    {
        RDPAccount r1 = Utils.Deserialize<RDPAccount>(body);
        if (r1 == null)
        {
            r1.SearchBBPAddress = "509";
            return r1;
        }
        try
        {
            string url = "http://sanc4.biblepay.org:9001/api/CommandCenter/Ziti/ScriptZitiServerSideUser";
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(r1);
            string sJsonResult = await CoreController.ReturnStringUsingBodyPost(url, json);
            r1 = Utils.Deserialize<RDPAccount>(sJsonResult);
            await OperationProcs.UpsertRDPAccount(r1);
            return r1;
        }
        catch (Exception ex)
        {
            r1.SearchBBPAddress = ex.Message;
            BMSCommon.Common.Log("Unable to create ziti " + ex.Message);
            return r1;
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("api/BMS/RDPPolicyChangeRequest")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> RDPPolicyChangeRequest([FromHeader] string body)
    {
        RDPAccount r1 = Utils.Deserialize<RDPAccount>(body);
        if (r1 == null)
        {
            return "509";
        }
        try
        {
            RDPAccount rFrom = OperationProcs.GetRDPAccount(r1, "");
            if (String.IsNullOrEmpty(rFrom.PublicKey))
            {
                return "510";
            }
            string sDataOut2 = string.Empty;
            string url = "http://sanc4.biblepay.org:9001/api/CommandCenter/Ziti/ScriptZitiPolicy";
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(r1);
            string sJsonResult = await CoreController.CallEndpoint(url, json);
            return sJsonResult;
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("Unable to create ziti " + ex.Message);
            return "511";
        }
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/SendMoney")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<DACResult> SendMoney([FromHeader] string body)
    {
        SendMoneyRequest r1 = Utils.Deserialize<SendMoneyRequest>(body);
        DACResult r2 = await Sanctuary.SendMoney(r1);
        return r2;
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/SendRawTx")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> SendRawTx([FromHeader] string body)
    {
        BBPNetHex r1 = Utils.Deserialize<BBPNetHex>(body);
        DACResult r2 = await WebRPC.SendRawTx(r1.TestNet, r1.Hex, String.Empty);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r2);
        return sResp;
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GObjectSubmit")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> GObjectSubmit([FromHeader] string body)
    {
        Proposal r1 = Utils.Deserialize<Proposal>(body);
        bool r3 = await GovernanceProposal.gobject_submit(r1);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r3);
        return sResp;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GObjectPrepare")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> GObjectPrepare([FromHeader] string body)
    {
        Proposal r1 = Utils.Deserialize<Proposal>(body);
        bool r3 = await GovernanceProposal.gobject_prepare(r1);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r3);
        return sResp;
    }



    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GetMasternodeList")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetMasternodeList([FromHeader] string body)
    {
        bool r1 = Utils.Deserialize<bool>(body);
        List<MasternodeListItem> r2 = WebRPC.GetMasternodeList(r1);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r2);
        return sResp;
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GetAddressUTXOs")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetAddressUTXOs([FromHeader] string body)
    {
        BBPNetAddress r1 = Utils.Deserialize<BBPNetAddress>(body);
        string r2 = WebRPC.GetAddressUTXOs(r1.TestNet, r1.Address);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r2);
        return sResp;
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GetSupply")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetSupply([FromHeader] string body)
    {
        bool fTestnet = Utils.Deserialize<bool>(body);
        SupplyType r2 = Sanctuary.GetSupply(fTestnet);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r2);
        return sResp;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/ValidateBBPAddress")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string ValidateBBPAddress([FromHeader] string body)
    {
        BBPNetAddress f1 = Utils.Deserialize<BBPNetAddress>(body);
        bool fValid = Sanctuary.ValidateBiblePayAddress(f1.TestNet, f1.Address);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(fValid);
        return sResp;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("api/webrpc/GetBlock")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public BitcoinSyncBlock GetBlock([FromHeader] string body)
    {
        BBPNetHeight f1 = Utils.Deserialize<BBPNetHeight>(body);
        BitcoinSyncBlock f = WebRPC.GetBlock(f1.TestNet, f1.Height);
        return f;
    }
}
