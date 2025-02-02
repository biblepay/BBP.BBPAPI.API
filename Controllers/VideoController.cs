using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Mvc;
using static bbp.core.api.Controllers.CoreController;
using static BBPAPI.DB;
using static BMSCommon.Encryption;

namespace bbp.core.api.Controllers;

[ApiController]
public class VideoController 
{
    public VideoController()
    {
    }


    [HttpPost]
    [Route("api/video/SaveVideo")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> SaveVideo([FromHeader] string body)
    {
        Video v = Des<Video>(body);
        bool f1 = await BBPAPI.DB.OperationProcs.SaveVideo(v);
        return Ser(f1);
    }

    [HttpPost]
    [Route("api/video/GetVideos")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<VideoNew> GetVideos([FromHeader] string body)
    {
        GetBusinessObject bo = Des<GetBusinessObject>(body);
        List<VideoNew> f1 = BBPAPI.DB.OperationProcs.GetVideos(bo.TestNet, bo.ParentID);
		return f1;
    }

    [HttpPost]
    [Route("api/video/SaveExtension1")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<bool> SaveExtension1([FromHeader] string body)
    {
        ExtensionObject bo = Des<ExtensionObject>(body);
        if (bo.password != BMSCommon.Common.GetConfigKeyValue("SECRET_PIN"))
        {
            throw new Exception("401");
        }

        bool f = await QuorumUtils.StoreDataByType3<ExtensionObject>(bo);
        return f;
    }



    [RequestSizeLimit(123000000)]
	[HttpPost]
	[Route("api/Video/PutExtension")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public static async Task<bool> PutExtension(ExtensionObject bo)
	{
        if (bo.password != BMSCommon.Common.GetConfigKeyValue("SECRET_PIN"))
        {
            throw new Exception("401");
        }
        bool f = await QuorumUtils.StoreDataByType3<ExtensionObject>(bo);
		return f;
	}

	[HttpPost]
	[Route("api/video/GetExtension")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public ExtensionObject GetExtension([FromHeader] string body)
	{
		ExtensionObject bo = Des<ExtensionObject>(body);
        List<ExtensionObject> l = OperationProcs.RetrieveExtensions();
		l = l.Where(a => a.id == bo.id).ToList();
		if (l.Count < 1)
		{
			return null;
		}
		else
		{
			return l[0];
		}
	}

    [HttpPost]
	[Route("api/video/GetExtensions")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public List<ExtensionObject> GetExtensions([FromHeader] string body)
	{
        List<ExtensionObject> l = OperationProcs.RetrieveExtensions();
		return l;
	}

	[HttpPost]
	[Route("api/video/GetRDPConnections")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public List<RDPConnection> GetRDPConnections([FromHeader] string body)
	{
        RDPConnection r = Des<RDPConnection>(body);
        List<RDPConnection> l = OperationProcs.GetRDPConnections(r);
		return l;
	}

	[HttpPost]
	[Route("api/video/SaveRDPConnection")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<RDPAccount> SaveRDPConnection([FromHeader] string body)
	{
		RDPConnection r = Des<RDPConnection>(body);
		bool f = await  OperationProcs.InsertRDPConnection(r);
		if (r.Direction.ToStr().ToLower() == "outbound" && r.Nickname != "deleted")
		{
			RDPAccount rdpDummy = new RDPAccount();
			BMSCommon.Common.Log("saving rdp 1");
			RDPAccount rTo = OperationProcs.GetRDPAccount(rdpDummy, r.Address);
			RDPAccount rFrom = new RDPAccount();
			rFrom.id = r.id;
            string url = "http://sanc4.biblepay.org:9001/api/CommandCenter/Ziti/ScriptZitiPolicy";
			RDPAccountPack rap = new RDPAccountPack();
			rap.DEST = rTo;
			rap.SOURCE = rFrom;
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(rap);
            string sJsonResult = await CoreController.ReturnStringUsingBodyPost(url, json);
			RDPAccount rdpa = Utils.Deserialize<RDPAccount>(sJsonResult);
			return rdpa;
		}
		RDPAccount rpa = new RDPAccount();
		return rpa;
	}

	[HttpPost]
	[Route("api/video/GetRDPAccount")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<RDPAccount> GetRDPAccount([FromHeader] string body)
	{
		RDPAccount r = Des<RDPAccount>(body);
		if (String.IsNullOrEmpty(r.id))
		{
			throw new Exception("Empty guid");
		}
		RDPAccount rdpDummy = new RDPAccount();
		rdpDummy.id = r.id;
		RDPAccount r0 = OperationProcs.GetRDPAccount(rdpDummy,string.Empty);
		if (r0 == null || String.IsNullOrEmpty(r0.PrivateKey))
		{
			// never existed, so derive key first and upsert
			r0 = new RDPAccount();
			BBPKeyPair kpRDP = ERCUtilities.DeriveKey(false, r.id);
			r0.id = r.id;
			r0.PublicKey = kpRDP.PubKey;
			r0.PrivateKey = kpRDP.PrivKey;
			r0.Added = DateTime.Now;
			await OperationProcs.UpsertRDPAccount(r0);
			r0 = OperationProcs.GetRDPAccount(r0,"");
		}
		return r0;
	}
}
