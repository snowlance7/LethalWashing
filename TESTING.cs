using Dawn;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;
using SnowyLib;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace LethalWashing
{
    [HarmonyPatch]
    public class TESTING : MonoBehaviour
    {
        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            try
            {
                if (!Utils.testing) { return; }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                switch (args[0])
                {
                    case "/lw_items":
                        foreach (var item in LethalContent.Items.Values)
                        {
                            logger.LogInfo(item.Item.name);
                        }
                        break;
                    default:
                        if (!Utils.testing || !IsServerOrHost) { return; }
                        Utils.ChatCommand(args);
                        break;
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}