namespace H3VRMod
{
    // Based on Potatoes' Twin Stick Swinger
    // Forked from https://github.com/potatoes1286/TwinStickSwinger
    // This version replaces Armswinger mode instead of Dash

    // It respects the following settings:
    // - TwinStick Options:
    //   - Movement Speed
    //   - Controller Forward/Side Root
    //   - TwinStick Left/Right Handedness
    //   - TwinStick Jump
    // - ArmSwinger Options:
    //   - ArmSwinger Jump
    //   - ArmSwinger Turning Mode

    // It IGNORES the following settings:
    // - TwinStick Options:
    //   - TwinStick Turning Mode
    //   - TwinStick Sprint Mode
    //   - TwinStick Sprint Toggle Mode
    // - ArmSwinger Options:
    //   - ArmSwinger Base Speed (Left Hand)
    //   - ArmSwinger Base Speed (Right Hand)
    internal static class PluginInfo
    {
        internal const string NAME = "TwinStick Arm Sprint";
        internal const string GUID = "dll.h3vr.odekak.twinstickarmsprint";
        internal const string VERSION = "1.0.1";
    }
}
