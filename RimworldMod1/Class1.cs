using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HugsLib;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

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
            if (instance != null)
                YieldInspector.instance.Logger.Message(message, args);
            Verse.Log.Message(message);
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
/*    [HarmonyPatch(typeof(StatsReportUtility), "StatsToDraw", new Type[] { typeof(Thing) } )]*/
//    Maybe use DrawStatsReport instead
    public static class PlantStatReportYield
    {
        public static MethodInfo fnStatsToDraw { get; set; }

        [HarmonyPrefix]
        public static bool Prefix(ref IEnumerable<StatDrawEntry> __result, Thing thing)
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


            if (thing is Plant plant && plant.IsCrop)
            {
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



    [HarmonyPatch(typeof(StatsReportUtility), "DrawStatsReport", new Type[] { typeof(UnityEngine.Rect), typeof(Thing)})]
    public static class PlantStatReportYield2
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("In transpiler");

            var instList = instructions.ToList();
            for (int i = 0; i < instList.Count; ++i)
            {
                var inst = instList[i];
                yield return inst;

                if (inst.opcode == OpCodes.Brfalse && instList[i+1].opcode == OpCodes.Ldsfld
                    && instList[i+1].operand == AccessTools.Field(typeof(StatsReportUtility), "cachedDrawEntries") )
                {
                    yield return new CodeInstruction(OpCodes.Ldarg, 1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PlantStatReportYield2).GetMethod("DrawYieldStatReports"));

                }

            }
            yield break;
        }

        public static void DrawYieldStatReports(Thing thing)
        {
            if (thing is Plant plant && plant.IsCrop)
            {
                var entries = AccessTools.Field(typeof(StatsReportUtility), "cachedDrawEntries").GetValue(null);
                var fnAdd = entries.GetType().GetMethod("Add", new Type[] {typeof(StatDrawEntry) });

                Log.Message("Adding to cachedDrawEntries");

                float maxYield = plant.def.plant.harvestYield;
                float efficiency = maxYield / plant.def.plant.growDays;
                fnAdd.Invoke(entries, new object[] { new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Yield".Translate(), plant.YieldNow().ToString(), 0, string.Empty) });
                fnAdd.Invoke(entries, new object[] { new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Maximum".Translate(), ((int)(maxYield)).ToString(), 0, string.Empty) });
                fnAdd.Invoke(entries, new object[] { new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Efficiency".Translate(), String.Format("{0:P2}", efficiency.ToString(), 0, string.Empty)) }) ;

//                 entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Yield".Translate(), plant.YieldNow().ToString(), 0, string.Empty));
//                 entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Maximum".Translate(), ((int)(maxYield)).ToString(), 0, string.Empty));
//                 //entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "Efficiency", String.Format("{0:0.##}%", efficiency.ToString())));
//                 entries.Add(new StatDrawEntry(StatCategoryDefOf.Basics, "YI.Efficiency".Translate(), String.Format("{0:P2}", efficiency.ToString(), 0, string.Empty)));


            }
        }
    }

}
