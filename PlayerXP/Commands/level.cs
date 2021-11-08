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
    public class level : ICommand
    {
        public string Command { get; } = "level";

        public string[] Aliases { get; } = new string[] { "lvl" };

        public string Description { get; } = "Gets your level with XP you have.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
				ev.Allow = false;
				Player player = ev.Arguments.Count == 0 ? ev.Player : Player.Get(ev.Arguments[0]);
				string name;
				bool hasData = pInfoDict.ContainsKey(player.UserId);
				if (player != null) name = player.Nickname;
				else name = hasData ? pInfoDict[ev.Player.UserId].level.ToString() : "[NO DATA]";
				ev.ReturnMessage =
					$"Player: {name} ({player.UserId})\n" +
					$"Level: {(hasData ? pInfoDict[player.UserId].level.ToString() : "[NO DATA]")}\n" +
					$"XP: {(hasData ? $"{pInfoDict[player.UserId].xp.ToString()} / {XpToLevelUp(player.UserId)}" : "[NO DATA]")}" + (PlayerXP.instance.Config.KarmaEnabled ? "\n" +
					$"Karma: {(hasData ? pInfoDict[player.UserId].karma.ToString() : "[NO DATA]")}" : "");
        }
    }
}
