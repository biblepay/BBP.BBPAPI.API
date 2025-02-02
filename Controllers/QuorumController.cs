using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.AspNetCore.Mvc;
using static BMSCommon.Model.BitcoinSyncModel;

namespace bbp.core.api.Controllers;

[ApiController]
public class QuorumController 
{
    // If we are the master we receive data and make new blocks
    // New block every 30 seconds (if data exists), if not the node sleeps and empty blocks are not created.

    public QuorumController()
    {
    }

    [HttpPost]
    [RequestSizeLimit(700000000)]
    [Route("api/QuorumController/InsertChainObjectsExternal")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<string> InsertChainObjectsExternal(List<ChainObject> lCO)
    {
        DACResult d = new DACResult();
        // List<ChainObject> allows us to push in multiple records into the memory pool
        // Memory pool is a file that gets appended with new chain objects.
        // Remove any objects from the CO that fail sanc authority test
        for (int i = 0; i < lCO.Count; i++)
        {
            ChainObject co = lCO[i];
            bool fVerified = QuorumUtils.VerifyChainObject(co);
            if (!fVerified)
            {
                lCO.RemoveAt(i);
            }
        }

        // Add these to a block
        QuorumUtils.AddToMemoryPool(lCO);
        await QuorumUtils.MakeNewBlock();
        d.Result = true;
        d.Response = "1";
        string s1 = Newtonsoft.Json.JsonConvert.SerializeObject(d);
        return s1;
    }


    [HttpPost]
    [Route("api/QuorumController/TestChainObjects")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<DACResult> TestChainObjects()
    {
        DACResult d = new DACResult();
        return d;
    }

}