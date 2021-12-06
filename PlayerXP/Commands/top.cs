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

    [CommandHandler(typeof(ClientCommandHandler))]
    public class top : ICommand
    {
        public string Command { get; } = "top";

        public string[] Aliases { get; } = new string[] { "leaderboard" };

        public string Description { get; } = "Provides topmost XP from 15 players.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
	            ev.Allow = false;
				string output;
				int num = 5;
				if (ev.Arguments.Count > 0 && int.TryParse(ev.Arguments[0], out int n)) num = n;
				if (num > 15)
				{
					ev.Color = "red";
					ev.ReturnMessage = "Leaderboard can be no larger than 15.";
					return;
				}
				if (pInfoDict.Count != 0)
				{
					output = $"Top {num} Players:\n";

					for (int i = 0; i < num; i++)
					{
						if (pInfoDict.Count == i) break;
						string userid = pInfoDict.ElementAt(i).Key;
						PlayerInfo info = pInfoDict[userid];
						output += $"{i + 1}) {info.name} ({userid}) | Level: {info.level} | XP: {info.xp} / {XpToLevelUp(userid)}{(PlayerXP.instance.Config.KarmaEnabled ? $" | Karma: {info.karma}" : "")}";
						if (i != pInfoDict.Count - 1) output += "\n";
						else break;
					}

					ev.Color = "yellow";
					ev.ReturnMessage = output;
				}
				else
				{
					ev.Color = "red";
					ev.ReturnMessage = "Error: there is not enough data to display the leaderboard.";
			}
		}
	}
}
