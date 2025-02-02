using BBP.CORE.API.Utilities;
using static BMSCommon.Encryption;

namespace BBP.CORE.API.Service
{
    public class KeyServer
    {

        public static string KeySuffix = "DGZZ";
        // If we have less than 100 DGZZ keys, generate more.

        public static async Task<bool> GenKeys()
        {
            try
            {
                string sPath = BMSCommon.Common.GetFolder("AssetKey", "assetkeys.dat");
                long lSize = GetFileSize(sPath) / 125;
                if (lSize > 125)
                {
                    return true;
                }
                else
                {
                    if (nCurThreads < 10)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            System.Threading.Thread t = new Thread(GenerateAssetKey);
                            t.Start();
                        }
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        private static long GetFileSize(string sPath)
        {
            if (System.IO.File.Exists(sPath))
            {
                FileInfo fi = new FileInfo(sPath);
                return fi.Length;
            }
            else
            {
                return 0;
            }
        }

        private static int nCurThreads = 0;
        private async static void GenerateAssetKey()
        {
            nCurThreads++;
            for (int i = 1; i < 100000; i++)
            {
                string sSeed = Guid.NewGuid().ToString();
                BBPKeyPair kpDoge = await Ethereum.DeriveAltcoinKey("exp", sSeed);
                if (kpDoge.PubKey.ToUpper().EndsWith(KeyServer.KeySuffix))
                {
                    string sData = sSeed + "|" + kpDoge.PubKey + "|" + kpDoge.PrivKey;
                    string sPath = BMSCommon.Common.GetFolder("AssetKey", "assetkeys.dat");
                    File.AppendAllText(sPath, sData + Environment.NewLine);
                }
            }
            nCurThreads--;
        }
    }
}
