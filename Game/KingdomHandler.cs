using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Data;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static RnSArchipelago.Utils.HookUtil;

namespace RnSArchipelago.Game
{
    internal unsafe class KingdomHandler
    {
        private IRNSReloaded rnsReloaded;
        private ILoggerV1 logger;
        private IReloadedHooks hooks;
        private SharedData data;

        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;

        internal KingdomHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks, SharedData data)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
            this.data = data;
        }

        // Ends the route early if kingdom sanity is enabled, but not enough kingdoms are unlocked, or progressive kingdom count != maxKingdoms
        internal RValue* EndRouteEarly(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (data.GetValue<string>(DataContext.Connection, "name") != default)
            {
                var isKingdomSanity = data.GetValue<long>(DataContext.Options, "kingdom_sanity")!;
                var isProgressive = data.GetValue<long>(DataContext.Options, "progressive_kingdoms");
                var maxKingdoms = data.GetValue<long>(DataContext.Options, "max_kingdoms_per_run")!;
                var kingdomCount = InventoryUtil.Instance.ProgressiveKingdoms;
                var visitiableKingdomsCount = InventoryUtil.Instance.AvailableKingdomsCount();
                if ((isKingdomSanity == 1 && visitiableKingdomsCount < maxKingdoms) || (isProgressive == 1 && kingdomCount < maxKingdoms))
                {
                    FindLayer(rnsReloaded, "RunMenu_Blocker", out var layer);
                    if (layer != null)
                    {

                        var hallway = layer->Elements.First;
                        while (hallway != null)
                        {
                            var instance = (CLayerInstanceElement*)hallway;
                            var instanceValue = new RValue(instance->Instance);

                            var maxCanRun = Math.Min(maxKingdoms, visitiableKingdomsCount);
                            if (isProgressive == 1)
                            {
                                maxCanRun = Math.Min(maxCanRun, kingdomCount);
                            }

                            if (rnsReloaded.FindValue((&instanceValue)->Object, "currentPos") != null &&
                                (rnsReloaded.FindValue((&instanceValue)->Object, "currentPos")->Real == rnsReloaded.FindValue((&instanceValue)->Object, "notchNumber")->Real - 1) &&
                                rnsReloaded.utils.GetGlobalVar("hallwayCurrent")->Real == maxCanRun)
                            {
                                this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "end halls" + rnsReloaded.FindValue((&instanceValue)->Object, "currentPos")->Real, returnValue, argc, argv), System.Drawing.Color.Gray);
                                rnsReloaded.ExecuteScript("scr_hallwayprogress_make_defeat", self, other, []);
                                return null;
                            }
                            hallway = hallway->Next;

                        }

                        layer = layer->Next;
                        
                        
                    }
                }
            }
            returnValue = endHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        internal RValue* CreateRoute(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (data.GetValue<string>(DataContext.Connection, "name") != default)
            {
                var isKingdomSanity = data.GetValue<long>(DataContext.Options, "kingdom_sanity")!;
                if (isKingdomSanity == 1)
                {
                    var isProgressive = data.GetValue<long>(DataContext.Options, "progressive_kingdoms");
                    var maxKingdoms = data.GetValue<long>(DataContext.Options, "max_kingdoms_per_run")!;
                    var kingdomCount = InventoryUtil.Instance.ProgressiveKingdoms;
                    var visitableKingdoms = InventoryUtil.Instance.AvailableKingdoms;
                    var isSetSeed = data.GetValue<long>(DataContext.Options, "set_kingdom_seed");
                    Random rand;
                    if (isSetSeed == 1 || isProgressive == 1)
                    {
                        rand = new Random((int)(data.GetValue<long>(DataContext.Options, "kingdom_seed")!));
                    } else
                    {
                        rand = new Random();
                    }

                    returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);

                    var unplacedKingdoms = new List<string>();
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                    {
                        unplacedKingdoms.Add("hw_nest");
                    }
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                    {
                        unplacedKingdoms.Add("hw_arsenal");
                    }
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                    {
                        unplacedKingdoms.Add("hw_lakeside");
                    }
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                    {
                        unplacedKingdoms.Add("hw_streets");
                    }
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                    {
                        unplacedKingdoms.Add("hw_lighthouse");
                    }

                    int maxCanRun = (int) Math.Min(maxKingdoms, unplacedKingdoms.Count);
                    if (isProgressive == 1)
                    {
                        maxCanRun = Math.Min(maxCanRun, kingdomCount);
                    }


                    // default is [hw_outskirts, ??, ??, ??, hw_keep(?), hw_pinnacle]
                    var hallkey = rnsReloaded.FindValue(self, "hallkey");
                    Console.WriteLine(maxCanRun + " " + visitableKingdoms);

                    
                    if (maxCanRun <= 3)
                    {
                        Console.WriteLine(maxCanRun);
                        rnsReloaded.ExecuteCodeFunction("array_delete", null, null, [*hallkey, new(maxCanRun+1), new(5 - maxCanRun)]);
                    } else
                    {
                        var dummyArray = new RValue[maxCanRun-2];
                        dummyArray[0] = *hallkey;
                        rnsReloaded.ExecuteCodeFunction("array_push", null, null, dummyArray);
                    }

                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_outskirts");

                    for (var i = 0; i < maxCanRun; i++)
                    {
                        int randomIndex = rand.Next(unplacedKingdoms.Count);
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, i + 1), unplacedKingdoms[randomIndex]);
                        unplacedKingdoms.Remove(unplacedKingdoms[randomIndex]);
                    }

                    var endArray = new RValue[3];
                    endArray[0] = *hallkey;
                    rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);

                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0)
                    {
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun+1), "hw_keep");
                    }
                    if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0)
                    {

                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun+2), "hw_pinnacle");
                    }

                    // Seem to need to go to pale keep and pinnicle, so do handling on arrival of keep
                    // Also seems to not actually allow more than 6

                    Console.WriteLine(rnsReloaded.ArrayGetLength(hallkey));

                    //rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 2), "hw_pinnacle");

                    /*var enemyData = rnsReloaded.utils.GetGlobalVar("enemyData");
                    for (var i = 0; i < rnsReloaded.ArrayGetLength(enemyData)!.Value.Real; i++)
                    {
                        string enemyName = enemyData->Get(i)->Get(0)->ToString();
                        if (enemyName == "en_wolf_blackear")
                        {
                            enemyData->Get(i)->Get(9)->Real = 420;
                        }
                        else if (enemyName == "en_wolf_greyeye")
                        {
                            enemyData->Get(i)->Get(9)->Real = 300;
                        }
                        else if (enemyName == "en_wolf_snowfur")
                        {
                            enemyData->Get(i)->Get(9)->Real = 350;
                        }
                    }*/

                    this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "halls", returnValue, argc, argv), System.Drawing.Color.Gray);

                    return returnValue;
                } else
                {
                    returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
                    return returnValue;
                }
            }
            returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}
