namespace SilenceInCity.Patch;

[HarmonyPatch] public class DoNotUnlockRecipe
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))] [HarmonyPrefix] [HarmonyWrapSafe]
    private static bool Check(Player __instance)
    {
        if (__instance == Player.m_localPlayer
            //  && !Player.m_debugMode
            && SilenceMono.InsideArea(__instance.transform.position)) return false;

        return true;
    }
}