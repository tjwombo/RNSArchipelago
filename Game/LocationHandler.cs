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

        // Send the starting locations
        internal void SendStartLocation()
        {
            long[] locations = STARTING_LOCATIONS.Select(x => session!.Locations.GetLocationIdFromName(GAME, x)).ToArray();
            var locationPacket = new LocationChecksPacket { Locations = locations };
            session!.Socket.SendPacketAsync(locationPacket);
        }

        // Send the location for finishing a notch if there is a generic location for it, i.e. battle, chest, or boss (not shop)
        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.notchCompleteHook!.OriginalFunction(self, other, returnValue, argc, argv);

            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "currentPos", out var instance);
            var element = ((CLayerInstanceElement*)instance)->Instance;
            
            var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
            kingdomName = kingdomName.Replace(Environment.NewLine, " ");
            var baseLocation = kingdomName + GetNotchName(element);

            HookUtil.FindElementInLayer(rnsReloaded, "Ally", "allyId", out instance);
            element = ((CLayerInstanceElement*)instance)->Instance;
            var characterId = (int)rnsReloaded.FindValue(element, "allyId")->Real;
            var character = InventoryUtil.Instance.GetClass(characterId);

            long[] locations = [session!.Locations.GetLocationIdFromName(GAME, baseLocation), session!.Locations.GetLocationIdFromName(GAME, character + " " + baseLocation)];
            var locationPacket = new LocationChecksPacket { Locations = locations };
            session!.Socket.SendPacketAsync(locationPacket);

            return returnValue;
        }

        // Get the name of the location for the notch based on its image and number of occurence
        private string GetNotchName(CInstance* element)
        {
            var notchPos = (int)rnsReloaded.FindValue(element, "currentPos")->Real;
            var notches = rnsReloaded.FindValue(element, "xSubimg");

            var notchType = rnsReloaded.ArrayGetEntry(notches, notchPos)->Real;

            if (notchType == 4)
            {
                return " Boss";
            }

            int count = 1;


            for (var i = 0; i < notchPos; i++)
            {
                if (rnsReloaded.ArrayGetEntry(notches, i)->Real == notchType)
                {
                    count++;
                }
            }

            if (notchType == 1)
            {
                return " Chest " + count;
            }

            return " Battle " + count;
        }
    }
}
