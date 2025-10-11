using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Data;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RnSArchipelago.Game
{
    internal unsafe class LocationHandler
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;

        internal IHook<ScriptDelegate>? notchCompleteHook;

        internal ArchipelagoSession? session;

        private static readonly string GAME = "Rabbit and Steel";
        private static readonly string[] STARTING_LOCATIONS = ["Starting Class", "Starting Kingdom", "Starting Primary", "Starting Secondary", "Starting Special", "Starting Defensive"];

        internal LocationHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
        }

        internal void SendStartLocation()
        {
            long[] locations = STARTING_LOCATIONS.Select(x => session!.Locations.GetLocationIdFromName(GAME, x)).ToArray();
            var locationPacket = new LocationChecksPacket { Locations = locations };
            session!.Socket.SendPacketAsync(locationPacket);
        }


        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.notchCompleteHook!.OriginalFunction(self, other, returnValue, argc, argv);

            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "currentPos", out var instance);
            var element = ((CLayerInstanceElement*)instance)->Instance;
            var notchPos = (int)rnsReloaded.FindValue(element, "currentPos")->Real;
            var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
            kingdomName = kingdomName.Replace(Environment.NewLine, " ");
            var baseLocation = GetNotchLocation(notchPos, kingdomName);

            HookUtil.FindElementInLayer(rnsReloaded, "Ally", "allyId", out instance);
            element = ((CLayerInstanceElement*)instance)->Instance;
            var characterId = (int)rnsReloaded.FindValue(element, "allyId")->Real;
            var character = InventoryUtil.Instance.GetClass(characterId);

            long[] locations = [session!.Locations.GetLocationIdFromName(GAME, baseLocation), session!.Locations.GetLocationIdFromName(GAME, character + " " + baseLocation)];
            var locationPacket = new LocationChecksPacket { Locations = locations };
            session!.Socket.SendPacketAsync(locationPacket);

            return returnValue;
        }

        private string GetNotchLocation(int notchPos, string kingdomName)
        {
            switch (kingdomName) {
                case "Kingdom Outskirts":
                    if (notchPos == 1)
                    {
                        return kingdomName + " Battle " + 1;
                    } else if (notchPos == 3)
                    {
                        return kingdomName + " Battle " + 2;
                    } else if (notchPos == 5)
                    {
                        return kingdomName + " Battle " + 3;
                    }
                    break;

            }
            return "";
        }
    }
}
