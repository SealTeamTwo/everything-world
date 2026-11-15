# Blocked seats (land audit, 2026-11-15)

The land audit now checks every seat for something where a sitter's body would be (the tree through MONO's bench was the first case found). This is what the full photo tour found on 2026-11-15, sorted by reading the code.

**Update, 2026-11-15: every REAL seat below is fixed**, and a fresh full tour confirms it. What the tour still lists (19 seats) is the BY DESIGN rides and cars, the Mulatu hat and the Punk water-tower edge. The fixes, in short:
- **Moved:** the Soul benches away from the lamps; Grunge stumps kept clear of the log, the axe target, the bunkhouse, the fire and each other; MONO's ash tree kept 4.5 m from the deck; Boom Bap brownstones never sit below the sidewalk; Far Shores loungers on top of the sand; Afrobeat canoes afloat where the sand runs out; the Fusion ride cymbal moved behind the kit.
- **Rebuilt:** Scores chair backrests on the right side; Undercity consoles facing the arc's middle, and Portishead chair backs turned; booths spaced or narrowed (Oddball diner, Sal's Pizza, Night Café); Arena hardtops with a higher roof and seats down inside; an open tractor cab with posts and panes; a shorter giant-shoe upper; the kawaii hearts thin along the chair back.
- **Seats removed** where someone or something already sits: the Bluegrass porch pickers, the Flatt & Scruggs banjo, the Taylor Swift banjo.
- **Small:** the Driftwood hot tub has no lid, and its water sits at the sitter's waist.
- **The check** now also skips leaves made of the Foliage surface (two street trees brushing Boom Bap stoops).

Totals: **REAL 69 seats (21 groups), BY DESIGN 32 seats (11 groups), UNSURE 11 seats (4 groups).**
Paths are relative to `Game/EverythingWorld/Assets/_Project/Scripts/World/Kits/`. Verdicts come from reading the code against the hit positions. Unity was not run.

## Table

| land | count | verdict | what | where |
|---|---|---|---|---|
| Afrobeat Coast | 1 | REAL | The middle beach canoe sits on the sea floor, past the island's levelled edge, so the glowing "sun road" strip on the water crosses the sitter's chest. The other two canoes are probably sunk too, but they are outside the strip. | canoe + seat AfroTown.cs:229; sun road AfroKit.cs:411 |
| Arena Rock Ridge | 10 | REAL | The 5 hardtop drive-in cars. Seats are at g+1.0, on top of the 0.9 m body, and the roof is at g+1.46..1.54, so heads go through it. Convertibles are fine. | roof ArenaPlaces.cs:106; seats :110 |
| Bedroom Pop Meadows | 2 | UNSURE | The bubble bath "rim" is a zero-depth box (size z = 0) floating over the middle of the tub. It grazes only the top 1 cm of the head box. Probably a typo for a real rim. | rim MeadowsPlaces.cs:164; seats :174 |
| Bluegrass Hollows (porch) | 4 | REAL | The four porch pickers are modelled seated musicians in Rocker chairs, but Rocker always adds a seat, so the player would sit inside them. | pickers BluegrassTown.cs:212-215 (body/instrument :229-247); seat from Rocker BluegrassKit.cs:446 (needs a no-seat option) |
| Bluegrass Hollows (Flatt & Scruggs) | 1 | REAL | A banjo is propped in the empty rocker, across the sitter's chest. If it's meant as a still life, drop the seat. | BluegrassArtists.cs:157 |
| Boom Bap Harbour | 7 | REAL (probable) | Brownstone stoop seats. On downhill houses the terrain-following sidewalk (Stone) rises 1.2 m or more above the house floor and under the stoop. The one Foliage hit is probably a street tree on the same slope. Check in play. | seat BoomTown.cs:130; sidewalk :95; trees :99, BoomKit.cs:265-268 |
| Country Plains (Jason Aldean) | 1 | REAL | The tractor seat (g+2.3) is inside the solid cab box (g+1.5..3.1). | cab CountryArtists.cs:193; seat :195 |
| Discotopia | 2 | BY DESIGN | Outward-facing limo seats. The back of the check box (z -0.1) reaches 7.5 cm into the cabin wall, which is the backrest. | cabin DiscoTown.cs:102; seats :105 |
| Driftwood Shore | 2 | REAL | The barrel hot tub's Prism is capped (capTop defaults to true), so a wooden lid at tc+1.1 hides the water and goes through the sitters' chests. Fix: `capTop: false`. | tub DriftwoodPlaces.cs:219; seats :222 |
| Electric Fields | 8 | BY DESIGN | Carousel creatures: the brass pole runs through the rider (seat-local x 0, z +0.05). | pole ElectricPlaces.cs:137; seat :142 |
| Far Shores (beach) | 2 | REAL | The two westmost loungers and their umbrellas are buried under the flat Stone sand disc, where the ground drops toward the sea. You can see it in Logs/Shots/Far_Shores/30_the_beach.png. | loungers FarTown.cs:128 (FarKit.cs:248-254); disc FarTown.cs:126 |
| Far Shores (Mulatu Astatke) | 1 | UNSURE | A hat sits on the chair back, its brim 0.98 m above the seat, just behind the head. Cosmetic at most. | seat FarArtists.cs:188; hat :189 |
| Far Shores (Tinariwen) | 5 | UNSURE | GableRoof's flat underside makes a tent ceiling 1.18 m above each cushion. A seated head probably clears it, but a standing player's head goes through that ceiling anywhere in the tent. | seats FarArtists.cs:201; roof :198; underside MeshKit.cs:180 |
| Funk Tropics (car) | 1 | BY DESIGN | Seat inside the long low car's solid cabin block. | cabin FunkPlaces.cs:156; seat :160 |
| Funk Tropics (giant shoes) | 10 | REAL | The seat is at face*1.3 on the toe, but the shoe's tilted upper reaches over it to about face*1.39, through the sitter's chest. Fix: seat at about face*1.55, or shorten the upper. | upper FunkKit.cs:259; seat :260; placed :367 |
| Fusion Isthmus (Yussef Dayes) | 1 | REAL | The ride cymbal (0.85 m above the stool) and its stand are 0.3 m in front of the drummer's face. | cymbal + stand FusionArtists.cs:185; stool seat :187 |
| Grunge Rainwood | 1 | REAL (likely) | A randomly placed "Sit on the stump" stump landed against the pale end of the big log on blocks. Stumps only avoid the logging road. | stump GrungePlaces.cs:136; log :144 |
| Hair Metal Strip | 3 | BY DESIGN | Salon dryer hoods come 5-7 cm into the top of the box. | hood HairPlaces.cs:203-204; seat :205 |
| Heartbreak Boardwalk | 4 | BY DESIGN | Still carousel: each horse's brass pole runs through the seat. | pole + seat HeartTown.cs:110 |
| Isle of Scores | 9 | REAL | The orchestra chairs are built backwards. The backrest is on the music-stand side, 0.2 m in front of the sitter's chest. Fix: `- face*.2f` to `+ face*.2f`. | backrest ScoresTown.cs:79; seat :81 |
| Jam Band Commonwealth | 8 | BY DESIGN (but odd) | Ferris-wheel gondola: its own hanger rod runs through the sitter. The roof is only 0.3 m above the seat, so the eye ends up above the roof. | rod JamPlaces.cs:232, roof :234; seat :238 |
| Latin Coast (Camila Cabello) | 1 | BY DESIGN | The Havana car's cabin block around the seat. | LatinArtists.cs:173 |
| Liquid Rapids | 1 | BY DESIGN | Rope swing: the rope hangs through the seat. | RapidsTown.cs:63-64 |
| Lo-Fi Hills (digitalluc) | 1 | BY DESIGN | The car's open door grazes the box (the seat is in the doorway). | LofiArtists.cs:166, 169 |
| Nu Metal Quarry | 2 | BY DESIGN | Driver's seats in the two Lot cars with open doors are half inside the cabin block. | cabin QuarryPlaces.cs:147; seat :151 |
| Oddball Backroads (diner) | 4 | REAL | The booths overlap. Centres are 2.4 m apart but each booth is 2.7 m wide, so each inner seat has the neighbouring booth's backrest in it. Fix: `b = 3.6f, 6.6f`. | OddballPlaces.cs:230 (spacing), :237-238 (boxes), :239 (seats) |
| Pop Republic (Taylor Swift) | 1 | REAL | The "Mean" banjo stands on the chair seat; its neck runs 0.12-0.98 m above it. | banjo PopArtists.cs:189; Chair() :205-210 |
| Power Metal Peaks (kawaii café) | 4 | REAL (small; a backrest) | The "heart-backed chairs": the neon-pink heart is a Blob whose radii (.04, .2, .2) are in world axes, not the chair's. In this café's orientation it is 0.4 m deep instead of 4 cm, spanning seat-local z -0.4..0 and y 0.18..0.58, so it sits in the sitter's lower back. Fix: pick the radii from the chair's facing, e.g. thin along `f.Right`. | PowerPlaces.cs:214 (`mk.Blob(neonPink, cp + up*.9f + f.Right*e*.2f, new Vector3(.04f,.2f,.2f), ...)`); seat same line |
| Punk Badlands (Sal's Pizza booths) | 2 | REAL | The door is 3.3 m wide (DoorWidth 2.8 + .5), so the window wall starts at wl = -0.5. Booth 1 (bx 4.3) gets clamped to 3.4, only 2.3 m from booth 0 (bx 1.1). Their inner seats are back to back, 0.3 m apart, and each red backrest (at ±1.3) sits inside the other booth's seat. This is the same bug as the Oddball diner. Fix: one booth, or narrower booths. | bx and clamp PunkPlaces.cs:177; booth boxes + seats :179; Door :150 |
| Punk Badlands (Water Tower edge) | 3 | UNSURE | "Sit with your legs over the edge": the deck rail is 0.19 m in front of each seat. The posts (r .03) and the top rail (1.03 m above the seat) cross the front of the box. The comment says you sit under the rail, so it's intended, but the rail is at face height and its solid collider blocks the legs. If changed: seat at `-PW/2f + .32f`. | seats PunkPlaces.cs:272; rail :264-269 |
| Small Hours (Night Café) | 1 | REAL | The right booth's outer seat (k=1, e=+1, x 5.35) is inside the café's east wall (x 5.2..5.5, Stone). The door is 3.0 m (DoorWidth 2.8 + .2), so wx0 = -1.3 and the booths start too far right. Fix: `bc = fo.P(wx0 + 1.7f + k*3.2f, ...)`, putting the outer seat at 4.75. | booths SmallHoursPlaces.cs:105-106; side wall :93; Door :88 |
| Soul Lowlands (main street) | 3 | REAL | Each of the 3 street benches has a globe lamp post through it. Benches are at d = StreetFrom+23+k*44 (23, 67, 111) and StreetW/2+.5 out; lamps are at d = StreetFrom+1+k*22 (also 23, 67, 111) and StreetW/2+.6 out. So the post (r .09, iron) stands 0.1 m behind the bench's single seat, through the backrest and the sitter's back. The PostCollider is also inside the bench collider. Fix: benches at `StreetFrom + 12f + k*44f`. | benches SoulTown.cs:209 (TownBench SoulKit.cs:281-285); lamps SoulTown.cs:195 (GlobeLamp SoulKit.cs:239-242) |
| Trip-Hop Undercity (control room) | 2 | REAL (likely) | The 7 consoles sit on an arc around f.P(0,0,3), radius 4, but are turned by +a instead of -a, so they fan outward instead of facing the arc's centre. The seats on k=1 and k=5 (1 m in front of their console) end up 0.46 m from the centre of the neighbouring console (k=0, k=6), inside its desk; that console's cold key lights (0.92 m) cross the sitter. Fix: `dr = f.r * Quaternion.Euler(0, -a, 0)`. | UnderTown.cs:115 (console, keys and seat all on this line) |
| Trip-Hop Undercity (Portishead "Sour Times") | 2 | REAL (a backrest) | The bar-table chairs' backrests are built sideways: `new Vector3(.06f, .7f, .5f)` in the chair's LookRotation(facing) frame makes a 6 cm wide, 0.5 m deep fin. Centred 0.22 m behind the seat, it spans z -0.47..+0.03 and up to 0.65 m above the seat, straight through the sitter's middle. Fix: size `(.5f, .7f, .06f)`. The Rob Dougan chair at :171 is built correctly. | UnderArtists.cs:136 |
| Velvet Rooftops (Ginuwine) | 1 | BY DESIGN | Carousel pony: the gold pole (Paint(Gold), r .05) runs through the rider. | pole VelvetArtists.cs:229; seat :230 |
| Math Rock Escarpment (MONO) | 1 | REAL (already known) | The pale ash tree's branches cross the viewing-deck bench. | deck MathArtists.cs:142-145; bench :146 (Bench MathKit.cs:560); tree at :148, trunk :149, branches :150 |

## Suggestions for the check

1. **Opt-in "ride / enclosed" flag on Seat** (e.g. `Interact.Seat(..., ride: true)` setting `Seat.enclosed`). The check skips flagged seats. This is the only rule that catches every BY DESIGN case: the cars (Funk, Latin, Lo-Fi, Quarry, the Disco limo), the dryer hoods, the carousels, the gondolas and the rope swing. Several of these are merged into the land's chunk mesh, so no parent or name rule reaches them.
   - Don't skip vehicles wholesale, whether by verb ("Sit in the …") or by type. The Arena car roofs, the Country tractor cab and the Driftwood tub lid are REAL hits on the seat's own vehicle or furniture.
2. **Parent rule** (cheap, partial): skip meshes on the seat's own parent when that parent isn't a land's "… life" object. This catches only the Electric "Carousel creature" and the Jam "Gondola". The Rapids rope swing is a separate "Rope swing" object, so it would need a name skip, or a rule that skips anything moved by life code.
3. **Pole rule** (generic for rides): skip triangles whose vertices all lie within about 0.08 m horizontally of the seat's vertical axis (seat-local x≈0, z between -0.05 and +0.08). That is a pole you hold, through the middle of the rider. It catches Electric (z +0.05), Heartbreak, Velvet, the Jam rod and the Rapids rope, and none of the REAL hits.
4. **Box top 1.25 → 1.1 m.** A seated head reaches about 0.9-1.0 m. This drops the Meadows rim graze (1 cm), the Hair dryer hoods (5-7 cm into the top) and the Tinariwen tent ceiling (1.18). Every REAL hit stays: the deepest-up ones are the Fusion cymbal at 0.85 m and the Arena roofs at 0.46-0.54 m.
5. **Box back -0.1 → -0.05 m.** This drops the Disco limo's cabin-wall graze (7.5 cm). REAL hits behind the sitter still register: the Soul lamp post reaches z -0.01, the Power heart z 0, the Portishead fin z +0.03.
6. **Backrests are not the false-positive cause.** Every correctly built backrest checked (rockers, benches, camp chairs, booth backs, Pop/Far/Grunge/dryer chairs, the TownBench at -0.24, the UnderKit bench at -0.26) lies 0.17 m or more behind the seat point, clear of z -0.1. The backrest hits are all real build bugs:
   - the Power heart: world-axis radii, 0.4 m deep, z -0.4..0;
   - the Portishead fin: rotated 90°, z -0.47..+0.03;
   - the Scores chairs: backrest on the front side, z ≈ +0.2;
   - a neighbouring booth's backrest inside the seat (Oddball, Punk pizza).
   So no "thick backrest at -0.1" exemption is needed.
7. **Two bug patterns worth a separate note in the build-land checklist:**
   - Booths laid out from a fixed door width: Punk pizza, Small Hours café and Oddball. HumanScale.DoorWidth is 2.8 m, which leaves less room than the booth loops assume.
   - Seats added under a modelled sitter or prop: the Bluegrass pickers, the Flatt & Scruggs banjo, the Pop banjo.
