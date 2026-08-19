using System.IO;
using NUnit.Framework;

public class ReactCombatBridgeJslibTests
{
    private static string ReadBridgeSource()
    {
        return File.ReadAllText(Path.Combine(
            "Assets", "Plugins", "WebGL", "InsastralBridge.jslib"));
    }

    [TestCase("Insastral_CombatConnect", "connect")]
    [TestCase("Insastral_CombatDisconnect", "disconnect")]
    [TestCase("Insastral_CombatCommand", "command")]
    public void ExposesGuardedCombatEntryPoint(string nativeName, string bridgeMethod)
    {
        string source = ReadBridgeSource();

        StringAssert.Contains(nativeName + ": function", source);
        StringAssert.Contains("window.insastralCombatBridge", source);
        StringAssert.Contains("window.insastralCombatBridge." + bridgeMethod, source);
    }
}
