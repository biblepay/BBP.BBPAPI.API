using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Mvc;
using NBitcoin.Crypto;
using System.Data;
using static bbp.core.api.Controllers.CoreController;
using static BBPAPI.DB;
using static BMSCommon.Encryption;
using static BMSCommon.Model.BitcoinSyncModel;

namespace bbp.core.api.Controllers;

[ApiController]
public class RepositoryController
{

    [HttpPost]
    [Route("api/Repository/StoreData")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public bool StoreData([FromHeader] string body)
    {
        DatabaseQuery r1 = Utils.Deserialize<DatabaseQuery>(body);
        bool r3 = QuorumUtils.StoreDataByType(r1);
        return r3;
    }


    [HttpPost]
    [Route("api/repository/GetDatabaseObjects")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public dynamic GetDatabaseObjects([FromHeader] string body)
    {
        DatabaseQuery r1 = Utils.Deserialize<DatabaseQuery>(body);
        dynamic r2 = QuorumUtils.GetDatabaseObjectsByType(r1);
        return r2;
    }

	[HttpPost]
	[Route("api/repository/GetChainDatabaseObjects")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public dynamic GetChainDatabaseObjects([FromHeader] string body)
	{
		DatabaseQuery r1 = Utils.Deserialize<DatabaseQuery>(body);
		dynamic r2 = QuorumUtils.GetDatabaseObjectsByType(r1);
        return r2;
	}


	[HttpPost]
	[Route("api/repository/UpsertPasswordManagerRecord")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public string UpsertPasswordManagerRecord([FromHeader] string body)
	{
		BMSCommon.Common.Log(body);
		PasswordManagerRecord r1 = Utils.Deserialize<PasswordManagerRecord>(body);
        string sResp = Newtonsoft.Json.JsonConvert.SerializeObject(r1);
		return sResp;
	}


	[HttpPost]
    [Route("api/Repository/SaveTimeLine")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> SaveTimeLine([FromHeader] string body)
    {
        return Ser(await DB.OperationProcs.SaveTimeline(Des<Timeline>(body)));
    }


    [HttpPost]
    [Route("api/repository/GetWellsReport")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<Well> GetWellsReport([FromHeader] string body)
    {
        List<Well> l = OperationProcs.GetWellsReport();
        return l;
    }


	[HttpPost]
	[Route("api/repository/GetWellsPinsReport")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public List<Pin> GetWellsPinsReport([FromHeader] string body)
	{
        List<Pin> r2 = OperationProcs.GetWellsPinsReport();
        return r2;
	}


	[HttpPost]
    [Route("api/Repository/GetTimeLine")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<Timeline> GetTimeLine([FromHeader] string body)
    {
        GetBusinessObject bo = Des<GetBusinessObject>(body);
        List<Timeline> l = DB.OperationProcs.GetTimeline(bo.TestNet, bo.ParentID);
        return l;
    }

    [HttpGet]
	[Route("api/Repository/GetTestVideos")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<string> GetTestVideos()
	{
        // Scan rumble for videos
        string sURL = "https://rumble.com/c/HGTV?page=5";
        BBPWebClient b = new BBPWebClient();
        string data = b.DownloadString(sURL);
        // split it by img
        List<VideoNew> lvn = new List<VideoNew>();
        string[] vData = data.Split("thumbnail__image");
        for (int i = 0; i < vData.Length; i++)
        {
            string src = BMSCommon.Common.ExtractXML(vData[i], "src=\"", "\"");
			string alt = BMSCommon.Common.ExtractXML(vData[i], "alt=\"", "\"");
            string href = BMSCommon.Common.ExtractXML(vData[i], "href=\"", "\"");
            string sKey = BMSCommon.Encryption.GetSha256HashI(alt);
            // Add a new video object into BMS
            VideoNew vn = new VideoNew();
            vn.id = Guid.NewGuid().ToString();
            //string sInsert = "upsert into videonew (id,title,url,added,cover) values ('" + sKey + "',@alt, @url,now(),@cover);";
            string sCode = BMSCommon.Common.ExtractXML(href, "/", "-");
            if (sCode.Length > 6)
            {
				BBPWebClient d1 = new BBPWebClient();
				string monetizedURL = "https://rumble.com" + href;
				string sVideoData = d1.DownloadString(monetizedURL);
                string sEmbed = BMSCommon.Common.ExtractXML(sVideoData, "embedUrl\":\"", "\"");
                vn.id = sKey;
                vn.title = alt;
                vn.url = sEmbed;
                vn.cover = src;
                vn.Added = DateTime.Now.ToStr();
                lvn.Add(vn);

            }

		}
        List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<VideoNew>(lvn);
        await CoreController.InsertChainObjectsInternal(co01);
        return "1";
	}

	[HttpPost]
    [Route("api/Repository/GetVerseMemorizers")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<VerseMemorizer> GetVerseMemorizers([FromHeader] string body)
    {
        List<VerseMemorizer> l = DB.OperationProcs.GetVerseMemorizers();
        return l;
    }

	[HttpPost]
	[Route("api/Repository/GetMenu")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public List<BBPMenu> GetMenu([FromHeader] string body)
	{
		List<BBPMenu> l = DB.OperationProcs.GetMenu();
        return l;
	}


	[HttpPost]
	[Route("api/Repository/GetVideoNew")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public List<VideoNew> GetVideoNew([FromHeader] string body)
	{
		List<VideoNew> l = QuorumUtils.GetDatabaseObjects<VideoNew>("videonew");
        return l;
	}


	[HttpPost]
    [Route("api/User/GetUserCountByEmail")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetUserCountByEmail([FromHeader] string body)
    {
        string sEmail = Des<string>(body);
        double l = OperationProcs.GetUserCountByEmail(sEmail);
        return Ser(l);
    }
    
    [HttpPost]
    [Route("api/User/PersistUser")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> PersistUser([FromHeader] string body)
    {
        User u = Des<User>(body);
        bool f = await OperationProcs.PersistUser(u);
        return Ser(f);
    }

    [HttpPost]
    [Route("api/Proposal/gobject_serialize")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> gobject_serialize([FromHeader] string body)
    {
        Proposal u = Des<Proposal>(body);
        bool f = await BBPAPI.GovernanceProposal.gobject_serialize(u);
        return Ser(f);
    }

	[HttpPost]
	[Route("api/User/SetPresence")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<string> SetPresence([FromHeader] string body)
	{
		Presence p = Des<Presence>(body);
        List<User> usr = QuorumUtils.GetBBPDatabaseObjects<User>();
        usr = usr.Where(a => a.id == p.UserId).ToList();
        if (usr.Count == 1)
        {
            usr[0].LastSeen = DateTime.Now;
            List<ChainObject> co01 = await QuorumUtils.ConvertObjectsToChainObjects<User>(usr);
            await CoreController.InsertChainObjectsInternal(co01);
        }
		return Ser(true);
	}


	[HttpPost]
	[Route("api/NBitcoin/GetPublicKeyFromPrivateKey")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public string GetPublicKeyFromPrivateKey([FromHeader] string body)
	{
      	SigSigner sigSignerPriv = Des<SigSigner>(body);
		sigSignerPriv.PubKey = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(sigSignerPriv.TestNet, sigSignerPriv.PrivKey);
    	return Ser(sigSignerPriv);
	}

	[HttpPost]
	[Route("api/NBitcoin/SignMessage")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<string> SignMessage([FromHeader] string body)
	{
		SigSigner s = Des<SigSigner>(body);
        s.Signature = ERCUtilities.SignMessage(s.TestNet, s.PrivKey, s.Message);
		return Ser(s);
	}

	[HttpPost]
	[Route("api/NBitcoin/VerifySignature")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public string VerifySignature([FromHeader] string body)
	{
		SigSigner s = Des<SigSigner>(body);
        s.Valid = ERCUtilities.VerifySignature(s.TestNet, s.PubKey, s.Message, s.Signature);
        return Ser(s);
	}

	[HttpPost]
	[Route("api/NBitcoin/QueryAddressBalance")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public string QueryAddressBalance([FromHeader] string body)
	{
		SigSigner s = Des<SigSigner>(body);
        double nBal = WebRPC.QueryAddressBalanceNewMethod(s.TestNet, s.PubKey);
     	return Ser(nBal);
	}


	[HttpPost]
	[Route("api/NBitcoin/DeriveKey")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<BBPKeyPair> DeriveKey([FromHeader] string body)
	{
		SigSigner s = Des<SigSigner>(body);
        BBPKeyPair p = ERCUtilities.DeriveKey(s.TestNet, s.Message);
        return p;
	}


    [HttpPost]
    [Route("api/Repository/GetChats")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetChats([FromHeader] string body)
    {
        bool f = Des<bool>(body);
        return Ser(f);
    }

    [HttpPost]
    [Route("api/Repository/GetNotifications")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetNotifications([FromHeader] string body)
    {
        GetBusinessObject f = Des<GetBusinessObject>(body);
        List<ChatNotification> l = DB.OperationProcs.GetNotifications(f.TestNet, f.ParentID);
        return Ser(l);
    }

    [HttpPost]
    [Route("api/Repository/StoreAttachment")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> StoreAttachment([FromHeader] string body)
    {
        Attachment f = Des<Attachment>(body);
        bool l = await DB.OperationProcs.StoreAttachment(f);
        return Ser(l);
    }
    [HttpPost]
    [Route("api/Repository/UpdateUserEmailAddressAsVerified")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> UpdateUserEmailAddressAsVerified([FromHeader] string body)
    {
        string f = Des<string>(body);
        bool l = await DB.OperationProcs.UpdateUserEmailAddressVerified(f);
        return Ser(l);
    }

    [HttpPost]
    [Route("api/Repository/InsertEmailAccount")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string InsertEmailAccount([FromHeader] string body)
    {
        EmailAccount f = Des<EmailAccount>(body);
        bool l = false;
        return Ser(l);
    }

    [HttpPost]
    [Route("api/Repository/GetArticles")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<List<Article>> GetArticles([FromHeader] string body)
    {
        string f = Des<string>(body);
        var v = DB.OperationProcs.GetArticles(f);
        return v;
    }


    [HttpPost]
    [Route("api/Repository/GetEmailAccounts")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetEmailAccounts([FromHeader] string body)
    {
        string s = Des<string>(body);
        List<EmailAccount> l = new List<EmailAccount>();
        return Ser(l);
    }

	[HttpPost]
	[Route("api/user/GetUserByNickName")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public User GetUserByNickName([FromHeader] string body)
	{
		BearerToken bt = Newtonsoft.Json.JsonConvert.DeserializeObject<BearerToken>(body);
        string sPubKey = NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(false, bt.PrivateKey);
        if (sPubKey == String.Empty || sPubKey == null)
        {
            sPubKey = "9999";
        }
        if (string.IsNullOrEmpty(bt.HexGuid))
        {
            bt.HexGuid = "9999";
        }
        List<User> u = QuorumUtils.GetBBPDatabaseObjects<User>();
        u = u.Where(a => (a.PasswordHash == bt.PasswordHash && a.Email2 == bt.EmailAddress) || a.Email2 == sPubKey || a.id == bt.HexGuid).ToList();
        return u[0];
	}

	[HttpPost]
	[Route("api/user/GetUserByNickNameOrEmail")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public User GetUserByNickNameOrEmail([FromHeader] string body)
	{
		BearerToken bt = Utils.Deserialize<BearerToken>(body);
        List<User> u = QuorumUtils.GetBBPDatabaseObjects<User>();
        u = u.Where(a => (a.NickName.ToLower() == bt.NickName.ToLower()) || a.Email2.ToLower() == bt.EmailAddress.ToLower()).ToList();
        if (u.Count == 0)
        {
            return null;
        }
        return u[0];
	}

}
