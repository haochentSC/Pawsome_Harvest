# Lottery Machine

Reusable VR-first lottery reward module.

## What Is Included

- `Sample/Prefabs/LotteryMachine.prefab`: drag-and-drop lottery machine.
- `Sample/Rewards/PhokemonRewardPool.asset`: ten fictional Phokemon card rewards with weighted odds.
- `Sample/Prefabs/*Card.prefab`: physical card rewards spawned into the tray.
- `Sample/Prefabs/RewardDisplayBoard.prefab`: standalone 10-slot display board for keeping one copy of each card type.
- `Scripts/RewardDefinition.cs`: reward data asset.
- `Scripts/RewardPool.cs`: weighted reward pool.
- `Scripts/LotteryMachine.cs`: draw flow, capsule reveal feedback, and reward result events.
- `Scripts/RewardCardInstance.cs`: runtime reward identity attached to spawned cards.
- `Scripts/RewardDisplayBoard.cs`: fixed-slot display board that snaps new cards and discards duplicates.
- `Scripts/RewardDisplaySlot.cs`: XR socket filtering/forwarding plus mouse-drag trigger fallback for display board slots.
- `Scripts/LotteryLeverXrInteractable.cs`: XR Interaction Toolkit select support for the pull lever.
- `Scripts/GrabbableReward.cs`: makes spawned rewards grabbable in VR and draggable with the mouse for editor testing.

## Reusing In Another Game

1. Create `RewardDefinition` assets for your game's rewards.
2. Put those rewards into a `RewardPool`.
3. Place `LotteryMachine.prefab` in your scene.
4. Optionally place `RewardDisplayBoard.prefab` in your scene as a separate object.
5. Assign your `RewardPool` to the prefab's `LotteryMachine` component.
6. Listen to `RewardCompletedEvent` or the C# `RewardCompleted` event to grant the reward in your own inventory/game system.

The machine spawns the won reward prefab in its tray as a grabbable scene object, with a short gold particle burst and reward sound when the capsule opens. Replace `Reward Reveal Sound` on the `LotteryMachine` component to use your own audio. The sample cards include a root collider, Rigidbody, `XRGrabInteractable`, and `GrabbableReward`. The host game still owns persistence, inventory, currency, and progression.

If the player draws again, previously generated rewards remain in the scene. Multiple rewards may occupy the tray until the player moves them.

The sample display board exists as its own `RewardDisplayBoard` prefab with one fixed slot for each sample card type. In VR, bring a spawned card near any display slot socket and release it; the board routes the card to its matching fixed position. In the editor, mouse-dragging a card onto the board still works as a fallback. If that card type is already on the board, the duplicate card is discarded.

## XR Setup

The project includes XR Interaction Toolkit and OpenXR packages. The lever uses `LotteryLeverXrInteractable`, so any XR interactor that can select an `XRSimpleInteractable` can trigger the draw.

For a specific headset, import or configure the matching XR controller/action presets, then make sure the controller interactor can reach the `PullLever` collider. The lever also supports mouse click in the editor for quick testing.

Drawn cards can be selected with an XR interactor. In the editor, press Play and drag a revealed card with the mouse for a quick non-headset check.
