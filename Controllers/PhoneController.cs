using BBP.CORE.API;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using BMSShared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Text;
using static bbp.core.api.Controllers.CoreController;

namespace bbp.core.api.Controllers;

[ApiController]
public class PhoneController
{
	

    [HttpPost]
    [Route("api/phone/BuyAndGetNewPhoneNumber")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string BuyAndGetNewPhoneNumber([FromHeader] string body)
    {
        PhoneRegionCountryAddress r1 = Utils.Deserialize<PhoneRegionCountryAddress>(body);
        string f = DB.PhoneProcs.BuyAndGetNewPhoneNumberInternal(r1).Result;
        string sResp = Utils.Serialize(f);
        return sResp;
    }

    [HttpPost]
    [HttpGet]
    [Route("api/phone/GetRoute")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetRoute([FromQuery] string callerid, [FromQuery] string destination)
    {
        PhoneCallerDestination dr1 = new PhoneCallerDestination();
        dr1.CallerID = callerid;
        dr1.Destination = destination;
        string r2 = DB.PhoneProcs.GetRoute(dr1);
        return r2;
    }


    [HttpPost]
    [Route("api/phone/GetPhoneUserRouting")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetPhoneUserRouting([FromHeader] string body)
    {
        PhoneCallerDestination r1 = Utils.Deserialize<PhoneCallerDestination>(body);
        PhoneUser r2 = DB.PhoneProcs.GetPhoneUserRouting(r1);
        string sResp = Utils.Serialize(r2);
        return sResp;
    }


    [HttpPost]
    [Route("api/phone/GetRegions")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<ComboBoxItem> GetRegions([FromHeader] string body)
    {
        string r1 = Utils.Deserialize<string>(body);
        List<ComboBoxItem> r2 = DB.PhoneProcs.GetRegionsInternal(r1).Result;
        return r2;
    }

    [HttpPost]
    [Route("api/phone/GetCallHistoryReport")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<PhoneCallHistory> GetCallHistoryReport([FromHeader] string body)
    {
        long r1 = Utils.Deserialize<long>(body);
        List<PhoneCallHistory> l = DB.PhoneProcs.GetCallHistoryReport(r1);
        return l;
    }


    [HttpPost]
    [Route("api/phone/SendSMS")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string SendSMS([FromHeader] string body)
    {
        SMSMessage r1 = Des<SMSMessage>(body);
        long f = DB.PhoneProcs.SendSMS(r1).Result;
        string sResp = Utils.Serialize(f);
        return sResp;
    }


    [HttpPost]
    [Route("api/phone/AddNewPhoneUser")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<String> AddNewPhoneUser([FromHeader] string body)
    {
        long d = await DB.PhoneProcs.AddNewPhoneUserInternal(Des<BBPAddressKey>(body));
        return Ser(d);
    }

    [HttpPost]
    [Route("api/phone/ProcessVoiceMailLD")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public String ProcessVoiceMailLD([FromHeader] string body)
    {
        PhoneCallerDestination r1 = Utils.Deserialize<PhoneCallerDestination>(body);
        string s = DB.PhoneProcs.ProcessVoicemail(r1);
        return s;
    }

    [HttpPost]
    [Route("api/phone/GetPhoneUserNameBasedOnRecordCount")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string GetPhoneUserNameBasedOnRecordCount([FromHeader] string body)
    {
        User r1 = Utils.Deserialize<User>(body);
        string sResp2 = DB.PhoneProcs.GetPhoneUserNameBasedOnRecordCount(r1);
        string sResp = Utils.Serialize(sResp2);
        return sResp;
    }

    [HttpPost]
    [Route("api/phone/GetPhoneUser")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public PhoneUser GetPhoneUser([FromHeader] string body)
    {
        User r1 = Utils.Deserialize<User>(body);
        PhoneUser pu = DB.PhoneProcs.GetPhoneUser(r1);
        return pu;
    }


    [HttpPost]
    [Route("api/phone/SetPhoneUserPhoneNumber")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> SetPhoneUserPhoneNumber([FromHeader] string body)
    {
        NewPhoneUser r1 = Utils.Deserialize<NewPhoneUser>(body);
        bool bResp2 = await DB.PhoneProcs.SetPhoneUserPhoneNumber(r1);
        string sResp = Utils.Serialize(bResp2);
        return sResp;
    }


    [HttpPost]
    [Route("api/phone/SetVoiceMailGreeting")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public string SetVoiceMailGreeting([FromHeader] string body)
    {
        VoiceGreeting vmg = Utils.Deserialize<VoiceGreeting>(body);
        bool fResp = false;
        string sResp = Utils.Serialize(fResp);
        return sResp;
    }

    [HttpPost]
    [Route("api/phone/GetRatesReport")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<PhoneRate> GetRatesReport([FromHeader] string body)
    {
        double r1 = Utils.Deserialize<double>(body);
        DataTable lpr = DB.PhoneProcs.GetRatesReport(r1);
        List<PhoneRate> l0 = GenericTypeManipulation.ConvertDataTable<PhoneRate>(lpr);
        return l0;
    }

    
    [HttpPost]
    [Route("api/phone/InsertPhoneUser")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<String> InsertPhoneUser([FromHeader] string body)
    {
        NewPhoneUser r1 = Utils.Deserialize<NewPhoneUser>(body);
        bool b = await DB.PhoneProcs.InsertPhoneUser(r1);
        string sResp = Utils.Serialize(b);
        return sResp;
    }

    [HttpPost]
    [Route("api/phone/ProcessWebHookLongDistance")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public String ProcessWebHookLongDistance([FromHeader] string body)
    {
        DB.PhoneProcs.ProcessWebHook(body);
        return "200";
    }

    [HttpPost]
    [Route("api/phone/SetPhoneRulesCreated")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> SetPhoneRulesCreated([FromHeader] string body)
    {
        long r1 = Utils.Deserialize<long>(body);
        bool r2 = await DB.PhoneProcs.SetPhoneRulesCreated(r1);
        string sResp = Utils.Serialize(r2);
        return sResp;
    }
}
