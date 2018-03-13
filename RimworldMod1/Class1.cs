﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HugsLib;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;

namespace YieldInspector 
{

    [StaticConstructorOnStartup]
    public class YieldInspector : ModBase
    {
        public static YieldInspector instance { get; set; }
        public override string ModIdentifier 
        {
            get { return "ray.YieldInspector";}
        }

        
        YieldInspector()
        {
            if (instance != null) instance = this;

        }

        public static void Log(string message, params object[] args)
        {
            YieldInspector.instance.Logger.Message(message, args);
        }

        
    


    }

    [HarmonyPatch(typeof(Zone_Growing))]
    [HarmonyPatch("GetInspectString")]
    public static class GrowthZoneYield
    {
        [HarmonyPostfix]
        public static void Postfix(ref Zone_Growing __instance, ref string __result)
        {
            var plantDef = __instance.GetPlantDefToGrow();
            var thePlant = plantDef.plant;
            int totalYield = 0, maxYield = 0, num = 0;


            foreach (Thing thing in __instance.AllContainedThings)
            {
                if (thing.def == plantDef && thing is Plant plant)
                {
                    totalYield += plant.YieldNow();
                    ++num;
                }
            }

            maxYield = (int)thePlant.harvestYield;
            float efficiency = maxYield / thePlant.growDays;

            __result += "\n" + "YI.InspectYields".Translate(new object[] { totalYield.ToString(), (maxYield * num).ToString() });
            __result += "\n" + "YI.Efficiency".Translate() + String.Format(": {0:P2}", efficiency.ToString());

        }
    }

    [HarmonyPatch(typeof(Plant))]
    [HarmonyPatch("GetInspectString")]
    public static class PlantYield
    {
        [HarmonyPostfix]
        public static void Postfix(ref Plant __instance, ref string __result)
        {
            __result += "\n" + "YI.InspectYields".Translate(new object[] { (__instance.YieldNow()).ToString(), ((int)(__instance.def.plant.harvestYield)).ToString() });
        }
    }


//     [HarmonyPatch(typeof(ThingDef))]
//     [HarmonyPatch("DisplaySpecialStats")]
    [HarmonyPatch(typeof(StatsReportUtility), "StatsToDraw", new Type[] { typeof(Thing) } )]
//    Maybe use DrawStatsReport instead
    public static class PlantStatReportYield
    {
        public static MethodInfo fnStatsToDraw = null;

        [HarmonyPrefix]
        public static bool Prefix(ref IEnumerable<StatDrawEntry> __result, ref Thing thing)
        {
            //             if (__instance.category is ThingCategory.Plant)
            //             {
            //                 PlantProperties plantProp = __instance.plant;
            //                 List<StatDrawEntry> entries = new List<StatDrawEntry>();
            //                 entries.AddRange(__instance.SpecialDisplayStats());
            //                 if (plantProp.Harvestable)
            //                 {
            //                     entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "Current Yield", ))
            //                 }
            //                 return false;
            //             }
            //             


            if (thing.def.category == ThingCategory.Plant)
            {
                Plant plant = thing as Plant;
                List<StatDrawEntry> entries = new List<StatDrawEntry>();
                if (fnStatsToDraw == null)
                {

                    fnStatsToDraw = typeof(StatsReportUtility).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(mInfo => mInfo.GetParameters().Types().Contains(typeof(Thing))).First();
                    YieldInspector.Log("Found StatsToDraw().");
                }
                if (fnStatsToDraw != null)
                {
                    YieldInspector.Log("Calling StatsToDraw().");
                    entries.AddRange((IEnumerable<StatDrawEntry>)fnStatsToDraw.Invoke(null, new object[] { thing }));
                }
                //entries.AddRange(StatsReportUtility.Stas(thing);

                if (plant.IsCrop)
                {
                    float maxYield = plant.def.plant.harvestYield;
                    float efficiency = maxYield / plant.def.plant.growDays;

                    entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Yield".Translate(), plant.YieldNow().ToString(), 0, string.Empty));
                    entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Maximum".Translate(), ((int)(maxYield)).ToString(), 0, string.Empty));
                    //entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "Efficiency", String.Format("{0:0.##}%", efficiency.ToString())));
                    entries.Add(new StatDrawEntry( StatCategoryDefOf.Basics, "YI.Efficiency".Translate(), String.Format("{0:P2}", efficiency.ToString(), 0, string.Empty) ));

                    __result = entries.AsEnumerable();
                    YieldInspector.Log("Changed enumerable.");
                }


                return false;
            }


            return true;
        }
    }




}
