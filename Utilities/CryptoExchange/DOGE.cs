using System.Net.Http.Headers;

namespace BBP.CORE.API.Utilities
{
    public static class DOGE
	{
		public static string GetDogeUtxos(string sAddress)
		{
            string sKey = BBPAPI.SecureString.GetDBConfigurationKeyValue("blockcypherapikey");

            string sURL = "https://api.blockcypher.com/v1/doge/main/addrs/" + sAddress + "?unspentOnly=true&includeScript=true&token=" + sKey;
			string sData = BMSCommon.Functions.ExecuteMVCCommand(sURL);
			return sData;
		}

		public static async Task<string> BroadcastTx(string sTxInfo, string sOrigTXID)
		{
			if (sTxInfo == null || sTxInfo == String.Empty)
			{
				return String.Empty;
			}
			string sBCAPIKEY = BBPAPI.SecureString.GetDBConfigurationKeyValue("blockcypherapikey");
			string sURL = "https://api.blockcypher.com/v1/doge/main/txs/push?token=" + sBCAPIKEY;
			sURL = "https://api.tatum.io/v3/dogecoin/broadcast";
            try
			{
				using (var httpClient = new System.Net.Http.HttpClient())
				{
					using (var request = new HttpRequestMessage(new HttpMethod("POST"), sURL))
					{
						httpClient.Timeout = new System.TimeSpan(0, 60, 00);
						httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
						string sBroadcastToken = BMSCommon.Common.GetConfigKeyValue("dogebroadcasttoken");
                        httpClient.DefaultRequestHeaders.Add("x-api-key", sBroadcastToken);
                        string sData = "tx_hex='" + sTxInfo + "'";
						sData = "{\"tx\": \"" + sTxInfo + "\"}";
                        sData = "{\"txData\": \"" + sTxInfo + "\"}";
                        StringContent st1 = new StringContent(sData);
                        st1.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        var oInitialResponse = await httpClient.PostAsync(sURL, st1);
                        string sJsonResponse = await oInitialResponse.Content.ReadAsStringAsync();
						dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject(sJsonResponse);
						string sTXID = o["txId"].ToString() ?? String.Empty;
						return sTXID;
					}
				}
			}
			catch(Exception ex)
			{
				return String.Empty;
			}
			return String.Empty;
		}
	}
}

