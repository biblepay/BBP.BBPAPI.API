using BBP.CORE.API.Service;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using static bbp.core.api.Controllers.CoreController;
using static BMSShared.GenericTypeManipulation;

namespace bbp.core.api.Controllers;

[ApiController]
public class PinController 
{
    public PinController()
    {
    }


    [HttpPost]
    [Route("api/pin/TestUpload")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task< string> TestUpload([FromHeader] string body)
    {
        string sSc = "d:\\Videos\\1.mp4";
        string sDest = "BB2BwSbDCqCqNsfc7FgWFJn4sRgnUt4tsM/1.mp4";
        await StorjIO.StorjUpload(sSc, sDest, "_INTERNAL_");
        string sWriteDest = "d:\\videos\\2.mp4";
        await StorjIO.StorjDownloadLg(sDest, sWriteDest);
        return "1";
    }


    [HttpPost]
    [Route("api/pin/GetPinsByHash")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<Pin> GetPinsByHash([FromHeader] string body)
    {
        HashPath v = Des<HashPath>(body);
        List<Pin> f1 = PinLogic.GetPinsByHash(v.Hash, v.Path);
        return f1;
    }

    [HttpPost]
    [Route("api/pin/GetPinsByUserId")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public List<Pin> GetPinsByUserId([FromHeader] string body)
    {
        string v = Des<HashPath>(body);
        List<Pin> f1 = PinLogic.GetPinsByUserID(v);
        return f1;
    }

    [HttpPost]
    [Route("api/pin/DbSavePin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> DbSavePin([FromHeader] string body)
    {
        Pin v = Des<Pin>(body);
        bool f1 = await PinLogic.DbSavePin(v);
        return Ser(f1);
    }

    [HttpPost]
    [Route("api/pin/UploadFileRetired")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> UploadFileRetired([FromHeader] string body)
    {
        UploadFileObject v = Des<UploadFileObject>(body);
        string sTempPath = System.IO.Path.GetTempPath();
        string sTempFN = System.Guid.NewGuid().ToString() + ".dat";
        string sFullPath = Path.Combine(sTempPath, sTempFN);
        System.IO.File.WriteAllBytes(sFullPath, v.FileBytes);
        string url = await StorjIO.StorjUpload(sFullPath, v.StorjDestinationPath, v.OverriddenBBPPrivateKey);
        UploadFileResult r1 = new UploadFileResult();
        r1.URL = url;
        if (System.IO.File.Exists(sFullPath))
        {
            System.IO.File.Delete(sFullPath);
        }
        return Ser(r1);
    }


    [Route("api/pin/getnft/{filename}")]
    [HttpGet]
    [ResponseCache(Duration = 86400 * 7, Location = ResponseCacheLocation.Any, NoStore = false)]

    public async Task<IActionResult> getnft(string filename)
    {
        // Use storj instead
        string sFileName = "BFjZ9eMmjCNZCBrtvBZSYqxvwhSPwxLBCT/nft/" + filename;
        string sNFT = HexadecimalEncoding.StringToHex(sFileName);
        return await getdata(sNFT);
	}

	[Route("api/getpng/{filename}")]
	[HttpGet]
	[ResponseCache(Duration = 86400 * 7, Location = ResponseCacheLocation.Any, NoStore = false)]

	public async Task<IActionResult> getpng(string filename)
	{
		// Use storj 
		string sFileName = "BFjZ9eMmjCNZCBrtvBZSYqxvwhSPwxLBCT/png/" + filename;
		string sNFT = HexadecimalEncoding.StringToHex(sFileName);
		return await getdata(sNFT);
	}

	[Route("api/pin/getdata/{filename}")]
    [HttpGet]
	[ResponseCache(Duration = 86400*7, Location = ResponseCacheLocation.Any, NoStore = false)]

	public async Task<IActionResult> getdata(string filename)
    {
        // Use storj instead
        string sourcepath = Encryption.FromHexString(filename);
		string sTempDir = System.IO.Path.GetTempPath();
		string sTempFN = Encryption.GetSha256HashI(sourcepath) + ".dat";
        string sTP = Path.Combine(sTempDir, sTempFN);
		FileInfo fi = new System.IO.FileInfo(sTP);
        if (fi.Exists == false)
        {
            bool fSucc = await StorjIO.StorjDownloadLg(sourcepath, sTP);
        }
		fi = new System.IO.FileInfo(sTP);
        if (fi.Exists)
        {
            string sMime = Utils.GetMimeType(sourcepath);
            var stream = new FileStream(sTP, FileMode.Open, FileAccess.Read);
            return new InlineFileStreamResult(stream, sMime)
            {
                FileDownloadName = filename
            };
        }
        else
        {
            return null;
        }
	}


	[Route("api/pin/bbpingress")]
    [HttpPost]
    [RequestSizeLimit(455000000)]
    [RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue)]
    public async Task<string> Index(List<IFormFile> file, [FromHeader] string key, [FromHeader] string url)
    {
        UnchainedReply u = new UnchainedReply();
        try
        {
            double nBal = 0;
            string sDestinationURL = string.Empty;
            string sSaveURL = String.Empty;
            string sDestFolder = String.Empty;
            string sPubKey = ERCUtilities.GetPubKeyFromPrivKey(false,key);
            nBal = WebRPC.QueryAddressBalanceCached(false, sPubKey, 60);
            if (nBal < 100)
            {
                throw new Exception("BBP Balance too low [" + nBal.ToString() + "].");
            }
            sDestinationURL = sPubKey + "/" + url;
            if (url == String.Empty)
            {
                throw new Exception("Header [url] is empty.");
            }

            string sTempDir = System.IO.Path.GetTempPath();
            string sTempFN = Guid.NewGuid().ToString() + ".dat";
            string sFullDest = Path.Combine(sTempDir, sTempFN);
            if (sFullDest.Contains("..") || sDestinationURL.Contains(".."))
            {
                throw new Exception("IO Corruption error 03232022::" + sFullDest + "::" + sDestinationURL);
            }

            // Uploads into ingress
            if (file.Count == 0)
            {
                u.Error = "You must post a file.";
                u.Result = -1;
                u.URL = String.Empty;
                string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(u);
                return Ser(u);
            }

            var postedFile = file[0];
            BMSCommon.Common.Log("Ingress::Key = " + key.ToString() + ",URL = " + sDestinationURL + ", DestFiles=" + sFullDest);

            if (true)
            {
                // Write the temp file to the temp dir
                var filePaths = new List<string>();
                var filePath = Path.GetTempFileName();
                filePaths.Add(filePath);
                using (var stream = new FileStream(sFullDest, System.IO.FileMode.Create))
                {
                    await postedFile.CopyToAsync(stream);
                }

                System.IO.FileInfo fi = new FileInfo(sFullDest);
                BMSCommon.Common.Log("BBPIngress_2::Writing " + sFullDest + ", sz = " + fi.Length.ToString());
                if (fi.Length > 0)
                {
                    // Store the file in Storj:
                    UploadFileObject ufo = new UploadFileObject();
                    ufo.SourceFilePath = sFullDest;
                    ufo.StorjDestinationPath = sDestinationURL;
                    ufo.OverriddenBBPPrivateKey = key;
                    string sURLResult = await StorjIO.StorjUpload(ufo.SourceFilePath, ufo.StorjDestinationPath, key);
                    u.URL = sURLResult;
                }
            }
            u.version = 1.4;
            return Ser(u);
        }
        catch (Exception ex)
        {
            BMSCommon.Common.Log("BBPIngress:Bad file post error::" + ex.Message);
            u.Error = "Ingress::BadFileError::" + ex.Message;
            return Ser(u);
        }
    }


    private bool ExecuteAction(string sAction, string sFullPath)
    {
        BMSCommon.Common.Log("Executing " + sAction);
        if (sAction == "UPGRADE_API")
        {
            // unzip it into the api directory; then the user has to restart the api
            string sDestDir = "/bbpcoreapi/publish/";
            System.IO.File.Copy(sFullPath, Path.Combine(sDestDir, "bbpcoreapi.zip"),true);
            BMSCommon.Common.Log("Done unzipping");
        }
        else if (sAction == "UPGRADE_WEB")
        {
			// unzip it into the api directory; then the user has to restart the api
			string sDestDir = "/bmsweb/publish/";
			System.IO.File.Copy(sFullPath, Path.Combine(sDestDir, "bmsweb.zip"),true);
			BMSCommon.Common.Log("Done unzipping");
		}
		return true;
	}
}
