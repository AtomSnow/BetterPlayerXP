using Exiled.API.Features;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.Events.EventArgs;
using PlayerXP;

namespace PlayerXP.Tag
{    
    public class Tag
    {
        public void OnPlayerChangeRole(ChangingRoleEventArgs ev)
        {
            if (PlayerXP.instance.Config.DisplayLevel == true)
            {
            ev.Player.CustomInfo = string.Empty;
            ev.Player.CustomInfo = ($"Level: 1");
            }
        }
    }
}
//Ignore my tinybrain, it's so late to code.