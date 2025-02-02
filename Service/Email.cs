using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using System.Net;
using System.Net.Mail;
using static BMSCommon.Common;

namespace BBP.CORE.API
{
	public static class Email
	{

        public static DACResult SendMail(BBPOutboundEmail e)
        {
            System.Net.Mail.MailMessage bbp_message = new MailMessage();
            bbp_message.Subject = e.Subject;
            if (e.From == String.Empty)
            {
                e.From = "contact@biblepay.org";
            }
            bbp_message.From = new MailAddress(e.From);

            foreach (string sTo in e.To)
            {
                bbp_message.To.Add(sTo);
            }
            foreach (string sCC in e.CC)
            {
                bbp_message.CC.Add(sCC);
            }
            foreach (string sBCC in e.BCC)
            {
                bbp_message.Bcc.Add(sBCC);
            }
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
                string sPass = BMSCommon.Common.GetConfigKeyValue("smtppassword");
                client.Credentials = new NetworkCredential("rob@biblepay.org", sPass);
                bbp_message.From = new MailAddress("rob@biblepay.org", "Team BiblePay");
                bbp_message.Body = e.Body;
                bbp_message.IsBodyHtml = e.IsBodyHTML;

                try
                {
                    client.Send(bbp_message);
                    return r1;
                }
                catch (Exception ex1)
                {
                    System.Threading.Thread.Sleep(1234);
                    Console.WriteLine("Error in Send email: {0}", ex1.Message);
                    r1.Error = "Timeout";
                    return r1;
                }
            }
            catch (Exception)
            {
                r1.Error = "Cannot send Mail.";
                Log(r1.Error);
            }
            return r1;
        }
    }
}
