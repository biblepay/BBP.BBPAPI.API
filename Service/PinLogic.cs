using BBP.CORE.API.Utilities;
using BMSCommon.Model;
using System.Data;

namespace BBP.CORE.API.Service
{
    public static class PinLogic
	{

        public static List<Pin> GetPinsByUserID(string sUserID)
        {
            List<Pin> pins = QuorumUtils.GetBBPDatabaseObjects<Pin>();
            pins = pins.Where(a => a.UserID == sUserID).ToList();
            return pins;
		}
        public static List<Pin> GetPinsByHash(string sHash, string sPath)
        {
            List<Pin> pins = QuorumUtils.GetBBPDatabaseObjects<Pin>();
            pins = pins.Where(a => a.FileHash == sHash && a.Path == sPath).ToList();
            return pins;
		}

        public static async Task<bool> DbSavePin(Pin p)
        {
            return await QuorumUtils.InsertObject<Pin>(p);
        }

    }
}
