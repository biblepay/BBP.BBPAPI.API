using Microsoft.AspNetCore;
using bbp.core.api;
using BiblePay.BMS;
using BBP.CORE.API.Utilities;


BMSCommon.Common.BMS_CONFIG_FILE_NAME = "bms.conf";
string sBindURL = BMSCommon.Common.GetConfigKeyValue("bindurlapi");

if (sBindURL != string.Empty)
{
    BMSCommon.Common.BMS_BIND_URL = sBindURL;
}


StartupConfig.CreateHostBuilder(args, BMSCommon.Common.BMS_BIND_URL, null).Build().Run();
