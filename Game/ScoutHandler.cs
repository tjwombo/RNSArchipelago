using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Connection;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class ScoutHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private ArchipelagoConnection conn = null!;

        internal IHook<ScriptDelegate>? itemScoutShopHook;

        internal static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        internal static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        internal Dictionary<string, Dictionary<long, ScoutedItemInfo>> chestContents = [];
        internal Dictionary<string, Dictionary<long, ScoutedItemInfo>> shopContents = [];

        internal ScoutHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
        }

        internal void SetConn(ArchipelagoConnection conn)
        {
            this.conn = conn;
        }

        // Add the task to the chest dictionary and create the new task for the next location
        private Task<Dictionary<long, ScoutedItemInfo>> SetChestLocationAndStartNext(Task<Dictionary<long, ScoutedItemInfo>> task, string oldLocation, string newLocation)
        {

            chestContents[oldLocation] = task.Result;
            task.Dispose();

            var locations = CHEST_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, newLocation + " " + x)).ToArray();

            return conn.session!.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations);
        }

        // Scout all the items in the current chest and then start scouting the shop items
        internal void GetArchipelagoChestItemInfo()
        {
            if (conn.session != null)
            {
                var locations = CHEST_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, "Crack in the Geode Chest 1 " + x)).ToArray();

                // Have to chain them like this, otherwise the task doesn't get set properly
                conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations).ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Crack in the Geode Chest 1", "Kingdom Outskirts Chest 1");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Kingdom Outskirts Chest 1", "Crack in the Geode Chest 2");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Crack in the Geode Chest 2", "Kingdom Outskirts Chest 2");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Kingdom Outskirts Chest 2", "Scholar's Nest Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Scholar's Nest Chest", "King's Arsenal Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "King's Arsenal Chest", "Red Darkhouse Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Red Darkhouse Chest", "Emerald Lakeside Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Emerald Lakeside Chest", "Churchmouse Streets Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Churchmouse Streets Chest", "The Pale Keep Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "The Pale Keep Chest", "Darkhouse Depths Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Darkhouse Depths Chest", "Subterra Sanctum Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Subterra Sanctum Chest", "Atelier Aurum Chest");
                }).Unwrap().ContinueWith((task) =>
                {
                    return SetChestLocationAndStartNext(task, "Atelier Aurum Chest", "Looping Hallway Chest");
                }).Unwrap().ContinueWith((task) =>
                {

                    chestContents["Looping Hallway Chest"] = task.Result;

                    GetArchipelagoShopItemInfo();
                });
            }
        }

        // Add the task to the shop dictionary and create the new task for the next location
        private Task<Dictionary<long, ScoutedItemInfo>> SetShopLocationAndStartNext(Task<Dictionary<long, ScoutedItemInfo>> task, string oldLocation, string newLocation)
        {

            shopContents[oldLocation] = task.Result;
            task.Dispose();


            var locations = SHOP_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, newLocation + " " + x)).ToArray();

            return conn.session!.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations);
        }

        // Scout all the items in the current shop
        internal void GetArchipelagoShopItemInfo()
        {
            long[] locations = [];

            if (conn.session != null)
            {
                if (inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, x)).ToArray();
                    shopContents["global"] = conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations).Result;
                }
                else if (inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                {
                    locations = SHOP_POSITIONS.Select(x => conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, "Scholar's Nest Shop " + x)).ToArray();

                    // Have to chain them like this, otherwise the task doesn't get set properly
                    conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations).ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Scholar's Nest Shop", "King's Arsenal Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "King's Arsenal Shop", "Red Darkhouse Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Red Darkhouse Shop", "Emerald Lakeside Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Emerald Lakeside Shop", "Churchmouse Streets Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Churchmouse Streets Shop", "The Pale Keep Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "The Pale Keep Shop", "Darkhouse Depths Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Darkhouse Depths Shop", "Subterra Sanctum Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Subterra Sanctum Shop", "Atelier Aurum Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        return SetShopLocationAndStartNext(task, "Atelier Aurum Shop", "Looping Hallway Shop");
                    }).Unwrap().ContinueWith((task) =>
                    {
                        shopContents["Looping Hallway Shop"] = task.Result;
                    });
                }

            }
        }

        // Scout the network items in the shop ahead of time so once we need the results the task has finished
        internal RValue* ScoutShopItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (inventoryHandler.isActive)
                {
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
                        // TODO: LOOK TO INTEGRATE THIS WITH ITEM CREATION/PLACEMENT
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
