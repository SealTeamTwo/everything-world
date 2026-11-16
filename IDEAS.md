# Ideas and open questions

Things to remember that aren't built or decided yet. Newest first.

## The owner's note-taking tour, 2026-11-15 (T notes, sorted)

102 notes and 46 snapshots over about 40 lands (`Logs/Live/notes.txt`, not in the repo). Overall: "a lot more pure time and effort dedicated to each land individually will go a long way."

**Loved:**
- Metalcore Caldera: visually the favourite land; everyone who has seen it is blown away.
- Concert Isles: the architecture.
- Neo Soul Quarter: the giant potted plants.
- Roots Bay: a surprise favourite. Find more music for it, and make it feel like a real Jamaican island.
- Old Jazz Quarter: the black-and-white noir.
- Math Rock Escarpment: the little musical valley between Metalcore and Djent/Power Metal.
- Power Metal Peaks.
- Velvet Rooftops: the neon skyscrapers in the water.
- Boom Bap Harbour: the railroad imagery.
- Afrobeat Coast: the colour theory.

**Fixes (things that look or work wrong):** all done 2026-11-15 (checked by the photo tour, not yet in play), except the Funk-to-Afrobeat bridge, which is a real shortcut and was left. How they were fixed:
- **Bridges:** bridge ends no longer land on cut-off islets (`islandmap.py`). Five moved, including the Hub's bridge, which now reaches the Prog Marches.
- **Hub:** the terminal, riddle and colour station face the middle, clear of the stands. The seat cue is a soft, turning circle.
- **Benches:** every bench seats two (`Interact.Bench`).
- **Flicker:** tiny height offsets wherever two surfaces met.
- **Suns:** the other lands' suns show only inside their own land (`LandOnly`).
- **Trails and climbs:**
  - The Fens trails wind round slopes and stay out of the sea.
  - The Virtuoso climb is one smooth deck.
- **Other:**
  - The Bayou bridges stay low and only cross water.
  - Bluegrass creek bed carved wider.
  - The sunken pool's water is solid.
  - The beach has a collider.
  - The bed has a duvet slope.
  - The Jam banners and buses are tie-dye.
- **Hub:**
  - The bridge towards Prog Marches goes halfway, glitches, then lands in the Djent Spires (snapshots 603, 604). There should be more bridges from the Hub.
  - "Choose your light" runs into the stands and faces an odd way. Turn it, the terminal and the riddle to face the centre.
  - The flashing blue square for sets would look better as a soft, semi-transparent, wispy blue circle.
- **Indie Downs:**
  - Death Cab for Cutie's plaque has a light post right next to it, and it shows a Pavement cover.
  - Sit on the campfire logs.
  - Benches should generally have two seats (606).
- **Shoegaze Fens:**
  - A path with no collision dips into the water.
  - Something blocks a path near Arab Strap (613).
  - A path drops very steeply down a hill (614).
- **Garage Rock Row:** the Kooks' plaque is inside a vehicle (615).
- **Bedroom Pop Meadows:** you can't jump onto the blankets and pillows (everyone tries).
- **Isle of Scores:** the floor glyph flickers against the floor, and two crossing paths flicker.
- **Concert Isles:**
  - Two fountains flicker (618, 619).
  - The artist plaques round one square are hidden (619).
- **Heartbreak Boardwalk:** a gazebo roof is invisible from underneath (621).
- **Isle of Memes:**
  - The green parts of windows flicker (624), and so does the blue of the dolphin piece (626).
  - Crossing paths flicker.
  - The Synthwave sun shows from here (627).
- **Dubstep Shards:** a huge sun in the distance that makes no sense (630).
- **Mount Virtuoso:**
  - The walk up is janky (632 to 636).
  - The grey thing at the top has no collision.
- **Southern Rock Bayou:** a bridge looks wrong (639 to 641).
- **Jam Band Commonwealth:**
  - The farmhouse is inside Widespread Panic's area.
  - The rainbows should look like tie-dye, not rainbow flags.
- **Bluegrass Hollows:** the river is badly broken.
- **Alt Rock Moors:** you can jump down into the aquarium and get stuck.
- **Lo-Fi Hills:** paths overlap.
- **Afrobeat Coast:**
  - A random bridge (647).
  - The grey plaza has no collision.

**Music:**
- "Andrew's Favorite" (Lou Doe) feels like a meme song (Isle of Memes?).
- A couple of Lo-Fi Hills songs may belong in the Game Isles.
- As a joke with friends, "The Night the Lights Went Out in Georgia" should also play on the Phish radio, while staying where it normally is.

**Artist areas that need much more:**
- **Lana Del Rey:** hugely important on the playlist; a Radiohead-scale treatment. *Built 2026-11-15: her own night quarter with five places for the five favourite songs (WORLD_GUIDE.md). The playlist only has some of her bigger songs; the owner and friends like many more, so more of hers may be added later.*
- **Virtual Riot:** Radiohead-scale. *Built 2026-11-15: the whole Dubstep Shards is now a crystal cave system with his Moon Hall in the middle (WORLD_GUIDE.md).*
- **Taylor Swift:** a friend's favourite; better, but not Radiohead-scale.
- **Others:** Phoebe Bridgers, Big Thief, Florence Road (more artistic and unique), Trey Anastasio.
- **Power Metal:** the castle with the dragon should belong to the biggest radio-station artist there.

**Lands to rework or add to (directions from the tour):**
- **Cities, not scattered buildings:**
  - Hip-Hop Boroughs should be laid out as urban blocks, but kept different from Boom Bap.
  - Old Jazz Quarter: far more blocks and streets, plus a little red neon to draw the eye.
  - Funk Tropics: a bigger urban part.
  - Boom Bap Harbour: less rural overall.
  - Discotopia: less rural, more of an interior feel.
  - Several lands have a main street; extend that so some lands are a town or city in every direction.
- **Velvet Rooftops:** bring the neon skyscrapers inland. Walk on rooftops and walkways, like a floating city, maybe the water pouring into a bottomless hole with the towers rising out of it and you crossing building to building.
- **Trip-Hop Undercity:** a friend wants it really under a city, mostly interiors, like a New York subway or metro.
- **Fusion Isthmus:** should really be an isthmus; canals everywhere, like a fusion Venice, and more town.
- **J-Rock Isles:** tight neon Tokyo-style alleyways, maybe Escher-like with different levels (642).
- **Power Metal Peaks:** a bit more Japanese theming (a kawaii dragon?).
- **Kingdom of Classic Rock:** carry on with the war fronts; the owner also likes a Greek Olympian setting.
- **Yacht Rock Riviera:** a friend thinks lands should match their terrain word better; here, a river.
- **Math Rock Escarpment:** more fantastical; the autumn forest fits the music, so mix in a contrasting theme.
- **Ambient Mists and Sample Gardens:**
  - They look far too alike.
  - Ambient Mists could be more imaginative: an older art movement than vaporwave, like a Matrix or 90s-techno look.
  - Sample Gardens: more fictional or imaginative.
- **Mount Virtuoso:** more atmospheric and epic, with something special at the summit.
- **Isle of Memes:** a friend couldn't tell it was vaporwave/Y2K; push the theme further.
- **Thin and need much more:** Studio Lot Island (sparse), Latin Coast (more alive), Soul Lowlands (basic), Far Shores (very basic, a big rework), Liquid Rapids, Trance Steppe (a special theme for both), Grunge Rainwood (more energising), Nu Metal Quarry (bland, which fits the music, but more), Afrobeat Coast (more detail).
- **Electro Foundry:** the owner loves electro; add a ton. (2026-11-16: he now thinks it may not be missing anything.)
- **Artistic spins:** give some lands an unexpected art theme fused with their own, built as art pieces that belong to that land. First idea: the Electro Foundry as tech crossed with ancient Egypt.
- **Too much empty land (2026-11-16):** most lands are 15-40% designed; the rest is wide ground with thin random scatter. Plan: give each land a walkable heart sized to its content, with edges that suit it (tree walls, cliffs, water, fences), props grouped with a reason, open space on purpose. Ranked list: Logs/LandAudit/land_space.md.
- **Dubstep Shards:** more about energy (crystal, electric, plasma).
- **Lo-Fi Hills:** super vibey and cozy.
- **Djent Spires:** a crystal mountain with an observation point at the top, reached by running through ice and crystal caves.
- **Discotopia:** the "Camel by Camel" area (Sandy Marton) could reference the viral Animal Crossing meme set to that song.
- **Punk Badlands:**
  - The theme is liked, but rough in low poly.
  - No trees and same-colour dunes make the bright paper boards stand out, which shows how many duplicates there are.
- **The crossings between lands:** each should look like the land you're about to enter, more specifically and artistically.
- **Travel:**
  - A three-way bridge joining IDM, Trance and Trip-Hop, meeting in a big circle over the sea.
  - More bridges from the Hub.

**Activities (one main game per land; see the lobby vision below):**
- Garage Rock Row: bowling, and a casino.
- Synthwave Coast: light bikes.
- Game Isles: arcade games, and coins to collect with a sound (they come back).
- Hair Metal Strip: motorcycle racing.
- Punk Badlands: skating.
- Post-Punk Docklands: a falling-blocks game with the containers.
- Iron Range: a zombie-survival round, maybe in the mines (ambitious).
- Lanternwood: ice fishing, ice skating on the frozen lake, and a telescope that really works.
- Indie Downs: kites you can fly.
- Southern Rock Bayou: the riverboat sails about, carrying the CCR radio station with it.

**Across the world:**
- Small path fixes, left for later: side paths overlap the path they join with a V-shaped notch (seen in the Prog Marches); a Hub path on the coast slope sinks under the ground in places.
- Humanoid avatars instead of orbs. The map is held in human hands, which doesn't fit an orb.
- Everything you pick up does its own thing when you use it. A toaster shoots up toast; you sip drinks (the Small Hours tea mugs) and eat food.
- Playable instruments: the piano keys and small instruments round the world, played with the keyboard, real notes.
- One vehicle for getting about faster, which changes look in each land.
- The Hub riddle unlocks a double jump instead of the floaty effect.
- The Hub's "launch a rocket" fires a real firework one day.

## The lobby game: the vision (2026-11-15)

The world is a lobby: a place to exist in, enjoy and lose yourself in with friends, like a hangout spot. Games live inside the lobby; finishing or leaving one leaves you standing in the world, never at a menu. "Finished" is when the small moments (a drink on a bench with friends) feel fluid and immersive. Places stay put so they can be loved; what happens in them keeps changing (chance, rare events, surprise), because staleness is the main worry.

- **A main activity per land:** a game inside the game, tied to the land's theme, distinct in every land. It starts and ends in the world (friends can wander up, join, watch or ignore it). Lots of work; worth it. Possible Hub link: a land's songs unlock in the Hub by completing its activity.
- **Small activities everywhere:** getting a coffee or a drink, sitting, things to fiddle with. Flesh these out so they flow.
- **Lands of other forms** (a friend's idea, case by case): underground or a metro station; a huge treetop forest city; one vast city; a land that is all interiors.
- **Something extremely unique about each land.** Example, Classic Rock Kingdoms: each main artist holds a territory; where the borders meet, soldiers fight; the borders are the radio stations' edges and move in real time (on the shared clock, so friends see the same front line).
- **Musical bypasses:** ways to travel between lands without passing through ones you don't want to hear (for example Bluegrass to Math Rock without djent or power metal). Could be a metro (ties in with the underground idea): lines and tunnels with their own music.
- **Local MP3s** (the owner's, about a week out; never in the repo): lets the world follow the music itself (builds, drops, beats). Hold detailed song-moment work until then.
- **The music hears the space** (once the MP3s play through Unity): step inside a building and the music sounds like it (muffled through walls, a room's echo, a hall's reverb, tinny from a radio in a shed; open air sounds open). The indoor spaces are already listed per land (IndoorSpaces).
- **Lands to rework, one by one** (2026-11-15): the owner will list lands that could use a change. First: the Post-Punk Docklands (loved), but much of its music doesn't fit the night-time scenery: it is grim post-punk (Joy Division, The Cure, The Smiths) next to bright new wave, pop and ska (The Go-Go's "Vacation", Nena, Madness, a-ha, Tears For Fears, Dexys, The Police) and noisy art rock (Maruja, Black Country, New Road, Melt-Banana, Sonic Youth).
  - **Agreed direction, not started (waiting until the owner says go):** the time of day follows the song. One docks; each song turns the whole land to its hour over a few seconds: post-punk deep night (as now), new wave/pop/ska a bright summer afternoon (sun on the water, gulls, washing, a busy quay), noise/art rock a stormy dusk (bruised orange sky, wind, far lightning). Artists' corners stay put; each artist "owns" an hour. Some things show only at their hour (neon and lit windows at night, beach chairs and people in the sun).
- **Song moods for every land** (2026-11-15, thinking it through; **on hold until the owner's note-taking tour**, T in game). Many lands have songs that don't fit their scenery, like the Docklands (Classic Rock's dark songs in sunshine, doom in the Psychedelic Wilds' daytime, night songs at the Folk Woods' sunrise, cosmic songs under the Iron Range's storm). The idea:
  - Each song gets a mood (storm, night, bright, golden...), first guessed from the Spotify valence and energy every track already has, plus title words, plus a hand-corrected list (fed by the owner's notes).
  - Each land decides what each mood looks like there, from its own look (shared sky system, never a shared look). Some lands never change (the Small Hours stays night); artist areas can keep their own hour.
  - Eased over about half a minute; it can start turning just before the next song (the schedule is known); all on the shared radio clock, so friends see the same sky.
  - Risk: whiplash between songs. Either change only for songs that clearly don't fit, or drift with the run of songs.
  - **After Lana Del Rey's night quarter (2026-11-15):** the owner is sold on changing the lighting, sky and ambience of an area for specific songs. Not the majority of songs, but for the right ones it was a powerful effect. So the "change only for songs that clearly don't fit, or that deserve it" version is the one to build. Still waiting for him to say go.
  - **The owner's point:** a blue-sky, sunny, green-grass, green-tree scene is probably the most boring and least expressive look. So a "bright" mood should never fall back to plain sunny green: each land's bright version needs its own character.
- **Generated music** only between the real songs (the wilds, crossings, the sea, a fallback), never replacing the playlist.

## Owner's notes after a long two-player playtest, 2026-11-14

- **The biggest issue: placement that doesn't make sense to a person walking around.** Examples: a large obstacle in the middle of a path, a house on a hillside half floating in the air, paths that go nowhere sensible. The photo tour shoots from flattering spots (arrival, artist fronts, high view) and the audit has no path or grounding checks, so these slip through. Some colliders are still missing too.
- **Local audio instead of (or alongside) Spotify:** the owner is collecting local copies of the songs, buying some, so the game can play the audio itself. This would reverse the "sound only from Spotify" decision, so plan it with the owner first. Audio files must never go in the public repo. The song display pieces wait until this is done.
- **A richer low-poly look, in the spirit of Valheim** (first version 2026-11-14: low mist (HeightFog shader, `heightFog...` in land_moods.json), colour grading (`grade`) and cloud shadows on the Folk Woods only; surface grain and ground colours blended at land borders everywhere; next: roll out to other lands once the owner has played it): simple shapes with rich light. That means fog and atmosphere, soft long shadows, light shafts, dense swaying grass and strong colour grading. Use the techniques only, not its look or assets.
- **What worked best:** visual interpretations of songs (the Zero Eden visualizer, Metalcore Caldera, the electro area, the arena, the Phish and Radiohead displays, the Velvet city). Sometimes the reference wasn't findable, so give each one a small clue such as the song title somewhere nearby.

## Designed lands, residents and things to do (2026-11-12, first version on the Oddball Backroads)

- **Every land laid out on purpose**, not scattered: a road network over the whole land, a string of places made to stop at (each with somewhere to sit, something to play with, a view, its people), and the countryside between them designed too (fields, hedgerows, lone trees on hilltops, dark hollows). Strong contrast by brightness and blue/yellow. The Oddball Backroads is the first; it becomes the standard for new lands, then a pass over the built ones.
- **Shared systems built:** press E to sit (benches, booths, car seats, a giant chair), to play with toys (doors that open, a jukebox, a fan), to pick things up (put down with E, throw with a click), and to say hi to residents. Walking into loose things (a ball of twine) pushes them.
- **Residents in every land:** small people who live and work there, placed with purpose (the cook, a farmer, a motel clerk), who look at you, wave, and say a line when you say hi. Their look and lines are each land's own.
- **Activities (to discuss):** mini golf is planned, probably in the Yacht Rock Riviera. Talk through an activity system (scores, turns, playing with friends) with the owner before building one.

## Surprise and spontaneity (2026-11-09, core to the theme, first version built)

Built (SurpriseDirector, SharedChance, all on the radio's shared clock, so friends on the same station share them): lights and fires follow the song's energy and pulse when a new song starts; on starry lands a comet in every song, a meteor shower in about one song in three, and the odd shooting star; the aurora with about one song in four (Lanternwood); five wandering lanterns, a new set with each song; snowmen that move each day (days start 5 am US Eastern). Test keys in play: F7 shooting star, F8 aurora, F9 a one-minute meteor shower. More ideas below.

The playlist is meant to be surprising and spontaneous, so the world shouldn't be the same every visit. Idea: drive surprises from the same shared clock as the radio, so friends see them together. Rare moments (a shooting star, the aurora on some nights, lanterns pulsing when a certain song starts), small things that shift by day or week (where snowmen stand, which fire is lit, heavier snow some days), and lights or weather reacting to the song playing (energy, mood).

## Claude sees the world itself (2026-11-09, next up)

- Run Unity from the command line (batch mode) to build the world, run the land audit and save screenshots from where you arrive, each artist area and above, so Claude can check a land without the owner testing every round. Unity has to be closed while it runs. The land audit and the `build-land` skill are already done.

## Saved for other lands (2026-11-10)

- **A crumbling stadium standing like a castle** (battlement stands, floodlight towers, a stage on the pitch): first built for the Kingdom of Classic Rock, taken out at the owner's request to save it for the stadium/arena rock land (Arena Rock Ridge).
- The Kingdom took a gentle "heavenly" touch (golden light through the Hall's dome, gold dust). If another land needs heaven as its main idea, keep the Kingdom's version understated.

## Owner's notes from play, 2026-11-11 (to do)

- **Garage Rock Row:** keep the main strip with the artist spots on it, but surround it with suburbia: lines of streets with small nondescript houses. Song artwork and song pieces can live out in the suburban streets.
- **The Synchronous World (Small Hours exhibit):** handrails or something similar along the trail, so there's a clearer path to follow the progression.
- **Akashic Sanctuary (Small Hours exhibit):** a sign with the song's name at each piece.
- **Light points of interest:** Metalcore Caldera, Post-Punk Docklands and the Iron Range keep their darkness, but use light more strategically and more liberally on every point of interest (and the Caldera's smaller artist spots need to read better).
- **Post-Punk Docklands:** a flat centre where the tall buildings stand, on something closer to a street grid, with slopes round it down to the docks.
- **Grunge Rainwood:** more colour and more lighting (it can be higher energy, not only gloomy), without overdoing it; make Alice In Chains (the owner's favourite of the scene) easy to find.

## Activities: the owner's thinking (2026-11-16, not decided, not built)

- **Walk up and play together:** activities are things in the world you walk up to and play with friends.
- **Shrinking:** starting an activity shrinks you down onto a tiny version (a tiny golf course on a table, say), so no activity is limited by the room its land has.
- **More activities wanted:** basketball and pool, besides mini golf (Riviera), skating, ice fishing and the rest above.
- **The reward is music:** doing well (going under par for the golf course, say) unlocks the full Everything Playlist while you play that activity. Before that, the activity only plays its land's songs. You can switch it back to just the land.
- **Completionist piece in the Hub:** each activity finished adds to something in the Hub (see the pedestals idea below).
- **One fixed bar per activity** (under par, say) for the unlock. High scores or records could be shown at the Hub.
- **Mini golf is the big one:** expected to be one of the main things friends do. 18 holes on the Riviera, built so another course could be added one day.
  - The first course is the Riviera's own: yacht-rock life in miniature (a yacht's deck, a swimming pool, a marina, a cocktail bar, a sunset pier). A world-tour course (each hole a tiny land) could be a second course later.
  - The more moving parts and tricks the better (moving obstacles, ramps, water that resets the ball). Everyone putts at once, in the style of party mini golf games.
  - A full round takes 20 to 30 minutes.
  - Build order: the game itself on 3 holes first (shrink, putt, scores, friends, the unlock), then the other 15.
- **Air hockey** is another candidate.
- **The feel to aim for** comes from party games the owner plays with friends: mini golf with everyone on the course at once, a nine-ball pool game with a twist of chance, a quick basketball shooting game. Take the feel and the kind of game only; every course, table, rule set and look is our own.
- **Collectibles** to find around the world (not a priority yet).

### An activity for every land (the owner wants one each; this list is a first draft to react to, 2026-11-16)

Rules every activity shares: a start object you press E on; you shrink onto a tiny version themed as its land (the owner leans toward shrinking for every activity: it suits the theme and leaves each one room to grow); friends join at the same object; it plays the land's songs until you beat its one bar; beating the bar unlocks the Everything Playlist switch for that activity and adds a trophy and your record at the Hub; leaving puts you back beside the object.

Activities come in families that share their workings, so each new one is mostly its own course and look. **Decided by the owner:** golf (Riviera), basketball, pool, air hockey, light bikes (Synthwave Coast), skating (Punk Badlands), ice fishing and skating (Lanternwood), falling blocks (Docklands, on hold), survive the waves (Iron Range), arcade games (Game Isles), giant puzzles. Everything else is a suggestion.

- **Ball games:** Yacht Rock Riviera: mini golf, 18 holes. Old Jazz Quarter: pool (plain pool, no twist). Hip-Hop Boroughs: basketball (casual freestyle: grab a ball, dribble, charge up a shot; every basket keeps its own count). Game Isles: arcade cabinets and air hockey. Glitterlands: bowling under the glitter balls. Oddball Backroads: giant pinball where you are the ball (the owner loves this one). Indie Downs: croquet. Small Hours: table shuffleboard in the diner. Latin Coast: beach football. Arena Rock Ridge: stadium penalty shoot-out. Jam Band Commonwealth: disc golf across the commons. Bluegrass Hollows: bag toss. Country Plains: horseshoes. Driftwood Shore: stone skipping.
- **Riding and racing:** Synthwave Coast: light bikes that leave walls of light. Punk Badlands: skating. Soul Lowlands: a roller rink. Hair Metal Strip: drag racing down the Strip. Garage Rock Row: go-karts. Nu Metal Quarry: mine carts. Southern Rock Bayou: airboat racing. Liquid Rapids: white-water rafting. Fusion Isthmus: boat racing round the record's grooves. Far Shores: paper boat racing down the glass stream. Funk Tropics: bumper boats. Mount Virtuoso: sled slalom. Melodic Bass Highlands: gliding through rings. Power Metal Peaks: dragon flight. Trance Steppe: an endless run down a tunnel of light. Lo-Fi Hills: paper planes.
- **Aim and survive:** Iron Range: survive the waves with friends. Folk Woods: archery. Kingdom of Classic Rock: jousting. Grunge Rainwood: axe throwing. Dubstep Shards: a bass cannon that knocks towers down. Electric Fields: laser tag. Heartbreak Boardwalk: carnival stalls. J-Rock Isles: festival stalls (goldfish scooping). Concert Isles: stage diving for distance.
- **Rhythm and music:** Afrobeat Coast: a drum circle. Discotopia: claim the light-up dance floor. Four-on-the-Floor Steppe: a sprint track with pads to hit on the beat. Isle of Scores: conduct the orchestra. Pop Republic: a music quiz show about the playlist. Sample Gardens: a sound-matching memory game.
- **Puzzles and courses:** Math Rock Escarpment: the giant puzzles. Post-Punk Docklands: falling blocks. Prog Marches: a giant board game with moving squares. IDM Glitchlands: circuit puzzles. Electro Foundry: a robot assembly line. Trip-Hop Undercity: a maze in the dark. Psychedelic Wilds: a mushroom bounce course. Metalcore Caldera: crossing the lava on sinking stones. Djent Spires: a climbing race. Isle of Memes: an absurd obstacle course. Studio Lot Island: a stunt course. Velvet Rooftops: a rooftop run. Delta Crossroads: a dice game at the crossroads (no stakes). Roots Bay: dominoes.
- **Calm and together:** Lanternwood: ice fishing and skating. Shoegaze Fens: catching fireflies. Ambient Mists: cloud sailing. Bedroom Pop Meadows: kites. Alt Rock Moors: hide and seek in the fog. Painting a mural together (the owner loves the activity, but it does not fit Boom Bap Harbour: it needs another land; the Psychedelic Wilds is one thought). Boom Bap Harbour: undecided (one thought: building a beat together on giant drum pads). Neo Soul Quarter: running the coffee bar together.
- **Later, bigger:** giant puzzles; a survive-the-waves game with friends in the Iron Range.

## Social spaces and minigames (2026-11-09, later)

- A big Christmas lodge (probably in or near Lanternwood) you can go inside, sit down in and talk with friends.
- Pub songs and Christmas songs might get an area of their own one day. For now the sea shanties and pub songs (Drunken Sailor, Wellerman, Ratlin' Bog) and A Holly Jolly Christmas live on the Isle of Memes.
- Minigames somewhere in the world.

## Performance (2026-11-10, first pass done)

- **Measured** with `python build/take_shots.py perf` (Unity closed): it times rendering from the hub and two busy lands, eight ways each, and tries switching suspects off, writing `Game/EverythingWorld/Logs/perf.txt`.
- **The cause was the terrain**, not the scenery: every distinct ground colour was its own material, and the built lands blend colours smoothly, so a terrain chunk had thousands of materials. Ground colours are now rounded to 24 shades per channel (`IslandWorldBuilder.Quantise`). The hub's view toward the Indie Isles went from 140 ms to 4 ms a frame; the busiest lands from about 58 ms to 3 ms.
- Also done: land-wide scenery and glow meshes are split into 80 m chunks (`MeshKit.Build`; use `BuildSingle` for a mesh that's moved as a whole), and `LandCuller` hides grass beyond 220 m, small props 400 m, glows 650 m, mid-sized scenery 900 m, big pieces 1.6 km, and lights out of reach. Triangles drawn from the hub fell from about a million to 50 thousand.
- **Memory (2026-11-10):** with eleven lands built, the meshes took 4.1 GB on the 8 GB graphics card, and frame times swung between 3 and 20 ms depending on what else was using it. Unity's static batching was keeping a second copy of all the scenery; it is now off (Player Settings, Standalone), which halved that to 2.1 GB at the same speed. `take_shots.py perf` now prints the memory and the biggest meshes.
- The grass is now most of what's left (about 1.5 GB: each land's swaying ground cover is 1-2.6 million vertices). With ~45 lands to go, that needs a fix before long: build grass only near the player, or thinner grass far from paths.
- Next, if it's ever needed: clouds (about 100 separate meshes per built land), simpler far versions of lands.

## A quest across the lands (2026-11-09, wanted, not designed yet)

- **One activity or minigame per land**, made for that land like everything else (its own idea, fitting its music), with something you have to do or finish in it.
- **Pedestals in the hub:** an empty pedestal for every land. Finishing a land's activity fills its pedestal (with something from that land). Filling every pedestal does something big (to be decided).
- **A passport (Sean's idea):** it gets stamped for each land you visit, each stamp designed for its land.
- **Collectibles or a scavenger hunt** hidden across the lands.
- Open questions: is progress per player or shared by friends playing together? Saved between sessions? What happens when every pedestal is full? Where does the passport show (a book you open, a page on the hub map)?
- Fits with what's built: the daily "something moves" happenings (the Driftwood bottle, the Fens heron) could become hunt items; the build-land skill should get a step for each land's activity once the system exists.
- Generally: buildings sized for groups (doorways 2.5 m+, rooms for 4-6 people), and places to sit and reflect in every land.

## Artist descriptions (2026-11-09, wanted, later)

Each artist area should have something to read about the artist, not just the plaque facts. Likely source: Last.fm's artist bio (`artist.getInfo`, already our tag source), trimmed to a few sentences. Last.fm bios are user-written wiki text under CC BY-SA, so show a small "from Last.fm" credit. Bigger artists (halls, temples) could get a longer text inside; shrines a line or two. Would need a fetch step in `build/` and a text field in the Unity export.
- **Story plaques in Gamehendge (2026-11-10):** once artist descriptions exist, put a short readable story text in front of each Gamehendge place (the Rhombus, Icculus's mountain, the Helping Friendly Book, Wilson's castle, the Lizards' village and the Lizards, the Famous Mockingbird). Each place already has a little name sign to grow into one.

## Sky per land (2026-11-08, decided)

Each land keeps its own sky, light, fog and weather (its look brief's "mood"), always at its best time of day. There is no shared day and night. Crossing a border blends to the next land's sky over a few seconds, like the music switching. Descriptions of every land are in `WORLD_GUIDE.md`.

## World shape and scale (2026-11-08, decided, next to build)

After walking the first Unity blockout:
- **Scale 2.5×:** the game world becomes about 12 × 8 km (2.5 m per map pixel).
- **Every artist gets an area**, sized by song count: shrine (1 song), monument (2–5), hall (6–15), grand temple (16+). Plots are saved in `build/artist_plots.json` so they stay put when the playlist grows; islands keep spare land for new artists and a plaza for the genre landmark.
- **Artist radio** for artists with 3+ songs while you stand in their area; smaller shrines keep the island's genre radio.
- **Continents in the middle, islands at the edges** (liked, details to confirm): the big central realms become shared landmasses where scenes border each other like countries (borders marked by rivers, ridges or roads), small realms stay island chains at the edge. Proposed: The Old Kingdoms + Riffland as one central continent; The Indie Isles, The Chart Isles and The Groovelands as medium landmasses; The Bass Reaches, The Boroughs and The Isles of Story as island chains. Music similarity still decides who borders whom.
- **Land sized by need, not one fixed scale** (decided after measuring the 2.5× build, where small islands were packed full): each scene gets its artist plots, about 2× that again kept free for future artists, its landmark plaza, and at least ~0.4 km² of open land to wander. About 25–35 km² of land in total; the world's outer size follows from that.
- Built so far (uncommitted, will be redone after the playlist update): the 2.5× export with artist plots in `build/islandmap.py`, and the Unity island world (`Scripts/World/`, menu Everything World > Build Island World Scene).

## Sound beyond Spotify (2026-11-08, low priority)

Not seen as necessary, just possible:
- **Game ambience on top of Spotify:** Unity can play its own fully 3D sounds (waves at the Driftwood Shore, lava at the Metalcore Caldera, crowds near stages, footsteps) alongside the Spotify music.
- **Volume dips at zone edges:** soften the cut between stations with a quick volume dip. Only volume can be changed through the API. It's steppy, it changes the player's real Spotify volume, and some devices (iPhones) refuse it.

Not possible: EQ, effects or spatial/3D placement of the Spotify music itself, since the audio never passes through the game (and Spotify's terms forbid capturing it).

## Artist areas (2026-11-08)

Every artist gets their own area inside their scene, showing:
- some of their cover art and song names,
- a plaque with info about the artist.

This replaces the older idea of forming "kingdoms" from how many songs an artist has, which no longer feels right.

Facts that shape this (from `build/scenes_layout.json` and the playlist export):
- **Each artist lives in exactly one scene.** All their songs are in that scene, so an artist area never has to be split.
- **Artists already have a position** (x, y) inside their scene from `layout_scenes.py`, with similar artists placed near each other. Artist areas can sit at those spots.
- **Most artists are small:** 871 of 1,154 artists have 1 song, 154 have 2, and only 30 have 6 or more. Phish alone has 66 songs, then The Beatles (20), Grateful Dead (17) and Radiohead (15). Areas need to scale, from a single display for a one-song artist up to a large area for Phish.
- **29 songs cross scenes through a featured artist,** for example Skrillex feat. Ellie Goulding, or Rihanna feat. JAY-Z. 257 artists only appear as features and have no area of their own.

Open questions:
- What does an area look like at each size? For example a single display or stall for 1 song, a small plot for 2–5, a building or landmark for the biggest.
- What goes on the plaque: genre tags, years, the friend who added the most songs, a short bio from Last.fm?
- Where do cover images come from? `build/fetch_covers.py` and `build/cover_manifest.json` already exist from the 2D map.
- Should cross-scene features become visible links, for example a signpost or a path between the two artists' areas?
- Does an artist area have its own music, for example the radio playing only that artist's songs while you stand there, or does it stay part of the scene's station?

- **Blue fire in the Metalcore Caldera** (a friend's idea in play, 2026-11-15): burning sulphur gives a blue flame, so the Caldera could have blue lava or blue flames somewhere, maybe a sulphur vent field or its hottest heart. That would also add strong blue/yellow contrast against the orange.

## Done

- **Zone radio** (2026-11-08): every scene is an always-on station on a shared clock, so friends hear the same part of the same song.
- Prog Marches: the owner plans to add much more prog to the playlist; leave the open marsh for new artists and give each new big band its own song places like the Floyd world.
