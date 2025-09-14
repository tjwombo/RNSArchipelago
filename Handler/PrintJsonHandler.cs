using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Enums;
using System;

namespace RnSArchipelago.Handler
{
    internal sealed class PrintJsonHandler : AMessageHandler
    {
        internal static readonly PrintJsonHandler Instance = new PrintJsonHandler();
        private PrintJsonHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var jsonInfo = (PrintJsonPacket)obj;
            Console.WriteLine(jsonInfo.MessageType);
            switch (jsonInfo.MessageType)
            {
                case JsonMessageType.AdminCommandResult:
                    break;
                case JsonMessageType.Chat:
                    break;
                case JsonMessageType.Collect:
                    break;
                case JsonMessageType.CommandResult:
                    break;
                case JsonMessageType.Countdown:
                    break;
                case JsonMessageType.Goal:
                    break;
                case JsonMessageType.Hint:
                    break;
                case JsonMessageType.ItemCheat:
                    break;
                case JsonMessageType.ItemSend:
                    break;
                case JsonMessageType.Join:
                    break;
                case JsonMessageType.Part:
                    break;
                case JsonMessageType.Release:
                    break;
                case JsonMessageType.ServerChat:
                    break;
                case JsonMessageType.TagsChanged:
                    break;
                case JsonMessageType.Tutorial:
                    break;
            }
            return null;
        }
    }
}
