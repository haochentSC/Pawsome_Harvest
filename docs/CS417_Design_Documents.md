### **Pitch**

You are a "Healer" of a small sick bunny in the house. Your mission is to provide nursing and emotional care for the bunny to recover its health and eventually open its heart to you so that you can carry the bunny to show beautiful scenery around the home.

You are a gardener taking care of a group of plants in pots. Your mission is to plant and grow plants and harvest plants to gain money. Repeat this process to gain enough money to play the lottery machine. The lottery machine provides rewards for the planting system, nursing system, as well as cards (or other items) for collection.

### **World**

A cozy home rendered in a soft, pastel tone. There is progressively beautiful scenery around the house as the game proceeds, and the interior is filled with fluffy bedding and cute furniture for the bunny and the caregiver (the game player).

A garden for the player to do the planting. Near the garden is a home. There is also a lottery machine near the garden. In the garden, there is an inventory for the player to select and pick things.

### **Genre**

The game is a cozy Role-Playing Game (RPG) focused on character (bunny) growth (in this game, healing) and emotional interaction with the bunny and the caregiver (the game player). It is also a simulation of farming with an unstable reward system.

### **Inspirations**

* **Tamagotchi:** the real-time status monitoring of a character and the necessity of consistent care.  
* **Animal Crossing:** the peaceful atmosphere, comforting visual aesthetics, and the gentle pace of interaction between animals and humans.  
* **Princess Maker:** a classic life simulation RPG where the player takes on the role of a guardian, raising a young girl via various activities and choices to shape her future as bright as possible.  
* **Stardew Valley:** an open-ended country-life RPG where the player, armed with hand-me-down tools and a few coins, experiences a country life.

### **Required Mechanics**

1. **Vital Sign Monitoring:** a system that tracks the bunny’s temperature, hunger, and stress levels.  
2. **Healing Interaction:** a mechanic using mouse clicks and drags to feed pills and food that mitigates hunger level of the bunny.  
3. **Bonding Interaction:** a mechanism where, petting, and hugging for emotional care to placate the stress level of the bunny.  
4. **Farming Interaction:** a mechanism where seeding, planting, and harvesting to earn money for purchasing items, upgrading, and drawing rewards from the lottery machine.  
5. **Lottery Interaction:** a mechanic using VR interaction to a lever to draw items with cost of money.

### **Core Loop Schedule**

1. **Observe (Check Status):** The player first checks the bunny’s vital signs to identify what it needs (e.g., hunger and stress levels). The player in the garden checks what plants they can plant as well as the current property.  
2. **Act:** The player performs a specific task listed in the mechanics, such as feeding a carrot or petting. The player performs seeding in the garden and takes care of the plants. If the player has enough money, they can choose to play the lottery machine to gain rewards for planting or caring for the bunny.   
3. **Feedback:** The rabbit reacts with various cute icons, providing immediate emotional reward and recovery of vital signs. The plants grow and can be harvested as converting to money shown on board as increase of number. 

### **Meta Loop Schedule**

1. **Environment Customization:** Reaching health milestones unlocks better environmental decorations around the home, expanding the visual world more beautifully. Spending money to upgrade fertilizer level to increase the growth speed of plants. Reaching a certain level of money unlocks trophies.   
2. **The Recovery:** the goal of this game is the "Recovery" state, where the bunny is healthy enough to see the beautiful scenery around the home with the caregiver.  
3. **The Collection:** one goal of the game is to gain a collection of special items from the lottery machine.  
4. **The Wealth:** one goal of the game is gaining a certain amount of money to achieve all trophies.

### **Summary**

Tiny Greenhouse XR is a cozy VR idle simulation set in a single greenhouse room. The player tends to potted plants that passively generate money, cares for a sick bunny whose vital signs decay over time, and spends earnings at a lottery machine for cross-system rewards and collectible items. From moment to moment, the player checks on their bunny's hunger and stress, feeds and pets it to build bonding, plants seeds, harvests mature crops, buys upgrades to multiply earnings, and pulls the lottery lever for a chance at seed packets, pet food, temporary boosts, or rare collectibles. Over the course of the game, the three systems reinforce each other: a well-bonded bunny doubles harvest income, farming funds lottery tickets, and lottery rewards feed back into planting and pet care. The experience ramps from a simple planting tutorial to a multi-system economy with trophies, collectibles, and environmental upgrades as long-term goals.

---

## Technical Design Document

### **Required Mechanics Plan**

1. **Farming Interaction** — *Owner: [Planting Teammate]*
   - **Resource Simulation** (2pts): Two Euler-integrated resources (money, fertilizer) tick every 1 second. Money rate = baseRate x activePots x soilMultiplier x lightBonus. Visible HUD displays both resources.
   - **Leveling-up Stats** (3pts): Three upgrade tracks (SoilQuality, GrowLights, Irrigation) each with 3 levels. Purchasing an upgrade exchanges money for a stat increase that enters the money rate calculation.
   - **Leveling to New Interactions** (2pts): Spending 500 money unlocks the Fertilizer Station, a new interactable that enables the second resource system.
   - **Ramping Difficulty** (2pts): Upgrade costs escalate per level (Soil: $50/$150/$400; Lights: $75/$200/$500; Irrigation: $100/$250/$600). Seed cost = 10 x 1.15^seedsBought. UI shows increased cost to next level.
   - *Subtotal: 9pts*

2. **Vital Sign Monitoring** — *Owner: [Pet Care Teammate]*
   - **Resource Simulation** (3pts): Three additional Euler-integrated resources — hunger (decays 2/s), stress (grows 1/s when hungry), bonding (passive gain when well-fed and low stress). World-space bars display all three above the bunny.
   - **Poke Interactor Buttons** (shared): "Check Vitals" button toggles the PetVitalsDisplay showing hunger, stress, and bonding levels.
   - *Subtotal: 3pts + shared*

3. **Healing Interaction** — *Owner: [Pet Care Teammate]*
   - **Conditional Despawning** (2pts): Feeding the bunny consumes a food item from the player's inventory (food item is removed from scene/inventory).
   - **Juicy Feedback** (included in overall tally): Heart particles burst from bunny, haptic pulse on feeding hand, spatialized munching sound.
   - *Subtotal: 2pts + feedback*

4. **Bonding Interaction** — *Owner: [Pet Care Teammate]*
   - **Points Scoring** (2pts): Bonding level (0-100) acts as a persistent score. Each petting interaction awards +5 bonding points. A visible element (bonding bar) tracks progress.
   - **Combo Streak** (2pts): Consecutive successful feeds without hunger hitting zero increases a streak counter that multiplies bonding gain per interaction.
   - *Subtotal: 4pts*

5. **Lottery Interaction** — *Owner: [Lottery Teammate]*
   - **Collectibles** (2pts): 10 unique collectible items can be won from the lottery. A collectible shelf tracks how many have been collected. Each collectible is individually obtained through the spin interaction.
   - **Copier** (3pts): Each lottery spin dynamically instantiates a new reward object with Rigidbody that flies out of the machine. No cap on copies — every spin creates a new instance.
   - **NPC Spawner** (2pts): Reward objects are spawned dynamically for the player to collect.
   - **Loot Drop** (2pts): Upon collecting a reward object, the player acquires resources (money, seeds, pet food, or boosts).
   - *Subtotal: 9pts*

### **Alpha Features Plan**

**[Planting Teammate] — 8pts additional:**
- **Inter-session Saves** (3pts): All game state (money, pot states, upgrade levels, pet vitals, lottery collectibles) is saved to JSON between sessions via SaveManager.
- **Idle Progress** (3pts): On reload, elapsed time is calculated and money/fertilizer earned at 50% efficiency (capped at 8 hours). "Welcome back! +X coins" popup displayed.
- **Restart Option** (1pt): A UI button resets all game state to initial values.
- **Quit Option** (1pt): A UI button exits the application.

**[Pet Care Teammate] — 8pts additional:**
- **Path Following** (2pts): The bunny follows a pre-defined idle walk path around its home area when not being interacted with.
- **Integrating 3D Meshes** (2pts): Bunny model, bed/cushion, food items (carrot, medicine bottle) imported and integrated into the scene.
- **Juicy Feedback** (4pts): 11 additional feedback triggers — heart particles (feed + pet), haptics (feed + pet), spatialized sounds (munch, purr, sad), warning particles on critical hunger, bonding milestone scale-pop, combo streak star particles, mood expression swap.

**[Lottery Teammate] — 8pts additional:**
- **Secrets** (2pts): A hidden interaction on the lottery machine (pressing a specific sequence) reveals an Easter egg collectible not in the normal reward table.
- **Ramping Difficulty** (2pts): Lottery ticket cost escalates: $25 base + $5 per 10 spins (cap $100). Display shows increasing cost.
- **Integrating 3D Meshes** (2pts): Lottery machine model, collectible item models (cards, figurines) imported and integrated.
- **Juicy Feedback** (2pts): 10 additional feedback triggers — haptic on lever pull, spatialized spin/win/lose sounds, star particles by rarity, EaseScale on collectible reveal, coin particles on reward spawn, boost aura particles.

### **Stretch Features Plan**

These are unassigned features we would like to include if time permits:
- **Teleportation** (1pt): Point-and-click teleportation between the garden, home/pet area, and lottery zone with a landing preview visualization.
- **Character Cosmetics & Customization** (2pts): Exchange money or lottery collectibles to change the appearance of the player's VR hands.
- **Mirrors** (2pts): A mirror object in the home area using a Camera-to-RenderTexture setup so the player can see themselves.
- **Sortable Inventory** (2pts): An expanded inventory modal for managing seeds, food items, and collectibles with drag-to-sort.
- **Path Planning** (4pts): Bunny dynamically pathfinds toward the player when bonding is high using NavMesh.

### **Collaboration Plan**

The team meets twice weekly on Discord (voice channel) for stand-ups and code review. All code is coordinated through GitHub with feature branches per system (`feature/planting`, `feature/pet-care`, `feature/lottery`). The planting system is the foundation — pet care and lottery both depend on `EconomyManager` being stable, so the planting owner completes prompts 5-12 first while teammates plan their systems. Pet care and lottery development happens in parallel during weeks 2-3. Cross-system integration (wiring harvest bonus, lottery rewards, shared save) happens in week 3 after individual systems are functional. Each teammate owns their feature branch and creates pull requests for review before merging to main. Merge conflicts are resolved together during Discord sessions.

