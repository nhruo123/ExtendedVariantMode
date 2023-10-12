using System;
using System.Collections.Generic;
using MonoMod.ModInterop;

[ModExportName("ExtendedVariants.CustomDashState")]
public static class CustomDashState
{
    internal static Dictionary<string, Func<bool>> getCustomStateFunctions = new();
    internal static Dictionary<string, Action<bool>> setCustomStateFunctions = new();
    
    public static void RegisterCustomDashState(string dashStateName, Func<bool> getCustomDashState, Action<bool> setCustomDashState)
    {
        getCustomStateFunctions[dashStateName] = getCustomDashState;
        setCustomStateFunctions[dashStateName] = setCustomDashState;
    }
}