using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class ScoutHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? itemScoutChestHook;
        internal IHook<ScriptDelegate>? itemScoutShopHook;

        internal static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        internal static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        internal Task<Dictionary<long, ScoutedItemInfo>> chestContents = null!;
        internal Task<Dictionary<long, ScoutedItemInfo>> shopContents = null!;

        internal ScoutHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler, ArchipelagoConnection conn)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.conn = conn;
        }

        // Scout all the items in the current chest
        private void GetArchipelagoChestItemInfo()
        {
            if (conn.session != null)
            {
                var locations = CHEST_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + x)).ToArray();

                chestContents = conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations);
            }
        }

        // Scout the network items in the chest ahead of time so once we need the results the task has finished
        internal RValue* ScoutChestItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (inventoryHandler.isActive)
            {
                GetArchipelagoChestItemInfo();
            }

            if (this.itemScoutChestHook != null)
            {
                returnValue = this.itemScoutChestHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call item scout chest hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Scout all the items in the current shop
        private void GetArchipelagoShopItemInfo()
        {
            long[] locations = [];

            if (conn.session != null)
            {
                if (inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, x)).ToArray();
                }
                else if (inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + x)).ToArray();
                }

                shopContents = conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations);
            }
        }

        // Scout the network items in the shop ahead of time so once we need the results the task has finished
        internal RValue* ScoutShopItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (inventoryHandler.isActive)
                {
                    GetArchipelagoShopItemInfo();

                    if (this.itemScoutShopHook != null)
                    {
                        returnValue = this.itemScoutShopHook.OriginalFunction(self, other, returnValue, argc, argv);
                    }
                    else
                    {
                        logger.PrintMessage("Unable to call item scout shop hook", System.Drawing.Color.Red);
                    }

                    var instance = new RValue(self);
                    long? id = -1;
                    for (var j = 0; j < 9; j++)
                    {
                        id = conn.session?.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[j]);

                        // TODO: RE-TURN THIS ON WHEN THE AP ITEM HAS BEEN BOUGHT
                        // if the item is an archipelago item, disable the purchase condition, mainly applies to hp and upgrades
                        if (id.HasValue && conn.session != null && !conn.session.Locations.AllLocationsChecked.Contains(id.Value))
                        {
                            *rnsReloaded.ArrayGetEntry(instance["storeSlotHeal"], j) = new RValue(0);
                            *rnsReloaded.ArrayGetEntry(instance["storeSlotUpgrade"], j) = new RValue(0);
                        }
                    }

                    return returnValue;
                }
            }

            if (this.itemScoutShopHook != null)
            {
                returnValue = this.itemScoutShopHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call item scout shop hook", System.Drawing.Color.Red); ;
            }

            return returnValue;
        }
    }
}
