namespace PlayerXP
{
    using System;
    using CommandSystem;
    using Exiled.Permissions.Extensions;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class xptoggle : ICommand
    {
        public string Command { get; } = "xptoggle";

        public string[] Aliases { get; } = new string[] { "txp" };

        public string Description { get; } = "Toggles XP gaining/saving.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        
        {
        if (!sender.CheckPermission("bpxp.xptoggle"))
        {
            response = "You can't use this command, you don't have \"bpxp.xptoggle\" permission.";
            return false;
        }

        {
				ev.IsAllowed = false;
				ev.Sender.RemoteAdminMessage($"XP saving has been toggled {(isToggled ? "on" : "off")}");
				isToggled = false;
        }
    }
}
}

//This will be rewritten soon.