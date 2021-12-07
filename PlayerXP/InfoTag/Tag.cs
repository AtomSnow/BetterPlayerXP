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
        public void OnPlayerChangeRole(ChangingRoleEventArgs ev)
        {
            if (PlayerXP.instance.Config.DisplayLevel == true)
            {
            ev.Player.RankName = string.Empty;
            Timing.CallDelayed(delay: 0, () => 
            {
            ev.Player.CustomInfo = ($"Level: 1");
            });
            }
        }
    }
}
//Ignore my tinybrain, it's so late to code.