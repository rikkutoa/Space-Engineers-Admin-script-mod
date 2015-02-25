﻿namespace midspace.adminscripts
{
    using System;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandSessionCreative : ChatCommand
    {
        public CommandSessionCreative()
            : base(ChatCommandSecurity.Admin, "creative", new[] { "/creative" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/creative <on|off>", "Turns creative mode on or off for you.");

            // Allows you to change the game mode to Creative.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server, you can:
            // * drop single blocks at any range, however they will only have 1% construction even though they appear 100% to you.
            // * remove any block instantly as per normal.
            // * drop a line or grid of blocks, these will be 100% constructed unlike single blocks.
            // * allows you to have copypaste.
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/creative ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Creative, bool.TrueString);
                            return true;
                        }
                        MyAPIGateway.Session.GetCheckpoint("null").GameMode = MyGameModeEnum.Creative;
                        MyAPIGateway.Utilities.ShowMessage("Creative", "On");
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Creative, bool.FalseString);
                            return true;
                        }
                        MyAPIGateway.Session.GetCheckpoint("null").GameMode = MyGameModeEnum.Survival;
                        MyAPIGateway.Utilities.ShowMessage("Creative", "Off");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
