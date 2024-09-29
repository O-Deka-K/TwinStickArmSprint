# TwinStick Arm Sprint

This is based on Potatoes' Twin Stick Swinger (https://github.com/potatoes1286/TwinStickSwinger). The main difference is that swinging your arms _only increases speed_, without affecting direction. It's similar to movement in Blade & Sorcery.

There's also another mode called "Head-Based Armswinger" that's like regular Armswinger (no TwinStick), except that it uses head-based direction instead of controller-based direction. You will always go in the direction that you're facing.

![Sosig running](https://i.imgur.com/ZLGeAgO.png)

### NOTE: REPLACES ARMSWINGER MOVEMENT MODE

Due to issues caused by replacing Dash movement mode, this mod replaces Armswinger mode instead. It uses a combination of TwinStick options and Armswinger options.

## For TwinStick Arm Sprint mode, the following settings are respected

- TwinStick Options:
  - Movement Speed
  - Controller Forward/Side Root
  - TwinStick Left/Right Handedness
  - TwinStick Jump
- ArmSwinger Options:
  - ArmSwinger Jump
  - _**ArmSwinger Turning Mode**_

You can also use the Global Movement Options, like setting the buttons to turn or jump.

## For TwinStick Arm Sprint mode, the following settings are _ignored_

- TwinStick Options:
  - _**TwinStick Turning Mode**_
  - TwinStick Sprint Mode
  - TwinStick Sprint Toggle Mode
- ArmSwinger Options:
  - ArmSwinger Base Speed (Left Hand)
  - ArmSwinger Base Speed (Right Hand)

## For Head-Based Armswinger mode, it only respects _ArmSwinger_ options

## TwinStick Arm Sprint Usage

1. Go to Wrist menu > Set Move Mode > **TS Arm Sprint**.
2. Use the analog stick (or touchpad on some controllers) on the movement hand to move around as in TwinStick mode.
3. Swing your arms to sprint. It doesn't matter which way you swing them. The faster you move, the faster you go!

## Head-Based Armswinger Usage

1. Change modes as shown below in the Config section.
2. Go to Wrist menu > Set Move Mode > **Head Armswinger**.
3. Look in the direction you want to go. Press the ArmSwinger button(s) to move and swing your arms to speed up.

## Notes

- Use _**ArmSwinger Turning Mode**_ to control the turning mode.
- If you have ArmSwinger Jump enabled, raise both controllers above your head to jump.
- (TwinStick Arm Sprint only) If you have TwinStick Jump enabled, press down on the analog stick on the offhand to jump.
- (TwinStick Arm Sprint only) Your max speed with the analog stick is set by the _**TwinStick Movement Speed**_ option.

## Config

- Default is TwinStick Arm Sprint mode.
- The game must be run at least once with the mod enabled in order for the config file to be created.
- To change modes, edit the config file. You can use the mod panel in the game, or edit `dll.h3vr.odekak.twinstickarmsprint.cfg` in the Config folder. R2modman has a "Config editor" option.

## Fixed Issues

Several bugs were fixed compared to Twin Stick Swinger. Jump now works fine, whether you use TwinStick jump or Armswinger jump. Also, there's no awkward momentum issue when going down stairs.

## Credits

potatoes1286 - For making a great mod and for sharing the code on GitHub
