using HarmonyLib;
using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Verse;

namespace WallArmorContainment
{
    [HarmonyPatch]
    public class WallArmorContainment : ModBase
    {
        private static float GetWallArmorHP(IntVec3 item, Map map)
        {
            var thingList = item.GetThingList(map).Where(thing => thing.def.defName == "RB_OverwallArmor");
            foreach (var thing in thingList)
            {
                if (thing is Building building)
                {
                    return building.HitPoints;
                }
            }

            return 0f;
        }
        
        private static MethodInfo GetWallArmorHP_MethodInfo = SymbolExtensions.GetMethodInfo(() => GetWallArmorHP(new IntVec3(), null));

        [HarmonyPatch(typeof(StatWorker_ContainmentStrength))]
        [HarmonyPatch("CalculateValues")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> StatWorker_ContainmentStrength_CalculateValues_Transplier(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction lastInstruction = null;
            var found = false;
            foreach (var instruction in instructions)
            {
                if (!found
                    && instruction?.opcode == OpCodes.Stloc_S
                    && (instruction.operand as LocalBuilder)?.LocalIndex == 4
                    && lastInstruction?.opcode == OpCodes.Add)
                {
                    found = true;
                    yield return CodeInstruction.LoadLocal(24);
                    yield return CodeInstruction.LoadLocal(0);
                    yield return new CodeInstruction(OpCodes.Call, GetWallArmorHP_MethodInfo);
                    yield return new CodeInstruction(OpCodes.Add);
                }

                lastInstruction = instruction;
                yield return instruction;
            }
        }
    }
}