using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using NBitcoin;
using System.Net;
using System.Net.Mail;
using static BMSCommon.Encryption;

namespace BBPAPI
{
    public static class ERCUtilities
    {
		public static BBPKeyPair DeriveKey(bool fTestNet, string sSha)
		{
			NBitcoin.Mnemonic m = new NBitcoin.Mnemonic(sSha);
			NBitcoin.ExtKey k = m.DeriveExtKey(null);
			BBPKeyPair k1 = new BBPKeyPair
			{
				PrivKey = k.PrivateKey.GetWif(fTestNet ? NBitcoin.Network.TestNet : NBitcoin.Network.Main).ToWif().ToString(),
				PubKey = k.ScriptPubKey.GetDestinationAddress(fTestNet ? NBitcoin.Network.TestNet : NBitcoin.Network.Main).ToString()
			};
			return k1;
		}

		public static string SignMessage(bool fTestNet, string sPrivKey, string sMessage)
		{
			try
			{
                if (sPrivKey == null || sMessage == String.Empty || sMessage == null)
                { return string.Empty; }

				BitcoinSecret bsSec;
				if (!fTestNet)
				{
					bsSec = Network.Main.CreateBitcoinSecret(sPrivKey);
				}
				else
				{
					bsSec = Network.TestNet.CreateBitcoinSecret(sPrivKey);
				}
				string sSig = bsSec.PrivateKey.SignMessage(sMessage);
				string sPK = bsSec.GetAddress().ToString();
				var fSuc = VerifySignature(fTestNet, sPK, sMessage, sSig);
				return sSig;
			}
			catch (Exception)
			{
				return String.Empty;
			}
		}

		public static bool VerifySignature(bool fTestNet, string BBPAddress, string sMessage, string sSig)
		{
			if (BBPAddress == null || sSig == String.Empty || BBPAddress == "" || BBPAddress == null || sSig == null || BBPAddress.Length < 20)
				return false;
			try
			{
				BitcoinPubKeyAddress bpk;
				if (fTestNet)
				{
					bpk = new BitcoinPubKeyAddress(BBPAddress, Network.TestNet);
				}
				else
				{
					bpk = new BitcoinPubKeyAddress(BBPAddress, Network.Main);
				}

				bool b1 = bpk.VerifyMessage(sMessage, sSig, true);
				return b1;
			}
			catch (Exception)
			{
				return false;
			}
		}


        public static DACResult SendVerificationEmail(User u)
        {
            MailAddress mTo = new MailAddress(u.Email2, u.NickName);
            MailMessage m = new MailMessage();
            m.To.Add(mTo);
            MailAddress mBCC1 = new MailAddress("Rob@biblepay.org", "team biblepay");
            m.Subject = "E-Mail Verification";
            string sURL = "https://unchained.biblepay.org/profile/verifyemailaddress?id=" + u.id.ToString()
                + "&key=" + Encryption.Base64Encode(EncryptAES256(u.id, "salt_" + u.id));
            m.Body = "<br>Dear " + u.NickName + ", <br><br> Please verify your e-mail address <a href='" 
                + sURL + "'>by clicking here.</a><br><br><br>"
                + "<br>Thank you for using BiblePay!";
            m.IsBodyHtml = true;
            DACResult r = SendMail4(false, m);
            return r;
        }


		public static DACResult SendMail4(bool fTestNet, System.Net.Mail.MailMessage bbp_message)
		{
			DACResult r1 = new DACResult();
			try
			{
				string sID = Encryption.GetSha256HashI(bbp_message.Subject);
				System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
				client.UseDefaultCredentials = false;
				client.Credentials = new System.Net.NetworkCredential("1", "2"); // Do not change these values, change the config values.
				client.Port = 587;
				client.EnableSsl = false;
				client.Host = "seven.biblepay.org";
				client.DeliveryMethod = SmtpDeliveryMethod.Network;
				client.UseDefaultCredentials = false;
                string sMailServerPW = BMSCommon.Common.GetConfigKeyValue("smtppassword");
				client.Credentials = new NetworkCredential("rob@biblepay.org", sMailServerPW);
				bbp_message.From = new MailAddress("rob@biblepay.org", "Team BiblePay");
				try
				{
					client.Send(bbp_message);
					return r1;
				}
				catch (Exception e)
				{
					System.Threading.Thread.Sleep(1234);
					Console.WriteLine("Error in Send email: {0}", e.Message);
					r1.Error = "Timeout";
					return r1;
				}
			}
			catch (Exception)
			{
				r1.Error = "Cannot send Mail.";
				BMSCommon.Common.Log(r1.Error);
			}
			return r1;
		}

		public static DACResult SendNFTEmail(User uBuyer, User dtSeller, RetiredNFT n, double nAmount)
        {
            try
            {
                MailAddress mTo = new MailAddress("contact@biblepay.org", "Team BiblePay");
                MailMessage m = new MailMessage();
                m.To.Add(mTo);
                MailAddress mBCC1 = new MailAddress(uBuyer.Email2, uBuyer.NickName);
                string sSellerEmail = dtSeller.Email2;
                string sSellerNickName = dtSeller.NickName ?? String.Empty;
                if (sSellerEmail == null)
                    sSellerEmail = "Unknown Seller";
                if (sSellerNickName == String.Empty)
                    sSellerNickName = "Unknown Seller NickName";
                if (n.Type.ToLower() == "orphan")
                {
                    m.CC.Add(mBCC1);
                    try
                    {
                        MailAddress mBCC2 = new MailAddress(sSellerEmail, sSellerNickName);
                        m.Bcc.Add(mBCC2);
                    }
                    catch (Exception) { }
                }
                else
                {
                    m.Bcc.Add(mBCC1);
                    try
                    {
                        MailAddress mBCC2 = new MailAddress(sSellerEmail, sSellerNickName);
                        m.Bcc.Add(mBCC2);
                    }
                    catch (Exception) { }
                }

                string sSubject = (n.Type.ToLower() == "orphan") ? "Orphan Sponsored " + n.GetHash() : "Bought NFT " + n.GetHash();

                m.Subject = sSubject;
                string sPurchaseNarr = (n.Type.ToLower() == "orphan") ? "has been sponsored" : "has been purchased";

                m.Body = "<br>Dear " + sSellerNickName + ", " + sPurchaseNarr + " by " + uBuyer.NickName + " for "
                    + nAmount.ToString() + ".  <br><br><br><h3>"
                    + n.Name + "</h3><br><br><div><span>" + n.Description + "</div><br><br><br><img src='" + n.AssetURL
                    + "' width=400 height=400/><br><br><br>Thank you for using BiblePay!";

                m.IsBodyHtml = true;
                return SendMail4(false, m);
            }
            catch(Exception ex)
            {
                BMSCommon.Common.Log(ex.Message);
                DACResult r = new DACResult();
                return r;
            }
        }


        public static async Task<DACResult> SendBBPFromSubscription(bool fTestNet, string sPrivKey, string sToAddress, double nAmount, string sOptPayload = "", string sOptNonce = "")
        {
            string sPubKey = ERCUtilities.GetPubKeyFromPrivKey(fTestNet, sPrivKey);
            string sData = WebRPC.GetAddressUTXOs(fTestNet, sPubKey);
            string sErr = String.Empty;
            string sHEX = String.Empty;
            string sTXID = String.Empty;
            NBitcoin.Crypto.BBPTransaction.PrepareFundingTransaction(fTestNet, nAmount, sToAddress, sPrivKey, sOptPayload, sData, out sErr, out sHEX, out sTXID);
            DACResult r = new DACResult();
            if (sErr != String.Empty)
            {
                r.Error = sErr;
                return r;
            }

            r = await WebRPC.SendRawTx(fTestNet, sHEX, sTXID);
            
            return r;
        }

        public static string GetPubKeyFromPrivKey(bool fTestNet, string sPrivKey)
        {
			return  NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(fTestNet, sPrivKey);
		}

        public static string GetPubKeyFromPrivKey(string sNetwork, string sPrivKey)
        {
			return NBitcoin.Crypto.BBPTransaction.GetPubKeyFromPrivKey(sNetwork, sPrivKey);
		}
		public static double QueryAddressBalance(bool fTestNet, string sAddress)
        {
            if (String.IsNullOrEmpty(sAddress))
            {
                return 0;
            }

            int nHeight = WebRPC.GetHeight(false);

            string sUTXOData = WebRPC.GetAddressUTXOs(fTestNet, sAddress);
            double nAmt = NBitcoin.Crypto.BBPTransaction.QueryAddressBalance(sUTXOData);
            BMSCommon.Common.Log("QAB::Address " + sAddress + "=" + nAmt.ToString());
            return nAmt;
        }
    }
}
