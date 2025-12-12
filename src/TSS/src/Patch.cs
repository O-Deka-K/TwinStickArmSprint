using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static FistVR.ControlOptions;

namespace TwinStickArmSprint
{
    public class Patch
    {
        private static readonly MethodInfo miHandUpdateArmSwinger = typeof(FVRMovementManager).GetMethod("HandUpdateArmSwinger", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miHandUpdateTwinstick = typeof(FVRMovementManager).GetMethod("HandUpdateTwinstick", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miAXButtonCheck = typeof(FVRMovementManager).GetMethod("AXButtonCheck", BindingFlags.Instance | BindingFlags.NonPublic);

        // This function finds the movement hand
        public static int GetMovementHand(FVRMovementManager instance)
        {
            int rightHand = instance.Hands[0].IsThisTheRightHand ? 0 : 1;
            return (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove) ? rightHand : 1 - rightHand;
        }

        // This is used by controllers that do not have an analog stick, so they use the touchpad instead
        [HarmonyPatch(typeof(FVRMovementManager), "ShouldFlushTouchpad")]
        [HarmonyPostfix]
        public static void Patch_ShouldFlushTouchpad(FVRMovementManager __instance, bool ___m_isLeftHandActive, bool ___m_isRightHandActive, FVRViveHand hand, ref bool __result)
        {
            if (!hand.IsInStreamlinedMode && hand.CMode != ControlMode.Index && hand.CMode != ControlMode.WMR && __instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                bool isTurningHand = hand.IsThisTheRightHand;

                if (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove)
                    isTurningHand = !isTurningHand;

                if (___m_isLeftHandActive && !isTurningHand)
                    __result = true;

                if (___m_isRightHandActive && isTurningHand)
                    __result = true;
            }
        }

        public class HandState
        {
            public Quaternion pointerRotation_0;
            public Quaternion pointerRotation_1;
            public int baseSpeedLeft;
            public int baseSpeedRight;
            public CoreControlMode coreControlMode;
            public bool isInStreamlinedMode_0;
            public bool isInStreamlinedMode_1;
            public bool touchpadNorthPressed_0;
            public bool touchpadNorthPressed_1;
            public bool touchpadWestPressed;
            public bool touchpadEastPressed;
        }

        // This function gets the movement axis velocity
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateMovementWithHand")]
        [HarmonyPrefix]
        public static bool Patch_HandMovementUpdate(FVRMovementManager __instance, ref Vector3 ___worldTPAxis, bool ___m_sprintingEngaged, FVRViveHand hand, out HandState __state)
        {
            __state = new HandState();

            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                // TwinStick Arm Sprint mode
                if (Plugin.HeadArmswinger == null || !Plugin.HeadArmswinger.Value)
                {
                    // Handle snap turning
                    if (__instance.Hands[0].CMode == ControlMode.Oculus)
                    {
                        __state.coreControlMode = GM.Options.ControlOptions.CCM;
                        __state.isInStreamlinedMode_0 = __instance.Hands[0].IsInStreamlinedMode;
                        __state.isInStreamlinedMode_1 = __instance.Hands[1].IsInStreamlinedMode;

                        GM.Options.ControlOptions.CCM = CoreControlMode.Streamlined;
                        __instance.Hands[0].IsInStreamlinedMode = true;
                        __instance.Hands[1].IsInStreamlinedMode = true;
                    }

                    // Only call HandUpdateArmSwinger if this is the non-movement hand
                    if (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove ^ hand.IsThisTheRightHand)
                    {
                        miHandUpdateArmSwinger.Invoke(__instance, [hand]);
                    }

                    if (__instance.Hands[0].CMode == ControlMode.Oculus)
                    {
                        // Restore controls used in Streamlined Mode
                        GM.Options.ControlOptions.CCM = __state.coreControlMode;
                        __instance.Hands[0].IsInStreamlinedMode = __state.isInStreamlinedMode_0;
                        __instance.Hands[1].IsInStreamlinedMode = __state.isInStreamlinedMode_1;
                    }

                    // Ignore TwinStick turn mode
                    var mode = GM.Options.MovementOptions.TwinStickSnapturnState;
                    GM.Options.MovementOptions.TwinStickSnapturnState = MovementOptions.TwinStickSnapturnMode.Disabled;

                    miHandUpdateTwinstick.Invoke(__instance, [hand]);

                    // Remove effect of sprinting
                    if (___m_sprintingEngaged && GM.Options.MovementOptions.TPLocoSpeedIndex < 5)
                    {
                        Vector3 normalized = ___worldTPAxis.normalized;

                        if (___worldTPAxis.magnitude > normalized.magnitude * 2f)
                            ___worldTPAxis -= normalized * 2f;
                    }

                    GM.Options.MovementOptions.TwinStickSnapturnState = mode;
                }
                // Head Armswinger mode
                else
                {
                    // Handle snap turning
                    miHandUpdateArmSwinger.Invoke(__instance, [hand]);

                    // Get head direction
                    Vector3 headForward = GM.CurrentPlayerBody.Head.forward;
                    headForward.y = 0f;
                    headForward.Normalize();

                    ___worldTPAxis = headForward;
                }

                miAXButtonCheck.Invoke(__instance, [hand]);
                return false;
            }

            return true;
        }

        // Maps movement axis speed to Armswinger base speeds
        // Reference:
        //   0     1     2     3     4     5
        //   0     0.15  0.25  0.5   0.8   1.2   ArmSwingerBaseSpeeMagnitudes
        //   0.7   1.3   1.8   2.6   4     6.5   TPLocoSpeeds
        public static readonly Dictionary<float, int[]> SpeedMap = new()
        {
            { 3.6f,   [5, 5] },
            { 3.0f,   [5, 4] },
            { 2.55f,  [5, 3] },
            { 2.4f,   [4, 4] },
            { 2.175f, [5, 2] },
            { 2.025f, [5, 1] },
            { 1.95f,  [4, 3] },
            { 1.8f,   [5, 0] },
            { 1.575f, [4, 2] },
            { 1.5f,   [3, 3] },
            { 1.425f, [4, 1] },
            { 1.2f,   [4, 0] },
            { 1.125f, [3, 2] },
            { 0.975f, [3, 1] },
            { 0.75f,  [2, 2] },
            { 0.6f,   [2, 1] },
            { 0.45f,  [1, 1] },
            { 0.375f, [2, 0] },
            { 0.225f, [1, 0] },
            { 0f,     [0, 0] },
        };

        // This function manipulates Armswinger settings and button states based on what the movement stick is doing
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateSmoothLocomotion")]
        [HarmonyPrefix]
        public static void Patch_SmoothLocomotionUpdate(FVRMovementManager __instance, Vector3 ___worldTPAxis, bool ___m_isRightHandActive, out HandState __state)
        {
            __state = new HandState();

            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                // TwinStick Arm Sprint mode
                if (Plugin.HeadArmswinger == null || !Plugin.HeadArmswinger.Value)
                {
                    // Don't allow the movement hand to do smooth turning
                    int moveHand = GetMovementHand(__instance);

                    // Since Classic Mode doesn't support smooth turning, switch to Streamlined Mode
                    if (__instance.Hands[0].CMode == ControlMode.Oculus && ___m_isRightHandActive)
                    {
                        __state.coreControlMode = GM.Options.ControlOptions.CCM;
                        __state.isInStreamlinedMode_0 = __instance.Hands[0].IsInStreamlinedMode;
                        __state.isInStreamlinedMode_1 = __instance.Hands[1].IsInStreamlinedMode;
                        __state.touchpadNorthPressed_0 = __instance.Hands[0].Input.TouchpadNorthPressed;
                        __state.touchpadNorthPressed_1 = __instance.Hands[1].Input.TouchpadNorthPressed;
                        __state.touchpadWestPressed = __instance.Hands[moveHand].Input.TouchpadWestPressed;
                        __state.touchpadEastPressed = __instance.Hands[moveHand].Input.TouchpadEastPressed;

                        GM.Options.ControlOptions.CCM = CoreControlMode.Streamlined;
                        __instance.Hands[0].IsInStreamlinedMode = true;
                        __instance.Hands[1].IsInStreamlinedMode = true;
                    }

                    if (__instance.Hands[0].CMode == ControlMode.Index || __instance.Hands[0].CMode == ControlMode.WMR)
                    {
                        __instance.Hands[moveHand].Input.Secondary2AxisWestPressed = false;
                        __instance.Hands[moveHand].Input.Secondary2AxisEastPressed = false;
                    }
                    else if (__instance.Hands[0].IsInStreamlinedMode)
                    {
                        __instance.Hands[moveHand].Input.TouchpadWestPressed = false;
                        __instance.Hands[moveHand].Input.TouchpadEastPressed = false;
                    }

                    // Armswinger buttons
                    ref bool armSwingPressed_0 = ref __instance.Hands[0].Input.BYButtonPressed;
                    ref bool armSwingPressed_1 = ref __instance.Hands[1].Input.BYButtonPressed;

                    if (__instance.Hands[0].IsInStreamlinedMode)
                    {
                        if (__instance.Hands[0].CMode == ControlMode.Index || __instance.Hands[0].CMode == ControlMode.WMR)
                        {
                            armSwingPressed_0 = ref __instance.Hands[0].Input.Secondary2AxisNorthPressed;
                            armSwingPressed_1 = ref __instance.Hands[1].Input.Secondary2AxisNorthPressed;
                        }
                        else
                        {
                            armSwingPressed_0 = ref __instance.Hands[0].Input.TouchpadNorthPressed;
                            armSwingPressed_1 = ref __instance.Hands[1].Input.TouchpadNorthPressed;
                        }
                    }

                    // If the movement stick is active, activate both Armswinger buttons
                    // This causes forward movement based on ArmSwingerBaseSpeed_Left and ArmSwingerBaseSpeed_Right
                    float twinStickSpeed = ___worldTPAxis.magnitude;
                    armSwingPressed_0 = (twinStickSpeed > 0f);
                    armSwingPressed_1 = (twinStickSpeed > 0f);

                    // Save rotation of hand pointers
                    __state.pointerRotation_0 = __instance.Hands[0].PointingTransform.localRotation;
                    __state.pointerRotation_1 = __instance.Hands[1].PointingTransform.localRotation;

                    // Save Armswinger settings
                    __state.baseSpeedLeft = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left;
                    __state.baseSpeedRight = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right;

                    // Only do this if we are moving
                    // worldTPAxis will be between 0 and TPLocoSpeeds[TPLocoSpeedIndex]
                    if (twinStickSpeed > 0f)
                    {
                        // Set hand pointers to direction given by movement stick
                        __instance.Hands[0].PointingTransform.forward = ___worldTPAxis.normalized;
                        __instance.Hands[1].PointingTransform.forward = ___worldTPAxis.normalized;

                        // For regular TwinStick mode, player speed = worldTPAxis.magnitude.
                        // For Armswinger (with no arm movement), player speed = (ArmSwingerBaseSpeeMagnitudes[Left] + ArmSwingerBaseSpeeMagnitudes[Right]) x 1.5.
                        // After adding arm movement, speed maxes out at 11, no matter what the base speed is.
                        foreach (float speed in SpeedMap.Keys)
                        {
                            if (twinStickSpeed > speed)
                            {
                                GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left = SpeedMap[speed][0];
                                GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right = SpeedMap[speed][1];
                                break;
                            }
                        }
                    }
                }
                // Head Armswinger mode
                else
                {
                    // Save Armswinger settings
                    __state.baseSpeedLeft = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left;
                    __state.baseSpeedRight = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right;

                    // Save rotation of hand pointers
                    __state.pointerRotation_0 = __instance.Hands[0].PointingTransform.localRotation;
                    __state.pointerRotation_1 = __instance.Hands[1].PointingTransform.localRotation;

                    // Set hand pointers to direction given by movement stick
                    __instance.Hands[0].PointingTransform.forward = ___worldTPAxis.normalized;
                    __instance.Hands[1].PointingTransform.forward = ___worldTPAxis.normalized;
                }
            }
        }

        // This function restores settings and states that were manipulated earlier
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateSmoothLocomotion")]
        [HarmonyPostfix]
        public static void Patch_SmoothLocomotionUpdateEnd(FVRMovementManager __instance, bool ___m_isRightHandActive, HandState __state)
        {
            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                if (Plugin.HeadArmswinger == null || !Plugin.HeadArmswinger.Value)
                {
                    if (__instance.Hands[0].CMode == ControlMode.Oculus && ___m_isRightHandActive)
                    {
                        // Restore controls used in Streamlined Mode
                        int moveHand = GetMovementHand(__instance);
                        GM.Options.ControlOptions.CCM = __state.coreControlMode;
                        __instance.Hands[0].IsInStreamlinedMode = __state.isInStreamlinedMode_0;
                        __instance.Hands[1].IsInStreamlinedMode = __state.isInStreamlinedMode_1;
                        __instance.Hands[0].Input.TouchpadNorthPressed = __state.touchpadNorthPressed_0;
                        __instance.Hands[1].Input.TouchpadNorthPressed = __state.touchpadNorthPressed_1;
                        __instance.Hands[moveHand].Input.TouchpadWestPressed = __state.touchpadWestPressed;
                        __instance.Hands[moveHand].Input.TouchpadEastPressed = __state.touchpadEastPressed;
                    }
                }

                // Restore rotation of hand pointers
                __instance.Hands[0].PointingTransform.localRotation = __state.pointerRotation_0;
                __instance.Hands[1].PointingTransform.localRotation = __state.pointerRotation_1;

                // Restore Armswinger settings
                GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left = __state.baseSpeedLeft;
                GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right = __state.baseSpeedRight;
            }
        }

        // This function renames the movement mode in the wrist menu
        [HarmonyPatch(typeof(FVRPointableButton), "Awake")]
        [HarmonyPostfix]
        public static void Patch_FixDashName(FVRPointableButton __instance)
        {
            var text = __instance.GetComponent<Text>();

            if (text != null && text.text == "Armswinger")
            {
                if (Plugin.HeadArmswinger == null || !Plugin.HeadArmswinger.Value)
                    text.text = "TS Arm Sprint";
                else
                    text.text = "Head Armswinger";
            }
        }
    }
}
