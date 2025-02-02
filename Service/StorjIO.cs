using BBP.CORE.API.Utilities;
using BMSCommon.Model;
using System.Text;
using uplink.NET.Models;
using uplink.NET.Services;
using static BMSCommon.Common;

namespace BBPAPI
{
    public static class StorjIO
    {
        private static Access UplinkAccessRO()
        {
            string sNewROKey = NVRAM.GetNVRamValue("storjrokey");
            var access = new Access(sNewROKey);
            return access;
        }

        private static Access UplinkAccess()
        {
            string sAccess = NVRAM.GetNVRamValue("storjrwkey");
            Access a = new Access(sAccess);
            return a;
        }

        internal static async Task<bool> BillForStorage(string sBBPAddress, long nTotalSize, int nTotalItems)
        {
            double nCharge = nTotalSize / 10000000;

            string sData = "<bbpaddress>" + sBBPAddress + "</bbpaddress>\r\n"
                + "<totalitems>" + nTotalItems.ToString() + "</totalitems>\r\n"
                + "<totalsize>" + nTotalSize.ToString() + "</totalsize>\r\n"
                + "<updated>" + DateTime.Now.ToShortDateString() + "</updated>\r\n"
                + "<charge>" + nCharge.ToString() + "</charge>\r\n";

            return await StorjIO.InternalStoreDatabaseData("usage", sBBPAddress, sData);
        }


        private static string AddPrefixToDestination(User u, string sDestination, string sOverriddenKey)
        {

            if (sOverriddenKey=="_INTERNAL_")
            {
                return sDestination;
            }

            if (sDestination.StartsWith("/"))
            {
                sDestination = sDestination.Substring(1, sDestination.Length - 1);
            }

            string sPrivKey = String.IsNullOrEmpty(sOverriddenKey) ? "_UNKNOWN_" : sOverriddenKey;
            if (sPrivKey == String.Empty)
            {
                throw new Exception("no privkey");
            }

            string sPubKey = ERCUtilities.GetPubKeyFromPrivKey(false,sPrivKey);
            if (sPubKey == String.Empty)
            {
                throw new Exception("invalid pubkey");
            }

            if (sDestination.StartsWith(sPubKey))
            {
                return sDestination;
            }
            string sNewDest = sPubKey + "/" + sDestination;

            sNewDest = sNewDest.Replace("//", "/");

            return sNewDest;
        }
        //Called by bbpingress + attachment controller
        public static async Task<string> StorjUpload(string sSource, string sPreDest, String sOverridenKey)
        {
                try
                {
                    SetStorjTempPath();

                    string sDest = AddPrefixToDestination(null,sPreDest, sOverridenKey);
                    Access access = UplinkAccess();
                    var bucketService = new BucketService(access);
                    Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
                    byte[] bytesToUpload = System.IO.File.ReadAllBytes(sSource);
                    var objectService = new ObjectService(access);
                    var uploadOperation = await objectService.UploadObjectAsync(thebucket, sDest, 
                        new UploadOptions(), bytesToUpload, false);
                    await uploadOperation.StartUploadAsync();
                    return sDest;
                }
                catch (Exception ex)
                {
                    Log("UplinkUL::" + ex.Message);
                }
            return String.Empty;
        }
        private class UploadWrapper
        {
            public Task? taskUpload { get; set; }
            public string Destination { get; set; }
            public string SourceFileName { get; set; }
            public int StartTime { get; set; }
            public UploadWrapper()
            {
                Destination = String.Empty; 
                SourceFileName = String.Empty;
                StartTime = 0;
                taskUpload = null;
            }
        }
        public static async Task<bool> StorjUploadEntireFolder(User u1, string sSourceFolder, string sDest, String sOverridenKey, 
            bool fLowercase = false, bool fRewrite = false)
        {
            try
            {
                SetStorjTempPath();

                string sBucket = "bbp0";
                string sDestNew = AddPrefixToDestination(null,sDest, sOverridenKey);
                ObjectList lObjList = await StorjGetObjects(sBucket, sDestNew + "/", false);
                Access access = UplinkAccess();
                var bucketService = new BucketService(access);
                Bucket thebucket = await bucketService.GetBucketAsync(sBucket);
                DirectoryInfo d = new DirectoryInfo(sSourceFolder);
                var objectService = new ObjectService(access);
                sSourceFolder = NormalizeFilePath(sSourceFolder);
                List<UploadWrapper> l = new List<UploadWrapper>();
                foreach (var file in d.GetFiles("*.*"))
                {
                    string sDest2 = sDestNew + "/" + file.Name;
                    if (fLowercase)
                    {
                        sDest2 = sDest2.ToLower();
                    }
                    bool fAlreadyInCloud = false;
                    for (int x = 0; x < lObjList.Items.Count; x++)
                    {
                        if (lObjList.Items[x].Key.ToLower().Contains(file.Name.ToLower()))
                        {
                            fAlreadyInCloud = true;
                        }
                    }
                    if (!fAlreadyInCloud || fRewrite)
                    {
                        StreamReader sr = new StreamReader(file.FullName);
                        var uploadOperation = await objectService.UploadObjectAsync(thebucket, sDest2, new UploadOptions(), sr.BaseStream, null);
                        UploadWrapper u = new UploadWrapper();
                        u.taskUpload = uploadOperation.StartUploadAsync();
                        u.SourceFileName = file.FullName;
                        u.Destination = sDest2;
                        u.StartTime = UnixTimestamp();
                        System.Diagnostics.Debug.WriteLine("adding " + sDest2);
                        l.Add(u);
                    }
                }
                // Babysit these
                int nStartTime = UnixTimestamp();
                while (true)
                {
                    int nCompleted = 0;
                    int nTotalTime = UnixTimestamp() - nStartTime;
                    if (nTotalTime > (60 * 15))
                    {
                        throw new Exception("operation timed out");
                    }
                    for (int i = 0; i < l.Count; i++)
                    {
                        UploadWrapper uw = l[i];
                        int nElapsed = UnixTimestamp() - uw.StartTime;


                        if (uw.taskUpload != null && (!uw.taskUpload.IsCompleted && (nElapsed > 267)))
                        {
                            // Restart this one
                            StreamReader sr = new StreamReader(uw.SourceFileName);
                            var uploadOperation = await objectService.UploadObjectAsync(thebucket, uw.Destination, new UploadOptions(), sr.BaseStream, null);
                            uw.taskUpload = uploadOperation.StartUploadAsync();
                            uw.StartTime = UnixTimestamp();
                            l[i] = uw;
                        }
                        if (uw.taskUpload != null && uw.taskUpload.IsCompleted)
                        {
                            nCompleted++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(nElapsed);
                        }

                        if (nCompleted == l.Count)
                        {
                            return true;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine(nCompleted.ToString() + "/" + l.Count.ToString());
                    await Task.Delay(1000);
                    if (l.Count == 0)
                    {
                        break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                string sMyData = ex.Message;
            }
            return false;
        }

        

        private static ObjectService _objsvc = null;
        private static BucketService _bucketsvc = null;
        private static Bucket _bucket = null;
        private static async Task<ObjectService> GetObjectService()
        {
            if (_objsvc != null && false)
            {
                return _objsvc;
            }
            Access access = UplinkAccess();
            _bucketsvc = new BucketService(access);
            _bucket = await _bucketsvc.GetBucketAsync("bbp0");
            _objsvc = new ObjectService(access);
            return _objsvc;
        }

        public static async Task<Stream> StorjDownloadStream(string sSource1)
        {

            try
            {
                ObjectService objectService = await GetObjectService();
                if (sSource1.StartsWith("/"))
                {
                    sSource1 = sSource1.Substring(1, sSource1.Length - 1);
                }
                sSource1 = sSource1.Replace("//", "/");
                var dlop = await objectService.DownloadObjectAsync(_bucket, sSource1, new DownloadOptions(), false);
                var myObject = await objectService.GetObjectAsync(_bucket, sSource1);
                Stream stream = new DownloadStream(_bucket, (int)myObject.SystemMetadata.ContentLength, sSource1);
                return stream;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("object not found"))
                {
                    throw (ex);
                }
                Log("Error in StorjDownloadStream::" + ex.Message);
                throw (ex);
            }
        }

        private static int OctetLength(string sData, string sDelimiter, int nOctetNumber)
        {
            string[] vData = sData.Split(sDelimiter);
            if (vData.Length < nOctetNumber)
                return 0;
            return vData[nOctetNumber].Length;
        }

        private static string AddMyPrefix(string sSource)
        {
            if (sSource.StartsWith("/"))
            {
                sSource = sSource.Substring(1, sSource.Length - 1);
            }
            int nOctLen = OctetLength(sSource, "/", 0);

            if (sSource.Contains("upload/tickets"))
            {
                return sSource;
            }
            if (nOctLen != 34)
            {
                sSource = "BB2BwSbDCqCqNsfc7FgWFJn4sRgnUt4tsM/" + sSource;
            }
            sSource = sSource.Replace("//", "/");

            return sSource;

        }

        public static async Task<bool> StorjDownloadLg(string sSource, string sDest)
        {
            try
            {
                SetStorjTempPath();
                Access access = UplinkAccess();
                var bucketService = new BucketService(access);
                Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
                var objectService = new ObjectService(access);
                sSource = AddMyPrefix(sSource);
                var dlop = await objectService.DownloadObjectAsync(thebucket, sSource, new DownloadOptions(), false);
                await dlop.StartDownloadAsync();
                System.IO.File.WriteAllBytes(sDest, dlop.DownloadedBytes);
                return dlop.Completed;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("object not found"))
                {
                    return false;
                }
                Log("Error in storjdownloadlg::" + ex.Message);
                return false;
            }
        }

        private static void SetStorjTempPath()
        {
            string sTempDir = IsWindows() ? System.IO.Path.GetTempPath() : "~/biblepay/temp";
            uplink.NET.Models.Access.SetTempDirectory(System.IO.Path.GetTempPath());
        }

        public static async Task<string> StorjDownloadString(string sSource0, string sOverriddenKey="")
        {
            try
            {
                SetStorjTempPath();
                Access access = UplinkAccess();
                var bucketService = new BucketService(access);
                Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
                var objectService = new ObjectService(access);
                string sSrc = AddPrefixToDestination(null,sSource0, sOverriddenKey);
                //Log("source of Storjdownloadstring:: " + sSrc);
                var dlop = await objectService.DownloadObjectAsync(thebucket, sSrc, new DownloadOptions(), false);
                await dlop.StartDownloadAsync();
                string sData = System.Text.Encoding.Default.GetString(dlop.DownloadedBytes);
                return sData;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("object not found"))
                {
                    if (!ex.Message.Contains("block_"))
                    {
                        Log("StorjDownloadString::" + sSource0 + "::Not found.");
                    }
                }
                else
                {
                    Log("unable to storjdownloadstring::" + ex.Message);
                }
                return String.Empty;
            }
        }


        /*
        public static async Task<bool> StorjFileExists(string sSource)
        {
            uplink.NET.Models.Access.SetDirectory(System.IO.Path.GetPath());
            Access access = UplinkAccess();
            var bucketService = new BucketService(access);
            Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
            var objectService = new ObjectService(access);
            try
            {
                // metadata
                string sFullSource = (String.Empty) + "/" + sSource;
                var myObject0 = objectService.GetObjectAsync(thebucket, sFullSource);
                await Task.WhenAll(myObject0);
                return myObject0.IsCompleted;
            }
            catch (Exception)
            {
                return false;
            }
        }
        */


        public static async Task<string> UplinkAccessCreate(User u, bool fTestNet)
        {
            if (fTestNet)
            {
                throw new Exception("Only support in mainnet");
            }
            
            string sBBPAddress = ERCUtilities.GetPubKeyFromPrivKey(fTestNet,u.BBPPrivKeyMainNet);
            if (sBBPAddress == String.Empty)
            {
                throw new Exception("Invalid BBP Priv Key");
            }
            string sPre = await StorjIO.UplinkGetDatabaseData("accesstoken", sBBPAddress);
            if (sPre != String.Empty)
            {
                return sPre;
            }

            Access access = UplinkAccess();
            var permission = new uplink.NET.Models.Permission
            {
                AllowDownload = true,
                AllowList = true,
                NotAfter = Convert.ToDateTime("1-1-2050"),
                NotBefore = Convert.ToDateTime("11-1-2022")
            };
            var prefixes = new List<SharePrefix>();
            if (String.IsNullOrEmpty(sBBPAddress) || fTestNet)
            {
                throw new Exception("Empty privkey");
            }
            prefixes.Add(new SharePrefix { Bucket = "bbp0", Prefix = sBBPAddress + "/" });
            var restrictedAccess = access.Share(permission, prefixes);
            string serializedAccess = restrictedAccess.Serialize();
            return serializedAccess.ToString();
        }

        internal static async Task<bool> WriteStorjIndex(string sBBPAddress, StringBuilder sbdata)
        {
            // The file containing all the object indexes
            return await StorjIO.InternalStoreDatabaseData("index", sBBPAddress, sbdata.ToString());
        }

        internal static async Task<ObjectList> StorjGetObjects(string sBucket, string sPrefix, bool fRecursive)
        {
            SetStorjTempPath();

            Access access = UplinkAccess();
            var bucketService = new BucketService(access);
            ListObjectsOptions listOptions = new ListObjectsOptions();
            listOptions.Prefix = sPrefix;
            listOptions.System = true;
            listOptions.Custom = true;
            listOptions.Recursive = fRecursive;
            Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
            var objectService = new ObjectService(access);
            ObjectList objects = await objectService.ListObjectsAsync(thebucket, listOptions);
            return objects;
        }

        internal static async Task<bool> InternalStoreDatabaseData(string sTable, string sKey, string sData)
        {
            string sFN = sKey + ".dat";
            string sPath = GetFolder("database", sFN);
            try
            {
                sPath = NormalizeFilePath(sPath);
                CreateDir(sPath);
                System.IO.File.WriteAllText(sPath, sData);
                string sDest = "database/" + sTable + "/" + sFN;
                await StorjUpload(sPath, sDest, "_INTERNAL_");
                return true;
            }
            catch (Exception ex)
            {
                Log("UplinkStoreDbData::" + ex.Message);
                return false;
            }
        }

        internal static async Task<string> UplinkGetDatabaseData(string sTable, string sKey)
        {
            string sFN = sKey + ".dat";
            string sPre = "database/" + sTable + "/" + sFN;
            string sSourcePath = AddPrefixToDestination(null, sPre, "_INTERNAL_");
            string sData = await StorjDownloadString(sSourcePath, "_INTERNAL_");
            return sData;
        }

        /*
        private static async Task<bool> UplinkDeleteDatabaseData(string sTable, string sKey)
        {
            string sFN = sKey + ".dat";
            string sPath = GetFolder("databas", sFN);
            try
            {
                string sDest = ix(String.Empty) + "/database/" + sTable + "/" + sFN;
                uplink.NET.Models.Access.SetDirectory(System.IO.Path.GetTempPath());
                Access access = UplinkAccess();
                var bucketService = new BucketService(access);
                Bucket thebucket = await bucketService.GetBucketAsync("bbp0");
                var objectService = new ObjectService(access);
                await objectService.DeleteObjectAsync(thebucket, sDest);
                return true;
            }
            catch (Exception ex)
            {
                Log("UplinkDL::" + ex.Message);
                return false;
            }
        }
        */

        public static async Task<string> GetHistoricalCharges(string sBBPAddress)
        {
            string sOut = await StorjIO.UplinkGetDatabaseData("historicalstoragecharges3", sBBPAddress);
            return sOut;
        }

        public static async Task<string> GetHistoricalUsage(string sBBPAddress)
        {
            string sOut = await StorjIO.UplinkGetDatabaseData("usage", sBBPAddress);
            return sOut;
        }
       
    }
}
