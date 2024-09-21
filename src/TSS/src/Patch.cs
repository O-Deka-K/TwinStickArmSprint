using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3VRMod
{
    public class Patch
    {
        public static int MovementHand = 0;

        // This function finds the movement hand
        [HarmonyPatch(typeof(FVRMovementManager), "Awake")]
        [HarmonyPrefix]
        public static void Patch_FindMovementHand(FVRMovementManager __instance)
        {
            int rightHand = __instance.Hands[0].IsThisTheRightHand ? 0 : 1;
            MovementHand = (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove) ? rightHand : 1 - rightHand;
        }

        // This is used by controllers that do not have an analog stick, so they use the touchpad instead
        [HarmonyPatch(typeof(FVRMovementManager), "ShouldFlushTouchpad")]
        [HarmonyPostfix]
        public static void Patch_ShouldFlushTouchpad(FVRMovementManager __instance, FVRViveHand hand, ref bool __result)
        {
            if (!hand.IsInStreamlinedMode && hand.CMode != ControlMode.Index && hand.CMode != ControlMode.WMR && __instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                bool isTurningHand = hand.IsThisTheRightHand;

                if (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove)
                    isTurningHand = !isTurningHand;

                if (__instance.m_isLeftHandActive && !isTurningHand)
                    __result = true;

                if (__instance.m_isRightHandActive && isTurningHand)
                    __result = true;
            }
        }

        // This function gets the movement axis velocity
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateMovementWithHand")]
        [HarmonyPrefix]
        public static bool Patch_HandMovementUpdate(FVRMovementManager __instance, FVRViveHand hand)
        {
            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                bool westDown = __instance.Hands[MovementHand].Input.Secondary2AxisWestDown;
                bool eastDown = __instance.Hands[MovementHand].Input.Secondary2AxisEastDown;

                // Handle snap turning
                // Don't allow the movement hand to do snap turning
                __instance.Hands[MovementHand].Input.Secondary2AxisWestDown = false;
                __instance.Hands[MovementHand].Input.Secondary2AxisEastDown = false;
                __instance.HandUpdateArmSwinger(hand);
                __instance.Hands[MovementHand].Input.Secondary2AxisWestDown = westDown;
                __instance.Hands[MovementHand].Input.Secondary2AxisEastDown = eastDown;

                // Ignore TwinStick turn mode
                var mode = GM.Options.MovementOptions.TwinStickSnapturnState;
                GM.Options.MovementOptions.TwinStickSnapturnState = MovementOptions.TwinStickSnapturnMode.Disabled;
                __instance.HandUpdateTwinstick(hand);
                GM.Options.MovementOptions.TwinStickSnapturnState = mode;

                __instance.AXButtonCheck(hand);
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
            { 3.6f,   new int[] {5, 5} },
            { 3.0f,   new int[] {5, 4} },
            { 2.55f,  new int[] {5, 3} },
            { 2.4f,   new int[] {4, 4} },
            { 2.175f, new int[] {5, 2} },
            { 2.025f, new int[] {5, 1} },
            { 1.95f,  new int[] {4, 3} },
            { 1.8f,   new int[] {5, 0} },
            { 1.575f, new int[] {4, 2} },
            { 1.5f,   new int[] {3, 3} },
            { 1.425f, new int[] {4, 1} },
            { 1.2f,   new int[] {4, 0} },
            { 1.125f, new int[] {3, 2} },
            { 0.975f, new int[] {3, 1} },
            { 0.75f,  new int[] {2, 2} },
            { 0.6f,   new int[] {2, 1} },
            { 0.45f,  new int[] {1, 1} },
            { 0.375f, new int[] {2, 0} },
            { 0.225f, new int[] {1, 0} },
            { 0f,     new int[] {0, 0} },
        };

        public class HandState
        {
            public Quaternion pointerRotation_0;
            public Quaternion pointerRotation_1;
            public int baseSpeedLeft;
            public int baseSpeedRight;
        }

        // This function manipulates Armswinger settings and button states based on what the movement stick is doing
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateSmoothLocomotion")]
        [HarmonyPrefix]
        public static void Patch_SmoothLocomotionUpdate(FVRMovementManager __instance, out HandState __state)
        {
            __state = new HandState();

            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                // Don't allow the movement hand to do smooth turning
                __instance.Hands[MovementHand].Input.Secondary2AxisWestPressed = false;
                __instance.Hands[MovementHand].Input.Secondary2AxisEastPressed = false;

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
                float twinStickSpeed = __instance.worldTPAxis.magnitude;
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
                    __instance.Hands[0].PointingTransform.forward = __instance.worldTPAxis.normalized;
                    __instance.Hands[1].PointingTransform.forward = __instance.worldTPAxis.normalized;

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
        }

        // This function restores settings and states that were manipulated earlier
        [HarmonyPatch(typeof(FVRMovementManager), "UpdateSmoothLocomotion")]
        [HarmonyPostfix]
        public static void Patch_SmoothLocomotionUpdateEnd(FVRMovementManager __instance, HandState __state)
        {
            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
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
                text.text = "TS Arm Sprint";
            }
        }
    }
}