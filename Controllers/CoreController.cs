using BBP.CORE.API;
using BBP.CORE.API.Database;
using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static BMSCommon.Model.BitcoinSyncModel;

namespace bbp.core.api.Controllers;

[ApiController]
public class CoreController 
{
    public CoreController()
    {
    }

    public static dynamic Des<T>(string json)
    {
        dynamic z = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        return z;
    }
    public static string Ser(object o)
    {
        string s = Newtonsoft.Json.JsonConvert.SerializeObject(o);
        return s;
    }


    [HttpPost]
    [Route("api/core/GetAltcoinKeyPair")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<Encryption.BBPKeyPair> GetAltcoinKeyPair([FromHeader] string body)
    {
        SigSigner s = Des<SigSigner>(body);
		Encryption.BBPKeyPair b = await Ethereum.DeriveAltcoinKey(s.Chain, s.PrivKey);
        return b;
	}

	[HttpPost]
	[Route("api/core/GetAltcoinBalance")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public async Task<string> GetAltcoinBalance([FromHeader] string body)
	{
		SigSigner s = Des<SigSigner>(body);
		Encryption.BBPKeyPair b = await Ethereum.DeriveAltcoinKey(s.Chain, s.PrivKey);
        double nAmt = await Ethereum.GetAltcoinBalance(s.Chain, b.PubKey);
		return Ser(nAmt);
	}


	[HttpPost]
    [Route("api/core/SendEmail")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public DACResult SendEmail([FromHeader] string body)
    {
        BMSCommon.Common.Log(body);
        BBPOutboundEmail r1 = Newtonsoft.Json.JsonConvert.DeserializeObject<BBPOutboundEmail>(body);
        DACResult r2 = Email.SendMail(r1);
        return r2;
    }


	[HttpPost]
    [Route("api/core/GetPriceQuote")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> GetPriceQuote([FromHeader] string body)
    {
        string r1 = Utils.Deserialize<string>(body);
        double dRes = await PricingService.GetPriceQuote(r1);
        string d0 = Newtonsoft.Json.JsonConvert.SerializeObject(dRes);
        return d0;
    }


    [HttpPost]
    [Route("api/core/GetGenesisBlock")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public DatabaseQuery GetGenesisBlock(DatabaseQuery q)
    {
        bool fOK = QuorumUtils.VerifyTemple(q.SanctuaryPrivateKey);
        if (!fOK)
        {
            q.Value = "501";
            return q;
        }
        // Must be a verified temple
        q.Value = SecureString.GetDBConfigurationKeyValue(q.Key);
        return q;
    }

    public static async Task<string> CallEndpoint(string sURL, string sJson)
    {
        HttpClient _Client = new HttpClient();
        TimeSpan ts0 = new TimeSpan(0, 10, 0, 0);
        _Client.Timeout = ts0;
        HttpContent httpContent = new StringContent(sJson, Encoding.UTF8, "application/json");
        ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
        var oInitialResponse = await _Client.PostAsync(sURL, httpContent);
        string sJsonResponse = await oInitialResponse.Content.ReadAsStringAsync();
        return sJsonResponse;
    }

    internal static async Task<string> ReturnStringUsingBodyPost(string sEndPoint, string sData)
    {
        HeaderPack h = new HeaderPack();
        h.listKeys.Add("body");
        h.listValues.Add(sData);
        string sResp2 = await PostToWebAPIEndpoint(sEndPoint, sData, h, "POST");
        return sResp2;
    }

    public async static Task<string> PostToWebAPIEndpoint(string sURL, string sBody, HeaderPack h, string sMethod)
    {
        try
        {
            HttpContent content = new StringContent(sBody);
            var httpClient = new System.Net.Http.HttpClient(new HttpClientHandler
            {
                UseProxy = false
            });

            using (httpClient)
            {
                using (var request = new HttpRequestMessage(new HttpMethod(sMethod), sURL))
                {
                    httpClient.Timeout = new System.TimeSpan(0, 5, 00);
                    int iLoc = 0;
                    if (h != null)
                    {
                        foreach (string sKey in h.listKeys)
                        {
                            string sValue = h.listValues[iLoc];
                            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(sKey, sValue);
                            iLoc++;
                        }
                    }

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = content;

                    // in case things break here, i just added this
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    // end of add

                    ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                    var oInitialResponse = await httpClient.PostAsync(sURL, content);
                    string sJsonResponse = await oInitialResponse.Content.ReadAsStringAsync();
                    return sJsonResponse;
                }
            }
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }


    public static async Task<DACResult> InsertChainObjectsInternal(List<ChainObject> l)
    {
        DACResult dr = new DACResult();
        if (l.Count < 1)
        {
            return dr;
        }
        User u1 = new User();
        u1.BBPPrivKeyMainNet = BMSCommon.Common.GetConfigKeyValue("sysuser");
        SanctuaryAuthority sa = new SanctuaryAuthority();
        sa.SanctuaryVotingAddressPrivateKey = BMSCommon.Common.GetConfigKeyValue("sancprivkey");
        QuorumUtils.HashChainObjects(l, u1, sa);
        for (int i = 0; i < l.Count; i++)
        {
            ChainObject co = l[i];
        }

        string url = "https://api.biblepay.org/api/QuorumController/InsertChainObjectsExternal";
        bool fDebugging = false;
        if (fDebugging)
        {
            url = "http://localhost:9000/api/QuorumController/InsertChainObjectsExternal";
        }
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(l);
        string sJsonResult = await CallEndpoint(url, json);
        DACResult dr1 = Utils.Deserialize<DACResult>(sJsonResult);
        return dr1;
    }

    public static async Task<DatabaseQuery> GetGenesisBlockRequest(DatabaseQuery q)
    {
        string url = "https://api.biblepay.org/api/core/GetGenesisBlock";
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(q);
        string sJsonResult = await CallEndpoint(url, json);
        DatabaseQuery dr1 = Utils.Deserialize<DatabaseQuery>(sJsonResult);
        return dr1;
    }
}

