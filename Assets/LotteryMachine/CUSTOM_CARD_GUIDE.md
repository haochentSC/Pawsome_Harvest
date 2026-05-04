# Custom Card Guide

This guide explains how to make your own lottery card reward and add it to the lottery machine.

The recommended workflow is to duplicate one of the sample cards, customize it in the Unity Editor, then create a `RewardDefinition` asset that points to your new prefab.

## Recommended Files

For a card named `MyCard`, use names like these:

- `Assets/LotteryMachine/Sample/Images/MyCardIcon.png`
- `Assets/LotteryMachine/Sample/Materials/Card_MyCard.mat`
- `Assets/LotteryMachine/Sample/Materials/MyCardIcon.mat`
- `Assets/LotteryMachine/Sample/Prefabs/MyCardCard.prefab`
- `Assets/LotteryMachine/Sample/Rewards/MyCard.asset`

You can use different folders if you want, but keeping custom card assets near the sample assets makes references easier to inspect.

## Make The Card Prefab

1. In the Project window, open `Assets/LotteryMachine/Sample/Prefabs`.
2. Duplicate a card prefab that is closest to what you want, such as `AquapupCard.prefab` or `FlamelingCard.prefab`.
3. Rename the duplicate to your card name, for example `MyCardCard.prefab`.
4. Open the prefab in Prefab Mode.
5. Rename the root object to match the prefab name.
6. Update the root card material on `CardBack` or create a new material like `Card_MyCard.mat`.
7. Update the text objects:
   - `Name`: the card name.
   - `Rarity`: `Common`, `Uncommon`, `Rare`, `Epic`, or `Legendary`.
   - `Flavor`: a short description.
   - `Stats`: short stat text, such as `HP 80`.
8. Update `CreatureIcon`:
   - For a text icon, keep the TextMesh Pro component and change the text.
   - For an image icon, import a PNG under `Sample/Images`, create an unlit material using that texture, and assign it to a flat quad like the Aquapup and Flameling cards.
9. Adjust scale and position so the card still fits inside the reveal tray.

The root prefab should have these components:

- `BoxCollider`
- `Rigidbody`
- `XRGrabInteractable`
- `GrabbableReward`

The lottery machine will try to add missing grabbable components when it spawns a reward, but keeping them on the prefab makes the card easier to test directly.

## Create The RewardDefinition

1. In the Project window, open `Assets/LotteryMachine/Sample/Rewards`.
2. Right-click and choose `Create > Lottery Machine > Reward Definition`.
3. Name the asset after your card, for example `MyCard.asset`.
4. Fill the fields in the Inspector:
   - `rewardId`: a unique lowercase id, such as `my_card`.
   - `displayName`: the player-facing name, such as `MyCard`.
   - `rarity`: the rarity enum value.
   - `weight`: the draw weight.
   - `cardArt`: optional sprite art for your own UI.
   - `cardMaterial`: the card material, such as `Card_MyCard.mat`.
   - `rewardPrefab`: your card prefab, such as `MyCardCard.prefab`.

A reward is considered drawable only when `rewardId` is not empty and `weight > 0`. The `rewardPrefab` is still needed for the machine to spawn a visible card.

## Make Your Own Reward Pool

Use a reward pool when you want the lottery machine to draw from your own cards or prizes.

1. Create one `RewardDefinition` asset for each reward you want the machine to draw.
2. In the Project window, open the folder where you want to keep the pool, such as `Assets/LotteryMachine/Sample/Rewards`.
3. Right-click and choose `Create > Lottery Machine > Reward Pool`.
4. Name the asset clearly, for example `MyRewardPool.asset`.
5. Select the new pool asset.
6. In the Inspector, set the `Rewards` list size to the number of rewards you want in the pool.
7. Drag each `RewardDefinition` asset into a list slot.
8. Make sure every reward in the list has a non-empty `rewardId`, a `weight` greater than `0`, and a `rewardPrefab`.
9. Select the lottery machine prefab instance in your scene.
10. On the `LotteryMachine` component, assign your custom pool to `Reward Pool`.
11. To customize capsule-open audio, assign your own clip to `Reward Reveal Sound`.
12. Press Play and use the lottery machine.

Draw odds are relative to the total drawable weight. For example, if the pool contains weights `40`, `40`, and `20`, those rewards have about `40%`, `40%`, and `20%` odds.

You can reuse one pool across multiple lottery machine instances, or create separate pools for different machines.

To use an existing sample pool:

1. Open `Assets/LotteryMachine/Sample/Rewards/PhokemonRewardPool.asset`.
2. In the Inspector, increase the `Rewards` list size by one.
3. Drag your new `RewardDefinition` asset into the new list slot.
4. Press Play and use the lottery machine. Your card can now be drawn.

## Metadata And Version Control

Unity creates `.meta` files for assets such as prefabs, materials, images, and reward assets. These files store GUIDs, and Unity uses those GUIDs to keep references connected.

Keep each `.meta` file with its asset in version control:

- `MyCardCard.prefab` and `MyCardCard.prefab.meta`
- `MyCard.asset` and `MyCard.asset.meta`
- `Card_MyCard.mat` and `Card_MyCard.mat.meta`
- `MyCardIcon.png` and `MyCardIcon.png.meta`

Do not hand-edit `.meta` files unless you know exactly why. Avoid deleting or regenerating `.meta` files after an asset is referenced by a reward definition, material, prefab, or reward pool, because that can break links in the Inspector.

## Testing Checklist

- The prefab opens without missing script warnings.
- The card looks correct in Prefab Mode.
- The root object has collider, Rigidbody, `XRGrabInteractable`, and `GrabbableReward` components.
- The `RewardDefinition` has a non-empty `rewardId`, a positive `weight`, and the correct `rewardPrefab`.
- The reward definition appears in the active `RewardPool`.
- The lottery machine's `Reward Pool` field points to the pool you want to use.
- Pressing Play and drawing from the machine can spawn the card.

If you run `Tools > Lottery Machine > Build Sample Content`, the builder creates the machine content only. It does not create or assign a reward pool, so assign your custom pool to the machine afterward.
