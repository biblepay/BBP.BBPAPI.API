using Org.BouncyCastle.Utilities.Encoders;

namespace BBP.CORE.API.Utilities
{
    public class BlockCypherUTXO
    {
        public string address { get; set; }
        public long total_received { get; set; }
        public long total_sent { get; set; }
        public long balance { get; set; }
        public long unconfirmed_balance { get; set; }
        public long final_balance { get; set; }
        public int n_tx { get; set; }
        public int unconfirmed_n_tx { get; set; }
        public int final_n_tx { get; set; }
        public BlockCypherUTXO()
        {
            address = String.Empty;
            total_received = 0;
        }
    }

    public class BlockCypherFunctions
    {
        public static string EncryptString(string sData, string sPassword)
        {
            int iPos = 0;
            string sHex = String.Empty;
            for (int i = 0; i < sData.Length; i++)
            {
                string sChar = sData.Substring(i, 1);
                char cChar = char.Parse(sChar);
                string sPasswChar = sPassword.Substring(iPos, 1);
                char cPasswChar = char.Parse(sPasswChar);

                var sum = (int)cChar + (int)cPasswChar;
                if (sum > 255)
                    sum -= 255;
                string sHexByte = sum.ToString("X2");
                sHex += sHexByte;
                iPos++;
                if (iPos >= sPassword.Length)
                {
                    iPos = 0;
                }
            }
            return sHex;
        }
        public static string DecryptString(string sData, string sPassword)
        {
            try
            {
                int iPos = 0;
                string sOut = String.Empty;
                for (int i = 0; i < sData.Length; i += 2)
                {
                    string sChunk = sData.Substring(i, 2);
                    int nValue = Convert.ToInt32(sChunk, 16);
                    string sPassChar = sPassword.Substring(iPos, 1);
                    char cPassChar = char.Parse(sPassChar);
                    var dif = nValue - (int)cPassChar;
                    if (dif < 0)
                    {
                        dif += 255;
                    }
                    char cDecChar = (char)dif;
                    sOut += cDecChar.ToString();
                    iPos++;
                    if (iPos >= sPassword.Length)
                    {
                        iPos = 0;
                    }
                }
                return sOut;
            }
            catch(Exception ex)
            {
                return String.Empty;
            }
        }
    }
}
