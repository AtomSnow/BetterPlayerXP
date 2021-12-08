using Exiled.API.Features;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.Events.EventArgs;
using PlayerXP;
using MEC;

namespace PlayerXP.Tag
{    
    public class Tag
    {
        public static Dictionary<string, PlayerInfo> pInfoDict = new Dictionary<string, PlayerInfo>();
        public int GetLevel(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].level;
			}
			else return -1;
		}
        public void OnVerified(VerifiedEventArgs ev)
        {
            if (PlayerXP.instance.Config.DisplayLevel == true)
            {
                ev.Player.CustomInfo = string.Empty;
                ev.Player.CustomInfo = ($"Level: {GetLevel(ev.Player.UserId)}");
            }
        }
    }
}
//Ignore my tinybrain, it's so late to code.