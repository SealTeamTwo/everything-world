using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.Activities.GolfSurface;

namespace EverythingWorld.Activities
{
    /// A whole course: its holes and where each sits on the water.
    public class GolfCourse
    {
        public string id, name; public GolfHole[] holes; public Vector3[] origins; public float[] yaws;   // each hole's own metres placed on the course
        public float width, depth;
        public int Par { get { var p = 0; foreach (var h in holes) p += h.par; return p; } }

        /// A hole's point, on the course.
        public Vector3 At(int hole, Vector3 local) => origins[hole] + Quaternion.Euler(0, yaws[hole], 0) * local;

        /// Lays the holes out in rows on the water, a gap between each.
        public void Arrange(int perRow, float gap)
        {
            origins = new Vector3[holes.Length]; yaws = new float[holes.Length];
            float cellW = 0f, cellD = 0f; foreach (var h in holes) { cellW = Mathf.Max(cellW, h.maxX - h.minX); cellD = Mathf.Max(cellD, h.maxZ - h.minZ); }
            cellW += gap; cellD += gap; var rows = Mathf.CeilToInt(holes.Length / (float)perRow); width = cellW * perRow; depth = cellD * rows;
            for (var i = 0; i < holes.Length; i++)
            {
                var h = holes[i]; var row = i / perRow; var col = i % perRow; if (row % 2 == 1) col = perRow - 1 - col;   // there and back, so the next hole is always next door
                var centre = new Vector3((col + .5f) * cellW - width / 2f, 0, (row + .5f) * cellD - depth / 2f);
                origins[i] = centre - new Vector3((h.minX + h.maxX) / 2f, 0, (h.minZ + h.maxZ) / 2f);
            }
        }
    }

    /// The Riviera Links: eighteen holes of yacht-rock life in miniature, standing in a sunset sea. Our own holes and
    /// tricks, in the manner of party mini golf (everyone putts at once, the more moving parts the better).
    public static class GolfCourses
    {
        static GolfCourse riviera;
        public static GolfCourse Riviera => riviera ??= MakeRiviera();
        public static GolfCourse ById(string id) => id == "riviera" ? Riviera : id == "pinball" ? Pinball : null;

        static GolfCourse pinball;
        /// The Oddball Backroads' pinball table, described the way a golf hole is (the same ball, the same pieces):
        /// one long sloping floor of ice with the drain at the bottom, two flippers, kick bumpers, a booster lane up
        /// the left, a spring, a pipe, and the launch lane up the right side.
        public static GolfCourse Pinball => pinball ??= MakePinball();
        public const float PinSlope = .2f; public static readonly Vector3 PinStart = new Vector3(10.2f, 0, 4.2f);

        static GolfCourse MakePinball()
        {
            const float top = 1f + 44f * PinSlope; float Y(float z) => 1f + z * PinSlope;
            var h = H("The Oddball", 3, "You are the ball.").On(Ice).Path(11f, "LRSE", 0, 0, 1, 0, 44, top);
            h.Wall(9.4f, 3.2f, 9.4f, 37f, .6f).Wall(9.4f, 3.2f, 11f, 3.2f, .6f)                 // the launch lane
             .Wall(11f, 39.5f, 6.5f, 44f, .6f).Wall(-11f, 39.5f, -6.5f, 44f, .6f)               // the top corners
             .Wall(-11f, 11f, -4.6f, 4.2f, .6f).Wall(9.4f, 9.4f, 4.6f, 4.2f, .6f)                // the guides down to the flippers
             .Flipper(-4.6f, 4.2f, 3.9f, -25f).Flipper(4.6f, 4.2f, 3.9f, 205f)
             .PondStrip(-11f, 1.3f, 9.4f, 1.3f, 1.3f)                                              // the drain
             .Bumper(-3f, 30f, .9f, 9f).Bumper(3f, 30f, .9f, 9f).Bumper(0, 34.5f, .9f, 9f).Bumper(-6.4f, 22f, .7f, 8f).Bumper(5.4f, 23f, .7f, 8f).Bumper(-7.2f, 12.5f, .6f, 8f).Bumper(6.3f, 11.5f, .6f, 8f)
             .Post(0, 12.5f, .35f, .7f).Post(-2.2f, 39f, .3f, .7f).Post(2.2f, 39f, .3f, .7f)
             .Boost(-9.8f, 14f, -9.8f, 31f, 1f, 15f).Spring(0, 19f, .8f, 6.5f).Pipe(-8.6f, 36.5f, 5.8f, 40f, 200, 5f, .6f)
             .Tee(PinStart.x, PinStart.z).Cup(0, -30f)
             .Prop("flamingo", -9.6f, 41.5f, 140, 1.6f).Prop("ring", 8f, 41.8f, 0, 1.5f).Prop("lamp", -11.5f, 22).Prop("lamp", 11.5f, 22).Prop("lamp", -11.5f, 40).Prop("lamp", 11.5f, 40);
            h.floorLook = "night"; h.Done(); h.cup = new Vector3(0, -50f, -200f);   // (no cup: nothing to putt into)
            var c = new GolfCourse { id = "pinball", name = "The Oddball", holes = new[] { h } }; c.Arrange(1, 0f); return c;
        }

        static GolfHole H(string name, int par, string about) => new GolfHole().Named(name, par, about);

        static GolfCourse MakeRiviera()
        {
            var holes = new List<GolfHole>();

            // 1. a straight putt down the gangplank
            holes.Add(H("Welcome Aboard", 2, "Straight down the gangplank.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 24.2f, 1)
                .On(Green).Disc(0, 29, 5.5f, 1).Cut(0, 24.15f, 2.58f)
                .Tee(0, 0).Cup(0, 30)
                .Prop("palm", -6, 6).Prop("palm", 6.5f, 17).Prop("buoy", -7, 27).Prop("sailboat", 11, 8, 20).Prop("lamp", -2.9f, 12).Prop("lamp", 2.9f, 12).Done());

            // 2. up the gangway to the upper deck: too soft and it rolls back down
            holes.Add(H("The Gangway", 3, "Up to the top deck. Too soft and it comes back.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 12, 1, 0, 20, 2.6f, 0, 27.2f, 2.6f)
                .On(Green).Disc(0, 32, 5.5f, 2.6f).Cut(0, 27.15f, 2.58f)
                .Tee(0, 0).Cup(0, 33)
                .Prop("yacht", -11, 16, 0, 1.4f).Prop("palm", 7, 4).Prop("lamp", -2.9f, 22).Prop("lamp", 2.9f, 22).Prop("buoy", 8, 30).Done());

            // 3. a dogleg round the marina: play it off the angled board
            holes.Add(H("Marina Dogleg", 3, "Round the corner. The angled board is your friend.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 22, 1, 21.2f, 22, 1)
                .Wall(-2.6f, 19.4f, 2.6f, 24.6f)
                .On(Green).Disc(26, 22, 5.5f, 1).Cut(21.15f, 22, 2.58f)
                .Tee(0, 0).Cup(27, 22).Route(0, 0, 0, 20, 27, 22)
                .Prop("sailboat", 9, 10, 90).Prop("sailboat", 15, 10, 90).Prop("yacht", 12, 31, 90, 1.2f).Prop("palm", -6, 12).Prop("buoy", 20, 14).Prop("lamp", 10, 24.9f).Done());

            // 4. past the propellers
            holes.Add(H("Propeller", 4, "Time it past the propellers.")
                .On(Deck).Path(3f, "LRS", 0, -3, 1, 0, 36.85f, 1)
                .Spinner(0, 15, 2.75f, 4, 70f).Spinner(0, 27, 2.75f, 2, -85f, .2f)
                .On(Green).Disc(0, 42, 6, 1).Cut(0, 36.8f, 2.98f)
                .Tee(0, 0).Cup(0, 43.5f)
                .Prop("yacht", 11, 20, 180, 1.5f).Prop("buoy", -7, 10).Prop("buoy", -8, 30).Prop("palm", 8, 40).Done());

            // 5. the pool: round it down either side, or off the diving board and over
            holes.Add(H("Pool Party", 4, "Round the pool, or off the diving board and over it.")
                .On(Stone).Path(8f, "LRSE", 0, -3, 1, 0, 40, 1)
                .PondStrip(0, 12.5f, 0, 24, 5f)
                .On(Deck).Path(1.3f, "LR", 0, 4.5f, 1, 0, 11.5f, 3.6f)
                .On(Green).Disc(0, 33.5f, 5, 1, 0, false)
                .Post(6.5f, 15, .28f, 1.6f, "parasol").Post(-6.5f, 21, .28f, 1.6f, "parasol")
                .Tee(0, 0).Cup(0, 35).Route(0, 0, 6.5f, 10, 6.5f, 27, 0, 35)
                .Prop("lounger", -10.2f, 14, 90).Prop("lounger", -10.2f, 19, 90).Prop("lounger", 10.2f, 17, -90).Prop("lounger", 10.2f, 22, -90).Prop("flamingo", 2.2f, 17, 30, 1, 1.02f).Prop("ring", -2.5f, 21, 0, 1, 1.02f)
                .Prop("palm", -10.5f, 5).Prop("palm", 10.5f, 30).Prop("cabana", 0, 43.2f, 180).Done());

            // 6. the keyboard: weave between the black keys
            {
                var h = H("Piano Keys", 3, "Weave between the black keys.").On(Stone).Path(3f, "LRS", 0, -3, 1, 0, 34.85f, 1);
                for (var k = 0; k < 5; k++) { var z = 8f + k * 5.2f; if (k % 2 == 0) h.Wall(-3f, z, .7f, z, .55f, "key"); else h.Wall(3f, z, -.7f, z, .55f, "key"); }
                holes.Add(h.On(Green).Disc(0, 40, 6, 1).Cut(0, 34.8f, 2.98f).Tee(0, 0).Cup(0, 41.5f)
                    .Route(0, 0, 1.8f, 8, -1.8f, 13.2f, 1.8f, 18.4f, -1.8f, 23.6f, 1.8f, 28.8f, 0, 41.5f)
                    .Prop("piano", -8, 12, 60).Prop("piano", 8.5f, 26, -120).Prop("palm", 7, 6).Prop("lamp", -3.3f, 20).Done());
            }

            // 7. the sandbar: bunkers and a hump on a bending fairway
            holes.Add(H("Sandbar", 3, "Mind the bunkers and the hump.")
                .On(Green).Path(3.2f, "LRS", 0, -3, 1, 0, 15, 1, 8, 30, 1, 8, 39.6f, 1)
                .On(Sand).Disc(1.4f, 10.5f, 1.8f, 1, 0, false).Disc(6.6f, 31.5f, 1.7f, 1, 0, false).Disc(1.2f, 20.5f, 1.5f, 1, 0, false)
                .On(Green).Disc(4.6f, 22.5f, 2.3f, 1.6f, -.6f, false)
                .Disc(8, 44.6f, 6, 1).Cut(8, 39.55f, 3.18f)
                .Tee(0, 0).Cup(8, 46).Route(0, 0, 0, 15, 8, 30, 8, 46)
                .Prop("palm", -6, 14).Prop("palm", 14, 24).Prop("parasol", 13, 36).Prop("lounger", 14.5f, 37.5f, 30).Prop("buoy", -5, 30).Done());

            // 8. the rowing club: boats crossing the lane
            holes.Add(H("Rowing Club", 4, "The rowers have right of way.")
                .On(Deck).Path(3.2f, "LRS", 0, -3, 1, 0, 39.95f, 1)
                .Slider(-2.05f, 10, 4.1f, 0, 2.2f, 1f, 4.2f, 0f).Slider(2.05f, 19, -4.1f, 0, 2.2f, 1f, 3.6f, .35f).Slider(-2.05f, 28, 4.1f, 0, 2.2f, 1f, 3f, .6f)
                .On(Green).Disc(0, 45, 6, 1).Cut(0, 39.95f, 3.18f)
                .Tee(0, 0).Cup(0, 46.5f)
                .Prop("buoy", -6, 10).Prop("buoy", 6, 19).Prop("buoy", -6, 28).Prop("sailboat", 12, 30, -20).Prop("palm", -8, 42).Done());

            // 9. the lighthouse: a ramp winding once round it to the green on top; the angled board at the end turns the ball in
            {
                var h = H("The Lighthouse", 5, "Round and up. The board at the top turns you in.");
                const float cx = 0f, cz = 22f, rad = 7f, top = 3.4f; var pts = new List<float> { -15, 15, 1 }; var route = new List<float> { -12, 15 };
                for (var k = 0; k <= 24; k++)
                {
                    var a = (-90f + k * 15f) * Mathf.Deg2Rad; var y = Mathf.Min(top, 1f + (top - 1f) * k / 22f);   // level for the last stretch, beside the green
                    pts.Add(cx + Mathf.Cos(a) * rad); pts.Add(cz + Mathf.Sin(a) * rad); pts.Add(y);
                    if (k % 3 == 0) { route.Add(cx + Mathf.Cos(a) * rad); route.Add(cz + Mathf.Sin(a) * rad); }
                }
                h.On(Deck).Path(1.9f, "LRS", pts.ToArray());
                h.Wall(-1.85f, 13.29f, 0, 16.9f, .6f, "kerb", top);                              // the angled board
                h.On(Green).Disc(cx, cz, 5.1f, top).Cut(-1.32f, 17.07f, 1.4f, top - .7f);        // the green, open to the top of the ramp
                route.Add(-1.3f); route.Add(17.2f); route.Add(cx - 1.6f); route.Add(cz + .2f);
                holes.Add(h.Post(2.2f, 23.5f, .3f, 1.8f, "lantern").Tee(-12, 15).Cup(cx - 1.6f, cz + .2f).Route(route.ToArray())
                    .Prop("tower", cx, cz, 0, 1, 0).Prop("buoy", -12, 28).Prop("buoy", 11, 12).Prop("sailboat", 14, 30, 200).Prop("palm", -17, 10).Done());
            }

            // 10. cocktail hour: glasses that kick the ball away
            holes.Add(H("Cocktail Hour", 3, "The glasses kick. Play the angles.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 13.4f, 1)
                .On(Stone).Disc(0, 22, 9, 1).Cut(0, 13.38f, 2.58f)
                .On(Green).Disc(0, 25.5f, 3.2f, 1, 0, false)
                .Bumper(-3.2f, 18f, .6f, 7f, "glass").Bumper(3.2f, 18f, .6f, 7f, "glass").Bumper(0, 21.2f, .6f, 7f, "glass").Bumper(-5.4f, 24.5f, .6f, 7f, "glass").Bumper(5.4f, 24.5f, .6f, 7f, "glass").Bumper(0, 29.6f, .6f, 7f, "glass")
                .Tee(0, 0).Cup(0, 25.8f).Route(0, 0, 0, 14, 2.4f, 21, 0, 25.8f)
                .Prop("cabana", 0, 33.5f, 180).Prop("palm", -10, 16).Prop("palm", 10, 16).Prop("lamp", -8, 28).Prop("lamp", 8, 28).Done());

            // 11. the drawbridge: jump the channel, then time the gate
            holes.Add(H("Drawbridge", 4, "Jump the channel, then wait for the gate.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 9, 1, 0, 15, 2.4f)
                .Path(3f, "LR", 0, 20, 1, 0, 36.85f, 1)
                .Gate(-3f, 29, 3f, 29, 4.4f, .5f)
                .On(Green).Disc(0, 42, 6, 1).Cut(0, 36.8f, 2.98f)
                .Tee(0, 0).Cup(0, 43.5f)
                .Prop("sailboat", -9, 17.5f, 90, .8f).Prop("buoy", 6, 17.5f).Prop("buoy", -5, 17.5f).Prop("palm", 8, 34).Prop("lamp", -3.3f, 24).Prop("lamp", 3.3f, 24).Done());

            // 12. sea breeze: gusts across a pier with no rail on the far side
            holes.Add(H("Sea Breeze", 3, "No rail on the left. Go when the gusts drop.")
                .On(Deck).Path(2.4f, "RS", 0, -3, 1, 0, 34.05f, 1)
                .Fan(2.4f, 11.5f, -2.4f, 11.5f, 2.5f, 12f, 4.4f, .5f, 0f).Fan(2.4f, 24f, -2.4f, 24f, 2.5f, 12f, 4.4f, .5f, .77f)   // (the far fan drops a second after the near one, so one calm spell carries a ball past both)
                .On(Green).Disc(0, 39.5f, 6, 1).Cut(0, 34f, 2.38f)
                .Tee(0, 0).Cup(0, 41)
                .Prop("fan", 3.6f, 11.5f, 180).Prop("fan", 3.6f, 24f, 180).Prop("sailboat", -10, 14, 0).Prop("sailboat", -12, 27, 10, .8f).Prop("buoy", -5, 6).Prop("palm", 8, 38).Done());

            // 13. the lazy river: let the current carry it round
            holes.Add(H("Lazy River", 3, "Let the river carry it. Mind the rocks.")
                .On(Deck).Path(2.2f, "LRS", 0, -3, 1, 0, 8, 1)
                .On(Stone).Path(2.2f, "LR", 0, 8, 1, 0, 19, 1, 6, 26, 1, 14, 26, 1, 20, 19, 1, 20, 8.5f, 1)
                .Current(0, 8, 0, 19, 2.3f).Current(0, 19, 6, 26, 2.6f).Current(6, 26, 14, 26, 2.3f).Current(14, 26, 20, 19, 2.6f).Current(20, 19, 20, 9.5f, 2.3f)
                .Post(-.7f, 14, .4f, .5f, "rock").Post(10, 26.8f, .45f, .5f, "rock").Post(20.8f, 15, .4f, .5f, "rock")
                .On(Green).Disc(20, 3.5f, 5.5f, 1).Cut(20, 8.54f, 2.18f)
                .Tee(0, 0).Cup(20, 2.6f).Route(0, 0, 0, 19, 6, 26, 14, 26, 20, 19, 20, 2.6f)
                .Prop("palm", 10, 14).Prop("lounger", 8.5f, 12, 20, 1, 0).Prop("flamingo", 10, 19, 0, 1, 0).Prop("palm", -6, 22).Prop("buoy", 27, 22).Done());

            // 14. on the rocks: ice, and ice cubes to bounce off
            holes.Add(H("On the Rocks", 4, "Ice. It doesn't stop when you'd like it to.")
                .On(Deck).Path(4f, "LRS", 0, -3, 1, 0, 6, 1)
                .On(Ice).Path(6f, "LRSE", 0, 6, 1, 0, 30, 1).Cut(0, 6, 3.98f).Cut(0, 30, 2.58f)
                .On(Deck).Path(2.6f, "LR", 0, 30, 1, 0, 33.7f, 1)
                .Post(-2.6f, 12, .8f, .9f, "icecube").Post(2.2f, 16.5f, .8f, .9f, "icecube").Post(-1f, 21.5f, .8f, .9f, "icecube").Post(3.6f, 25f, .8f, .9f, "icecube").Post(-4.2f, 26.5f, .8f, .9f, "icecube")
                .On(Green).Disc(0, 38.5f, 5.5f, 1).Cut(0, 33.65f, 2.58f)
                .Tee(0, 0).Cup(0, 39.5f)
                .Prop("glassbig", 10, 18, 0, 1, 0).Prop("palm", -9, 8).Prop("buoy", 8, 32).Done());

            // 15. smooth sax: three horns, and only one comes out by the cup
            holes.Add(H("Smooth Sax", 3, "Three horns play onto a turning stage. Go when a bell faces the flag.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1, 0, 10.05f, 1)
                .Disc(0, 16, 6.5f, 1).Cut(0, 10.04f, 2.58f)
                .On(Green).Disc(0, 40, 6.5f, 1)
                .On(Sand).Disc(-3.4f, 37.6f, 2f, 1, 0, false)
                // the three bells go slowly round the green on a turning stage, each playing straight across its middle: the
                // cup is off to one side, so a bell only finds it as it comes into line (the left horn plays softest, the right hardest)
                .Spinning(0, 40, 20f, 0f, "stage")
                .Pipe(-4f, 18.5f, 4.7f, 0, 180, 3.6f).Prop("bell", 5.3f, 0, 180)
                .Pipe(0, 20.6f, -2.35f, 4.07f, 300, 4.7f).Prop("bell", -2.65f, 4.59f, 300)
                .Pipe(4f, 18.5f, -2.35f, -4.07f, 60, 5.6f).Prop("bell", -2.65f, -4.59f, 60)
                .Still()
                .Tee(0, 0).Cup(1.7f, 41.5f).Route(0, 0, 0, 20.6f, 1.7f, 41.5f)
                .Prop("sax", -4f, 18.5f, 0).Prop("sax", 0, 20.6f, 0).Prop("sax", 4f, 18.5f, 0)
                .Prop("palm", -9, 30).Prop("palm", 9, 30).Prop("buoy", 0, 28).Done());

            // 16. yacht hop: three decks, two jumps
            holes.Add(H("Yacht Hop", 4, "Three yachts. Two jumps. Hit them hard.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 1.2f, 0, 8, 1.2f, 0, 13, 2.4f)
                .Path(3.2f, "LR", 0, 18, 1.2f, 0, 27, 1.2f, 6.5f, 27, 1.2f, 11.5f, 27, 2.4f)
                .Path(3.2f, "LR", 16.5f, 27, 1.2f, 25f, 27, 1.2f)
                .On(Green).Disc(30, 27, 6, 1.2f).Cut(24.9f, 27, 3.18f)
                .Tee(0, 0).Cup(31.5f, 27).Route(0, 0, 0, 27, 31.5f, 27)
                .Prop("hull", 0, 4, 0, 1, 0).Prop("hull", 2, 24, 0, 1.2f, 0).Prop("hull", 25, 27, 90, 1.3f, 0).Prop("buoy", 7, 15).Prop("buoy", 14, 20).Prop("palm", 36, 34).Done());

            // 17. the hotel lobby: revolving doors, and the bellhop's boom
            holes.Add(H("Hotel Lobby", 6, "Two revolving doors and a porter's barrier.")
                .On(Stone).Path(3f, "LRS", 0, -3, 1, 0, 34.85f, 1)
                .Spinner(0, 10, 2.8f, 4, 80f, 0f, "door").Spinner(0, 19, 2.8f, 4, -80f, .1f, "door")
                .Boom(-3f, 27.5f, 4.6f, 0, 50, 3.4f)
                .On(Green).Disc(0, 40, 6, 1).Cut(0, 34.8f, 2.98f)
                .Tee(0, 0).Cup(0, 41.5f)
                .Prop("palm", -7, 6).Prop("palm", 7, 6).Prop("lamp", -3.3f, 15).Prop("lamp", 3.3f, 15).Prop("cabana", 0, 48.5f, 180).Done());

            // 18. into the sunset: down the slipway, off the lip, through the sun and into the bowl
            holes.Add(H("Into the Sunset", 3, "A firm putt: the slipway and the sun do the rest. Smash it and it is gone.")
                .On(Deck).Path(2.6f, "LRS", 0, -3, 7, 0, 4, 7, 0, 24, 2f, 0, 28, 3.4f)
                .Boost(0, 8, 0, 20, 2.5f, 10f)
                .On(Green).Path(2.6f, "LRS", 0, 28.3f, 1f, 0, 37.1f, 2f)   // a landing apron under the sun: a ball that falls short can putt on up into the bowl
.On(Green).Disc(0, 46, 9, .6f, 1.4f).Cut(0, 37.05f, 2.58f)
                .Ring(0, 46, 9.05f, 2f, 7f, "glass", -52, 232)
                .Tee(0, 0).Cup(0, 46).Route(0, 0, 0, 28, 0, 46)
                .Prop("sunring", 0, 32.5f, 0, 1, 4.4f).Prop("yacht", -14, 38, 10, 1.6f).Prop("yacht", 15, 42, 170, 1.4f).Prop("palm", -12, 52).Prop("palm", 12, 52).Prop("buoy", 5, 31).Prop("buoy", -5, 34).Done());

            var c = new GolfCourse { id = "riviera", name = "The Riviera Links", holes = holes.ToArray() };
            c.Arrange(6, 16f);
            return c;
        }
    }
}
