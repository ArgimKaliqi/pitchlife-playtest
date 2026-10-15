# Pitchlife — P01 public playtest

A small Unity football sandbox for playtesting shooting, passing and ball contact. This repository is a clean source snapshot: it contains the playable sandbox rather than the private planning repository or its history.

**Branch:** `playtest/strike-contact`

**Editor:** Unity 6.3 LTS, `6000.3.5f2`

**Project folder:** [`game/Pitchlife.Unity`](game/Pitchlife.Unity)

## Open and play

1. Clone or download this branch.
2. Install Unity Hub and Editor **6000.3.5f2**. Android Build Support with SDK/NDK/OpenJDK is needed only for Android builds.
3. In Hub, choose **Add project from disk** and select `game/Pitchlife.Unity`.
4. Allow package import to finish, then open `Assets/_Pitchlife/Scenes/P01FootballSandbox.unity`.
5. Press **Play** and click inside the **Game** view. The camera and pitch are created when Play starts, so "No cameras rendering" before Play is expected.

The project includes its scene, URP rendering assets, tuning data, package lock and Unity metadata. Internet access is required for the initial package download. If setup needs repairing, save the current scene and use **Tools > PITCHLIFE > Set Up P01 Sandbox**.

## Controls

- Select **Shot**, **Ground pass** or **Lofted pass**.
- **Swipe/drag upward** to set power; sideways movement sets the initial aim.
- **Release** to open the football close-up. The kick waits until you click/tap a point on the ball.
- **Center:** straight, driven shot with no initial sidespin.
- **Below center:** higher chip and backspin.
- **Above center:** lower shot and topspin/dip.
- **Left side:** curves right. **Right side:** curves left.
- Clicking outside the ball does nothing. **Back to aiming** cancels the pending kick.
- **R / Reset:** restore the ball and players. Resolved actions reset automatically after two seconds.
- **Replay last:** restore the last starting positions and strike parameters without adding another attempt or goal.
- **Opponents on/off:** toggle the defender and goalkeeper for practice.

Blue is the receiving teammate, red the defender and yellow the goalkeeper. A yellow line previews the initial trajectory while aiming; it does not predict the later contact adjustment, bounces or collisions.

## What to test

- Does the swipe direction and strength match your intention?
- Can you learn the difference between center, low, high and side contacts?
- Can you pass to the blue teammate, with and without opponents?
- Do the goalkeeper and defender move plausibly?
- Does Reset always recover a clean starting state? Does Replay preserve the kick's curve?
- Does the close-up remain readable at your Game view/window size?

When reporting a problem, include the action and contact point, Unity version, a screenshot and the Console error text. Avoid posting account credentials or unrelated logs.

## Validation and limitations

Validated with Unity `6000.3.5f2`: **20 EditMode + 19 PlayMode tests passed**. Coverage includes swipe/contact sequencing, chips/topspin/sidespin, ball physics, passing, outcomes, replay, and recovery of actor references after script reloads. Run the suites through **Window > General > Test Runner**.

This is a gameplay prototype with placeholder players. The kick model combines contact-derived torque, tuned launch elevation and bounded Magnus acceleration; it is not a calibrated simulation of foot deformation or full aerodynamics. Physical Android performance and touch feel still need device testing.

No prebuilt app is included in this source-only playtest. **Tools > PITCHLIFE > Build Android Test APK** creates an ARM64 development APK under `Builds/Android/`.

Tuning lives in `Assets/_Pitchlife/Settings/FootballTuning.asset`. `P01FootballSandbox.AttemptFinished` exposes each action's outcome, strike point and launch velocities for later gameplay integration.
