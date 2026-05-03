This assignment is graded off of individual contributions. Like our previous MP submissions, you will upload a video playthrough of your game talking through features implemented for points on the rubric. Unlike previous submissions you will show only the features you personally completed. This is to reward students who want to go harder on their free game without requiring other students to match their pace. However, there is a multiplier for how many teammates you successfully collaborated with and there are also extra points for doing the work of collaboration: iterating on design plans, reviewing each other's code, fixing merge conflicts. For these points you should include screen capture of your Google docs and Github comments respectively and talk through how many of the points you believed you've earned.

Github is mandatory for this project. If your collaborators did not push any commits to your Github they will not be counted towards the below multipliers:

* If you are the only code contributor on your github, all your story points are multiplied by x0.75  
* If there are 2-3 code contributors on your github, all your story points are multiplied by x1.5  
* If there are 4-6 code contributors on your github, all your story points are multiplied by x2  
* If there are 7 or more contributors on your github, all your story points are multiplied by x2.5

You and your team will design and develop any VR game you desire. Our goal is to assess your mastery of game development techniques. Accordingly we will count visible features demonstrated in your personal video to track how many games systems you have mastered. If there is a game system you want to demonstrate your skills with, message the professor or your coach on Piazza or email and we will consider adding it to this rubric for points.

As an open-ended game design project, all features are optional features. You must assemble your own roster of story points that you will contribute to your project which will become your individual grade for this assignment (after teamsize multipliers). You must have implemented them yourselves personally and demonstrate it in your own video. If you worked on a feature with another teammate (e.g. by pair programming or each contributing commits to a feature branch on Github) please list their names next to the feature and we will split the points amongst you. If you and another teammate both claim the same feature and do not list each other in this way, you will lose these points.

## Game Features Story Points

Below are *User Stories* corresponding to features and programmable elements you can deliver in your game. As user stories, these are likely to change as we get feedback (in this case from you the students), so check back here for updates on rubric definitions as they evolve. So too, if there is a game feature you want to implement that requires work not reflected in these user stories, please reach out to the professor or teaching staff with your proposed story and we will account for how many points it may be worth.

The stories are divided into sections for easy navigation

### Scoring:

* Win Condition Scoreboard (2pts): A visible element in the environment tracks the player's progress towards the win condition or other notion of success in the game  
* Collectibles (2pts): there are objects in the environment that the player can collect individually (either through colliding with them or an interaction). A visible element tracks how many collectibles have been collected  
* Inventory:  
  * Hotswap Modal (2pts): pressing a button on the Oculus controllers reveals a display "modal" of several items the player can choose between (e.g. by scrolling or by grabbing the items from the revealed palette). This "modal" should not be visible otherwise  
  * Sortable Inventory (requires the Hotswap Modal story) (2pts): the inventory can be further expanded to show items not on the more quickly-accessed scroll. Items in this expanded inventory can be moved into a position on the more quickly-accessed Hotswap Modal  
  * Bag of Holding (requires the Sortable Inventory) (2pts): there are objects in the environment that can be collected and appear in the Inventory. The player can also choose to drop the items from the inventory back into the world.  
  * Spatial Inventory (requires the Sortable Inventory story) (2pts): every item in the inventory has its own position and size in the inventory. The player can grab and drag items from one location to another to rearrage and sort their inventory. There is a finite size to the inventory that challenges the player to manage the acquisition of items  
* Goal Zones (2pts): there are goal spaces in the environment the player needs to reach to progress  
* Loss Timer (1pts): a countdown timer is displayed on screen and when it drops to 0 the player loses

### Juice:

* Juicy Feedback (1 feedback \= 1pts, 2 feedbacks \= 2pts, 3 feedbacks \= 3pts, 5 feedbacks \= 4pts, 8 feedbacks \= 5pts, 13 feedbacks \= 6pts, 21 feedbacks \= 7pts): interactions provide feedback to the player of what their action has accomplished, and simulated events highlight their unfolding to draw the player's attention. Options for feedback are:  
  * Spatialized Sound Effects: a sound effect plays upon the event. It is spatially located to draw attention to what's changed  
  * Particle Bursts: a non-looping burst of particles is played upon the event.  It is spatially located to draw attention to what's changed  
  * Particle Emission Rate Tracking (requires the Resource Simulation story): the particle emission rate is set to match the growth rate of a resource  
  * Motion Eases: a script-driven motion eases in and out on an object to draw attention to what's changed  
* Integrating 3D meshes (3 integrations \= 1pts, 5 integrations \= 2pts, 8 integrations \= 3pts): a number of bespoke meshes are imported into Unity and integrated into the scene, visible in the video (hint: find models on [Sketchfab](https://sketchfab.com/3d-models/popular)  
* [Links to an external site.](https://sketchfab.com/3d-models/popular)  
*  and use the [Sketchfab Unity Importer](https://sketchfab.com/blogs/community/new-sketchfab-plugin-for-unity-allows-model-download-at-runtime/)  
* [Links to an external site.](https://sketchfab.com/blogs/community/new-sketchfab-plugin-for-unity-allows-model-download-at-runtime/)  
* )  
* Triggering integrations (1 trigger \= 1pt, 2 triggers \= 2pts, 3 triggers \= 3pts, 5 triggers \= 4pts, 8 triggers \= 5pts, 13 triggers \= 6pts, 21 triggers \= 7pts)  
  * Animation: an event triggers a unique non-looping animation to play  
  * Music: moving to a new zone or level triggers a unique music cue to play  
* Expressive Face (2pts): there is a face in the environment whose eyes track the player's movements and reacts positively or negatively to the player's progress  
* Stylized Shaders:  
  * Toon Shaders (2pts): The diffuse and specular lighting is passed through some kind of stepped function to create hard lines between shadow and light to stylize the shading into something analogous to hand-drawn cartoons  
  * Solar Coaster (2pts): Motion is simulated from one end of a mesh strip by an offset on the UV coordinates of the texture map that increases over time  
  * Solar Blast (requires the Solar Coaster story) (2pts): A blast like effect is animated by scrolling the UV coordinates over a circular mesh  
  * UV Distortion (2pts): wiggling, flapping, or blobby jiggling is animated on a texture by scrolling a second noise texture and using the noise to displace the UV coordinates of the first texture

### Interactables:

* Grab Interactables (1 prop \= 1pts, 3 props \= 2pts): there are objects with Grab Interactables (called "props") and Direct Interactors on the player hands  
* Poke Interactor Buttons (1 button \= 1pts, 2 buttons \= 2pts): there are buttons or sliders that can be interacted with by a Poke Interactor on the player hands (for  
* [example](https://youtu.be/bts8VkDP_vU?si=4LZHQsulRsMa9_wG)  
* [Links to an external site.](https://youtu.be/bts8VkDP_vU?si=4LZHQsulRsMa9_wG)  
* [![][image1]](https://youtu.be/bts8VkDP_vU?si=4LZHQsulRsMa9_wG)  
* )  
* Hit Boxes (1 hit box event \= 1 pts, 2 hit box events \= 2pts): there are scripts triggered by a particular collider intersecting with particular objects  
* Copier (3pts): an interaction dynamically instances new copies of an object with a physics rigidbody (e.g. paper clips from stapler, projectiles from a cannon, copies of objects placed on copier machine). There is no cap on how many copies you can make (that is, it isn't just moving some pre-existing objects)  
* Prop Verbs (requires the Grab Interactable story) (2 Prop Verbs \= 1pts, 3 Prop Verbs \= 2pts, 4 Prop Verbs \= 3pts, 6 Prop Verbs \= 4pts, 9 Prop Verbs \= 5pts): pressing a button on the controller creates an effect in the scene, but only when a certain Grab Interactble is held in the controller's corresponding hand  
* (5pts) Slice and dice: player can draw a line on an object which slices the object in half. Slicing must generate two independent objects where there was originally only one. The new objects' geometry should follow the line the player drew

### Progression:

* Tutorial Progression (2pts): the game starts with none of the core loop objects/interactables visible and then reveals them one by one alongside explanatory text that teaches the player how to use them.  
* Tutorial Pop-ups (requires Tutorial Progression) (1 unlock pop-up \= 1 pts, 2 unlock pop-ups \= 2pts, 4 unlock pop-ups \= 3pts, 8 unlock pop-ups \= 4pts): when a player first encounters a new mechanic a new explanatory text display pop-ups spatially located next to the new mechanic's object or display that explains how the new mechanic works.  
* Secrets (2pts): through a hidden interaction, a script triggers to remove an obstruction or otherwise cause new content to appear  
* Resource Simulation (1 resource \= 1 pts, 2 resources \= 2pts, 3 resources \= 3pts, 5 resources \= 5pts, 8 resources \= 6pts, 13 resources \= 7pts): there are resource counters that are Euler integrated over timesteps (for example: health, experience points, stamina, gold, food, seeds, etc) or increase with discrete events. A visible element in the scene indicates how much of the resource there is  
  * Leveling-up Stats (requires the Resource Simulation story) (3pts): there is an interaction to exchange some resource for an increase in a "stat" number. This stat enters the calculations for gaining more resource (e.g. damage per second, passive generator output)  
  * Leveling to New Interactions (requires the Resource Simulation story) (2pts): the player can exchange some resource to unlock a new interactable/interactor to play with  
  * Ramping Difficulty (requires Leveling-up Stats) (2pts): after a level-up, the cost for the next level-up increases. There is a visible element that marks the increased distance to the next level-up  
  * Branching Level Tree (requires "Leveling to New Interactions") (2pts): leveling up to unlock one new interaction opens the possibility to unlock two other new interactions previously unavailable  
* Points Scoring (2 pts): certain discrete interactions award the player with points. A visible element in the scene indicates how many points you have  
* Combo Streak (requires either Resource Simulation or Points Scoring) (2pts): performing an interaction at the precise time, angle, or location increases a combo counter that multiplies the points scored each time or improves the calculations for gaining more resource  
* Multiple Levels (2 levels \= 3pts, 3 levels \= 4pts, 4 levels \= 5pts, 6 levels \= 6pts, 9 levels \= 7pts, 14 levels \= 8pts): the player can move to new scenes with distinct content and interaction placement after "clearing" a level. "Clearing" may be either meeting the Win Condition for that level, elapsing a certain amount of time (see "Timer" story), or reaching an endpoint (see "Goal Zones")  
* Level Clear with Ranking (requires Level Transition and Points Scoring) (2pts): before moving to the next level, the amount of points is ranked as D-Rank, C-Rank, B-Rank, or A-Rank (even up to S-Rank, SS-Rank, and SSS-Rank)  
* Level Transition (requires Multiple Levels) (2pts): upon "clearing" a level, a transition is played announcing the level completion. This may also be an event that triggers juice for the "Juicy Feedback" stories above (for example, a burst of sparks to mark the level completion, a gold clinking sound effect playing as it tallies your score, animated eases transitioning in the new level's objects)  
* Branching Routes (requires Level Transition) (2pts): on the level-up screen there is an interaction available to choose between two different levels to play next  
* Vection-free Motion Schemes  
  * Field of View Restriction on Motion (1pts): the field of view narrows when moving  
  * Constant Velocity Motion Scheme (1pts): a transition to a new location is performed as a constantly velocity motion in a straight line  
  * Rest frame vehicle (2pts): a rest frame is added that transports the vehicle through the environment with a fixed element around them at all times  
  * Teleportation (1pts): the player can move through the scene by pointing to where they want to teleport. There is a visualization previewing where the player would land  
  * Portals (1pts): (requires the Teleportation feature) Instead of pointing and clicking to teleport, the player may teleport by interacting with a portal object whose texture shows the view from a distant point. [Hint](https://medium.com/@tmaurodot/creating-a-portal-system-in-unity-f25954537c00)  
  * [Links to an external site.](https://medium.com/@tmaurodot/creating-a-portal-system-in-unity-f25954537c00)  
  * : add a Camera to the scene and use the "Target Texture" property to send its view to a texture you put on an object to be your "portal"  
* Procedural Generation (requires Multiple Levels) (7pts): new levels are procedurally generated to create an endless supply of new content for your player to enjoy

### Pausing:

* Restart Option (1pts): a UI button is available for restarting the challenge from the beginning  
* Quit Option (1pts): a UI button is available for quitting the game  
* Inter-session Saves (3pts): the state of the game (for example anything under the "Progression" header) are saved between sessions  
* Idle Progress (requires the Inter-session Saves story) (3pts): upon reloading the game, any time-based mechanics (such as the Resource Simulation story) are updated as if the game had been progressing the whole time the player was away

### Non-Player Behaviors:

* Path Following (2pts): a non-player character or other object follows a path pre-defined by the designer  
* Path Planning (requires the Path Following feature) (4pts): the path the character/object follows is dynamically planned in response to the player's effects using a planning algorithm  
* NPC Spawner (2pts): new dynamic objects are spawned for the player to contend with  
* Conditional Despawning (2pts): through (possibly multiple) player interactions a non-player object can be removed from the scene  
  * Loot Drop (requires Conditional Despawning and Resource Simulation) (2pts): upon removing the non-player object from the scene, the player acquires more resource  
* Enemies (requires Resource Simulation) (2pts): through a collider, the non-player object can reduce the player's Resource  
* Attacking Health (requires Enemies and Conditional Despawning) (2pts): the enemies have their own resource they need to stay in the game that is tracked and visibly displayed somewhere in the proximity of the enemy in the scene. Through an interaction the player can "attack" the enemy and decrease its resource.  
* State Machines:  
  * Visible Flag (2pts): the game reacts to a condition on what your player has done to switch a memory state of a Boolean variable. This could be as short-term as enemies going aggro when the player with a certain distance of them or as long-term as a door elsewhere in the dungeon being unlocked. Either way, when this "flag" is switched some element in the scene (either environment or on a character) that is visible to the player at that location must change its appearance to track the switched Boolean variables. This flag being thrown also counts as a "Simulated Event" for the purposes of juicy feedback but the permanent visible change does not count as a feedback.  
  * Locking in Sockets (requires Visible Flag) (1pts): the flag being thrown moves a socket interactor into a new location for access  
  * Dropping Toys (requires Visible Flag) (1pts): the flag being thrown spawns a new interactable object

### Other:

* Multiplayer (10pts): two players can interact with the same interactables in the same simulated space at the same time  
* Mirrors (2pts): there is an object that lets the player see themselves in reflection (  
* [hint](https://youtu.be/4v3eAhuleI0?si=UngzwI-TUj6yxjVO)  
* [Links to an external site.](https://youtu.be/4v3eAhuleI0?si=UngzwI-TUj6yxjVO)  
* [![][image2]](https://youtu.be/4v3eAhuleI0?si=UngzwI-TUj6yxjVO)  
* : use a camera to a RenderTexture)  
* Character Cosmetics & Customization (requires Resource Simulation) (2pts): through an interaction, the player can exchange resource to change the model of their hands or head or how it looks

### Collaboration:

On Github:

* Code Review (2 reviews \= 1pts, 4 reviews \= 2pts, 8 reviews \= 3pts): a teammate had code in a different branch and submits a pull request to add their code to the main branch. Before approving the merge you leave comments on things you'd like to see changed. One pull request counts as one "review" for this rubric  
* Revising Code from Feedback (2 revisions \= 1pts, 4 revisions \= 2pts, 8 reviews \= 3pts): Before merging your side branch into the main branch, a teammate reviewed your code and left comments. You then incorporated at least one of those requested changes before merging into the main branch. One merge counts as one "revision" for this rubric  
* Merge Conflict Management (1 deconflicts \= 1pts, 2 deconflicts \= 2pts, 3 deconflicts \= 3pts): when merging two branches you manually corrected which lines from each branch should make it to the final branch. Merging two branches together as a whole counts as one "deconflict" for this rubric

On Docs:

* Design Rescoping (3pts): after some time working on this project you and your team needed to strategically refocus the scope of your project onto different features. You left at least three comments or revisions to the design documents. To demonstrate this in your video, open the doc's edit version history (clock icon) and show three different timestamps with your name on them, or open the doc's Comment history (text bubble icon) and show three comments with your name on them. These icons are in the upper left of the Google Doc editor as shown in the below figure:

[image1]: <data:image/png;base64,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>

[image2]: <data:image/png;base64,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>