namespace PlayerXP
{
    using System;
    using System.Linq;
    using CommandSystem;
    using Exiled.API.Features;
    using Exiled.API.Features.Items;
    using RemoteAdmin;
    using Exiled.Events.EventArgs;
    using System.Collections.Generic;
    using System.IO;
    using Exiled.Permissions.Extensions;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class xpsave : ICommand
    {
        public string Command { get; } = "xpsave";

        public string[] Aliases { get; } = new string[] { "sxp" };

        public string Description { get; } = "Forces a file save from the current round cache.";

        private void SaveStats()
		{
			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for a total of {pInfoDict.Count} players.");
			foreach (KeyValuePair<string, PlayerInfo> info in pInfoDict)
			{
				if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for {info.Key}...");
				File.WriteAllText(Path.Combine(PlayerXP.XPPath, $"{info.Key}.json"), JsonConvert.SerializeObject(info.Value, Formatting.Indented));
			}
		}

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        
        {
        if (!sender.CheckPermission("bpxp.xpsave"))
        {
            response = "You can't use this command, you don't have \"bpxp.xptoggle\" permission.";
            return false;
        }
        
        {
				ev.IsAllowed = false;
				ev.Sender.RemoteAdminMessage("Stats saved!");
				SaveStats();
        }
    }
}
}