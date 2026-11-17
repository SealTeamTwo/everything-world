using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades' artists. Each stands on a giant macaron set into the cake, with their name on a wafer sign,
    /// their covers on a biscuit board, a lollipop that glows for every song without its own piece, and one thing made
    /// from their songs:
    /// - Flay! (the one radio station): a stack of clouds to bounce up ("In The Sky"), a crescent moon to sit in at the
    ///   top ("dreamseeker"), and one grey teddy among the bright ones ("Forgotten").
    /// - DraGonis: a candy rocket on a geyser that fires you higher than anything but the cherry ("Escape Velocity"), a
    ///   bench under a clock ("Waiting"). kamome sano: a stack of pancakes under pouring syrup ("sweet syrup"), a turning
    ///   cube of light ("infomorph"). Mameyudoufu: a block of tofu with a face, a row of sweets under a stamp ("Quality
    ///   Control"), a heart-shaped moon ("Moonheart"). Nokae: a ring of candy-cane swords that dance ("Swords Dance"), a
    ///   mirror ("Verity").
    /// - The rest, one each: a giant smile, a glass flower, a door standing open on the sky, a tree of gold leaves, a
    ///   black-and-white cube, a party popper, a ribboned stage screen, an easel, jellyfish round a signpost, an umbrella
    ///   under its own rain cloud, a rice-ball car on a strip of road, a cup of green tea, a figure of eight, a misty
    ///   pool, a snail with a house, a question-mark box, a gate with purple hills, a tower of blocks, a battery.
    public partial class KawaiiKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public ArtistArea area; public HashSet<string> lit = new HashSet<string>();
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        internal static Dictionary<string, (string title, string id)[]> SongTable => SongIds;
        static string Find(string artist, string title) { if (SongIds.TryGetValue(artist, out var l)) foreach (var (t, id) in l) if (t.StartsWith(title)) return id; return null; }

        public override (string song, string label, string artist)[] SpecialSongs
        {
            get
            {
                var list = new List<(string, string, string)>();
                void Add(string artist, string title) { var id = Find(artist, title); if (id != null) list.Add((id, title, artist)); }
                Add("MOTTO MUSIC", "Signs of Rain"); Add("Flay!", "dreamseeker"); Add("tonytonychopper999", "ENERGIZE"); Add("Feryquitous", "Monochrome Anomaly"); Add("Diveo", "Summer Trees"); Add("Amidst", "Glass Petals");
                return list.ToArray();
            }
        }

        /// The song's id (and it is marked as having its own piece). If the title isn't found, the artist's first song.
        string Song(Plot z, string title)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list) || list.Length == 0) return null;
            foreach (var (t, id) in list) if (t.StartsWith(title)) { z.lit.Add(id); return id; }
            z.lit.Add(list[0].id); return list[0].id;
        }

        Material SongGlow(string song, Color c, float s = 1.5f)
        {
            if (song == null) return GlowM(c, s);
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (berry == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var k = Mathf.Abs(a.name.GetHashCode()) % 6;
            Dais(z, k);
            Piece(z, k);
            SongLamps(z);
            CoverBoard(z, z.At(reach * .62f, -reach * .5f - 1.4f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .62f + 1.5f, reach * .5f + 1.4f), front, stick, Paint(Ink), Yellow, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .78f), true, k);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .85f, .9f), HasStation(a) ? 26f : 15f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .95f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .9f, reach * .3f) + up * 2.2f; var look = z.At(-reach * .2f, 0) + up * 1.9f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a biscuit board with an icing edge.
        void CoverBoard(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.75f; var lo = Lowest(ground, w / 2f) - .1f;
            foreach (var e in new[] { -1f, 1f }) { var lp = ground + Vector3.Cross(Vector3.up, dir) * e * (w / 2f - .1f); z.mk.Prism(stick, new Vector3(lp.x, lo, lp.z), Vector3.up, .08f, ground.y + 1f - lo, 6); }
            z.mk.Box(white, c - Vector3.up * .1f - dir * .06f, new Vector3(w + .4f, size + 1.5f, .12f), rot); z.mk.Box(Paint(new Color(.96f, .8f, .55f)), c - Vector3.up * .1f, new Vector3(w + .15f, size + 1.25f, .16f), rot);
            z.solid.Box(white, c - Vector3.up * .6f, new Vector3(w + .4f, size + 2.4f, .3f), rot);
            z.mk.Box(Paint(new Color(1f, .97f, .92f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .03f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .11f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A small lollipop for each song without its own piece: it glows while the song plays.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 3) * 1.2f, Foot(z.a) * .22f + (k / 3) * 1.2f);
                z.mk.Prism(stick, p, up, .05f, 1.2f, 5); z.mk.Prism(SongGlow(id, Sweet[k % 4], 1.6f), p + up * 1.5f - z.front * .1f, z.front, .34f, .2f, 12); ArtistBits.PostCollider(z.root, p, 1.5f, .18f);
                k++;
            }
        }

        /// The base: a giant macaron set into the cake, and the artist's name on a wafer sign at the back.
        void Dais(Plot z, int k)
        {
            var up = Vector3.up; var R = Foot(z.a) * .78f; var lo = Lowest(z.c, R) - .2f;
            z.mk.Prism(SweetM(k), new Vector3(z.c.x, lo, z.c.z), up, R, z.c.y - .12f - lo, 26, 1.04f); z.mk.Prism(cream, z.c - up * .12f, up, R * 1.02f, .12f, 26); z.mk.Prism(SweetM(k), z.c, up, R * 1.04f, .16f, 26, .95f);
            z.solid.Prism(cream, z.c - up * .3f, up, R * 1.02f, .46f, 14);
            var sg = z.At(-Foot(z.a) * .62f, Foot(z.a) * .42f); var w = Mathf.Clamp(z.a.name.Length * .34f + 1f, 2.6f, 7f);
            foreach (var e in new[] { -1f, 1f }) { var lp = sg + z.side * e * (w / 2f - .2f); z.mk.Prism(stick, new Vector3(lp.x, Lowest(lp, .2f) - .1f, lp.z), up, .09f, 3.4f + lp.y - Lowest(lp, .2f), 6); }
            z.mk.Box(Paint(new Color(.96f, .8f, .55f)), sg + up * 2.9f, new Vector3(w, 1.1f, .16f), z.Rot); z.mk.Box(SweetM(k + 1), sg + up * 2.9f - z.front * .03f, new Vector3(w + .3f, 1.4f, .12f), z.Rot);
            BoxCol(z.root, sg + up * 1.8f, new Vector3(w, 3.6f, .25f), z.Rot); ArtistBits.Label(z.labels, z.a.name, sg + up * 2.9f + z.front * .1f, z.front, .34f, Ink, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- one thing from the songs
        GameObject Loose(MeshKit m, string name, Vector3 at, Transform parent = null) { var go = m.BuildSingle(name, parent != null ? parent : life.transform, false); if (go != null) go.transform.position = at; return go; }

        /// A teddy: 'c' is its colour.
        void Teddy(MeshKit mk, Vector3 g, Vector3 f, float sc, Material m, System.Random rng)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, f);
            mk.Blob(m, g + up * sc * .7f, new Vector3(.7f, .75f, .6f) * sc, rng, .06f); mk.Blob(m, g + up * sc * 1.6f, new Vector3(.52f, .5f, .48f) * sc, rng, .06f);
            foreach (var e in new[] { -1f, 1f }) { mk.Blob(m, g + up * sc * 2.05f + side * e * sc * .38f, Vector3.one * sc * .18f, rng, .04f); mk.Blob(m, g + up * sc * .25f + side * e * sc * .45f + f * sc * .55f, new Vector3(.26f, .24f, .4f) * sc, rng, .04f); mk.Box(Paint(Ink), g + up * sc * 1.7f + side * e * sc * .18f + f * sc * .45f, Vector3.one * sc * .08f, Quaternion.identity); }
        }

        void Piece(Plot z, int k)
        {
            var up = Vector3.up; var fr = z.front; var sd = z.side; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 31 + k); var c = z.c + up * .16f; var mk = z.mk;
            switch (z.a.name)
            {
                case "Flay!":
                {
                    var sky = Song(z, "In The Sky"); var dream = Song(z, "dreamseeker"); var forgot = Song(z, "Forgotten"); life.nightSongs.Add(dream);
                    // "In The Sky": clouds to bounce up, one above the other
                    var at = z.At(-3f, -2f); var top = at;
                    for (var i = 0; i < 4; i++)
                    {
                        var p = at + up * (3.2f + i * 6.4f) + Quaternion.Euler(0, i * 80f, 0) * fr * 3f; top = p;
                        var m = new MeshKit(); for (var j = 0; j < 5; j++) m.Blob(j == 0 ? SongGlow(sky, Color.white, .5f) : white, Quaternion.Euler(0, j * 72f, 0) * Vector3.forward * 1.5f, new Vector3(1.7f, .9f, 1.7f), rng, .2f); m.Blob(white, up * .3f, new Vector3(2f, 1f, 2f), rng, .2f);
                        var go = Loose(m, "Cloud", p); course.AddPad(HyperCourse.Kind.Bounce, p + up * .9f, 2.6f, up * 22f, go.transform); halos.Add(p, 3f, Color.white, .12f);
                    }
                    Jelly(at, 1.8f, up * 16f, Soda);
                    // "dreamseeker": a crescent moon to sit in, on the highest cloud's level
                    var moon = top + up * 7.5f - fr * 2f; var mm = SongGlow(dream, Yellow, 1.5f);
                    for (var i = 0; i < 12; i++) { var ang = (-70f + i * 20f) * Mathf.Deg2Rad; var q = moon + sd * Mathf.Cos(ang) * 3f + up * Mathf.Sin(ang) * 3f; mk.Blob(mm, q, Vector3.one * (.5f + .5f * Mathf.Sin(i / 11f * Mathf.PI)), rng, .05f); }
                    z.solid.Box(cream, moon - up * 2.9f, new Vector3(3f, .5f, 1.6f), rot); Interact.Seat(tlife.transform, moon - up * 2.6f, fr); halos.Add(moon, 5f, Yellow, .3f); Star(moon + up * 1f + sd * 1f);
                    // "Forgotten": bright teddies in a row, and one grey one a little apart
                    for (var i = 0; i < 4; i++) Teddy(mk, z.At(6f, -4f + i * 1.8f), fr, .8f, GlowM(Sweet[i], .7f), rng);
                    Teddy(mk, z.At(4f, 6.5f), fr, .8f, SongGlow(forgot, new Color(.55f, .55f, .6f), .5f), rng); BoxCol(z.root, z.At(6f, -1.3f) + up * .9f, new Vector3(7.2f, 1.8f, 1.2f), rot); ArtistBits.PostCollider(z.root, z.At(4f, 6.5f), 1.8f, .6f);
                    Folk(z.At(8f, 3f), -fr, Resident.Pose.Stand, new[] { "Bounce up the clouds. There's a moon at the top you can sit in.", "That grey bear has been here longer than any of us.", "When the dream song plays, it goes dark and the stars come out." }, Local());
                    break;
                }
                case "DraGonis":
                {
                    var esc = Song(z, "Escape Velocity"); var wait = Song(z, "Waiting"); var at = z.At(-2f, -1f);
                    // a candy rocket standing over a geyser: step under it and go
                    for (var i = 0; i < 3; i++) { var leg = at + Quaternion.Euler(0, i * 120f, 0) * fr * 2.6f; mk.Box(stick, leg + up * 2f, new Vector3(.24f, 4.2f, .24f), Quaternion.LookRotation((at - leg).normalized) * Quaternion.Euler(-18f, 0, 0)); ArtistBits.PostCollider(z.root, leg, 2f, .25f); }
                    mk.Prism(SongGlow(esc, Color.white, .6f), at + up * 4f, up, 1.3f, 4.6f, 12); mk.Prism(cherryM, at + up * 8.6f, up, 1.3f, 2.4f, 12, .05f); for (var i = 0; i < 3; i++) mk.Box(cherryM, at + up * 4.8f + Quaternion.Euler(0, i * 120f + 60f, 0) * fr * 1.6f, new Vector3(.14f, 2f, 1.2f), Quaternion.Euler(0, i * 120f + 60f, 0) * rot); mk.Prism(SongGlow(esc, Soda, 1.6f), at + up * 6.2f + fr * 1.32f, fr, .5f, .04f, 12);
                    Jelly(at, 1.7f, up * Mathf.Sqrt(2f * 38f * 60f), Yellow, HyperCourse.Kind.Geyser); halos.Add(at + up * 3f, 3f, Yellow, .3f);
                    // "Waiting": a bench under a clock that never moves
                    var b = z.At(3.5f, 4f); Bench(mk, b, fr, k); mk.Prism(stick, b - fr * 1f, up, .08f, 3.4f, 6); mk.Prism(SongGlow(wait, Cream, 1.2f), b - fr * 1f + up * 3.6f - fr * .1f, fr, .7f, .2f, 16); mk.Box(Paint(Ink), b - fr * .88f + up * 3.85f, new Vector3(.06f, .5f, .02f), rot); mk.Box(Paint(Ink), b - fr * .88f + up * 3.6f + sd * .18f, new Vector3(.36f, .06f, .02f), rot);
                    break;
                }
                case "kamome sano":
                {
                    var syrup = Song(z, "sweet syrup"); var info = Song(z, "infomorph"); var at = z.At(-2f, -2f);
                    for (var i = 0; i < 5; i++) mk.Prism(Paint(new Color(.95f, .76f, .42f)), at + up * (i * .5f), up, 2.4f - i * .08f, .42f, 18, .96f); z.solid.Prism(cream, at, up, 2.4f, 2.5f, 10);
                    mk.Blob(SongGlow(syrup, new Color(.85f, .45f, .1f), 1.2f), at + up * 2.6f, new Vector3(2f, .3f, 2f), rng, .2f); mk.Box(SongGlow(syrup, new Color(.85f, .45f, .1f), 1.2f), at + up * 4.6f + sd * .6f, new Vector3(.16f, 4f, .16f), Quaternion.identity); mk.Box(Paint(Yellow), at + up * 2.75f, new Vector3(.7f, .2f, .7f), Quaternion.Euler(0, 20f, 0));
                    mk.Prism(white, at + up * 6.9f + sd * 1.2f, -sd, .5f, 1.4f, 10); mk.Box(stick, at + up * 6.9f + sd * 2.1f, new Vector3(.9f, .14f, .14f), rot);
                    var cm = new MeshKit(); cm.Box(SongGlow(info, Soda, 1.6f), Vector3.zero, Vector3.one * 1.3f, Quaternion.Euler(35f, 0, 35f)); cm.Box(GlowM(Color.white, 1.2f), Vector3.zero, Vector3.one * .7f, Quaternion.Euler(0, 45f, 0));
                    var cg = Loose(cm, "Infomorph", z.At(3f, 4f) + up * 2.6f, z.root); life.spinners.Add((cg.transform, new Vector3(.3f, 1f, .2f).normalized, 70f)); mk.Prism(stick, z.At(3f, 4f), up, .2f, 1.2f, 8); ArtistBits.PostCollider(z.root, z.At(3f, 4f), 3.2f, .7f); halos.Add(z.At(3f, 4f) + up * 2.6f, 2.4f, Soda, .35f);
                    break;
                }
                case "Mameyudoufu":
                {
                    var qc = Song(z, "Quality Control"); var moon = Song(z, "Moonheart"); life.nightSongs.Add(moon); var at = z.At(-2.5f, 0);
                    // a block of tofu with a face
                    mk.Box(white, at + up * 1.5f, new Vector3(3.2f, 3f, 2.6f), rot); BoxCol(z.root, at + up * 1.5f, new Vector3(3.2f, 3f, 2.6f), rot);
                    foreach (var e in new[] { -1f, 1f }) { mk.Box(Paint(Ink), at + up * 1.9f + sd * e * .7f + fr * 1.31f, new Vector3(.22f, .34f, .02f), rot); mk.Prism(pinkM, at + up * 1.4f + sd * e * 1.1f + fr * 1.3f, fr, .22f, .02f, 10); }
                    mk.Box(Paint(Ink), at + up * 1.35f + fr * 1.31f, new Vector3(.5f, .08f, .02f), rot);
                    // "Quality Control": a row of sweets under a stamp
                    var belt = z.At(4f, 0); mk.Box(Paint(new Color(.3f, .32f, .5f)), belt + up * .7f, new Vector3(6f, .2f, 1.1f), rot); foreach (var e in new[] { -2.6f, 2.6f }) mk.Box(stick, belt + sd * e + up * .3f, new Vector3(.2f, .6f, 1f), rot); BoxCol(z.root, belt + up * .4f, new Vector3(6f, .8f, 1.1f), rot);
                    for (var i = 0; i < 6; i++) mk.Blob(SweetM(i), belt + sd * (-2.5f + i * 1f) + up * 1f, Vector3.one * .28f, rng, .06f);
                    mk.Box(stick, belt + up * 2.4f, new Vector3(.3f, 1.6f, .3f), rot); mk.Box(SongGlow(qc, Mint, 1.6f), belt + up * 1.55f, new Vector3(.8f, .16f, .8f), rot); mk.Box(stick, belt + up * 3.2f, new Vector3(1.6f, .2f, .4f), rot); foreach (var e in new[] { -.7f, .7f }) mk.Box(stick, belt + sd * e + up * 1.9f - fr * .7f, new Vector3(.16f, 2.8f, .16f), rot);
                    // "Moonheart": a heart-shaped moon on a stick
                    var hm = z.At(-5f, 5f); var hg = SongGlow(moon, new Color(1f, .7f, .8f), 1.6f); mk.Prism(stick, hm, up, .08f, 4f, 6); foreach (var e in new[] { -1f, 1f }) mk.Blob(hg, hm + up * 5f + sd * e * .55f, new Vector3(.75f, .75f, .3f), rng, .04f); mk.Prism(hg, hm + up * 4.75f - fr * .15f, fr, 1f, .3f, 3, 1f, 180f); halos.Add(hm + up * 4.8f, 2.6f, Pink, .35f); ArtistBits.PostCollider(z.root, hm, 3f, .2f);
                    break;
                }
                case "Nokae":
                {
                    var swords = Song(z, "Swords Dance"); var ver = Song(z, "Verity"); var at = z.At(-1.5f, -1f);
                    var sm = new MeshKit(); for (var i = 0; i < 6; i++) { var d = Quaternion.Euler(0, i * 60f, 0) * Vector3.forward * 2.4f; for (var j = 0; j < 6; j++) sm.Box(j % 2 == 0 ? white : SongGlow(swords, Cherry, 1.2f), d + up * (1f + j * .42f), new Vector3(.2f, .42f, .08f), Quaternion.Euler(0, i * 60f, 12f)); sm.Box(Paint(Yellow), d + up * .75f, new Vector3(.7f, .12f, .14f), Quaternion.Euler(0, i * 60f, 12f)); sm.Box(Paint(Ink), d + up * .45f, new Vector3(.14f, .5f, .1f), Quaternion.Euler(0, i * 60f, 12f)); }
                    var sg = Loose(sm, "Swords dancing", at + up * .3f, z.root); life.spinners.Add((sg.transform, up, 55f)); ArtistBits.PostCollider(z.root, at, 3.4f, 2.6f); halos.Add(at + up * 2f, 3.4f, Cherry, .2f);
                    var mr = z.At(3f, 4.5f); mk.Box(SongGlow(ver, new Color(.8f, .95f, 1f), 1f), mr + up * 1.6f, new Vector3(1.5f, 2.6f, .08f), Quaternion.LookRotation(-sd)); mk.Box(Paint(Yellow), mr + up * 1.6f + sd * .03f, new Vector3(1.8f, 2.9f, .1f), Quaternion.LookRotation(-sd)); BoxCol(z.root, mr + up * 1.5f, new Vector3(1.8f, 3f, .2f), Quaternion.LookRotation(-sd));
                    break;
                }
                case "Alesda!":
                {
                    var s = Song(z, "Nikoniko"); var at = z.At(-2f, 0) + up * 3.4f; var m = SongGlow(s, Yellow, 1.3f);
                    mk.Prism(m, at - fr * .25f, fr, 2.8f, .5f, 26); foreach (var e in new[] { -1f, 1f }) mk.Prism(Paint(Ink), at + sd * e * .95f + up * .8f + fr * .26f, fr, .32f, .03f, 12);
                    for (var i = 0; i < 9; i++) { var ang = (200f + i * 17.5f) * Mathf.Deg2Rad; mk.Box(Paint(Ink), at + sd * Mathf.Cos(ang) * 1.6f + up * (Mathf.Sin(ang) * 1.6f + .2f) + fr * .27f, new Vector3(.4f, .22f, .03f), rot * Quaternion.Euler(0, 0, i * 17.5f - 70f)); }
                    foreach (var e in new[] { -1f, 1f }) mk.Prism(pinkM, at + sd * e * 1.9f + fr * .26f, fr, .4f, .03f, 12);
                    mk.Prism(stick, z.At(-2.3f, 0), up, .18f, 1f, 8); ArtistBits.PostCollider(z.root, z.At(-2f, 0), 6f, .5f); halos.Add(at, 4f, Yellow, .3f);
                    break;
                }
                case "Amidst":
                {
                    var s = Song(z, "Glass Petals"); life.glassSong = s; var at = z.At(-1.5f, 0); var m = SongGlow(s, new Color(.8f, .9f, 1f), 1.2f);
                    mk.Prism(Paint(new Color(.5f, .8f, .7f)), at, up, .1f, 2.6f, 6);
                    for (var ring = 0; ring < 2; ring++) for (var i = 0; i < 7; i++) { var d = Quaternion.Euler(0, i * 51.4f + ring * 25f, 0) * fr; var lift = ring == 0 ? .5f : 1.3f; var len = ring == 0 ? 2.2f : 1.4f; var sx = Vector3.Cross(up, d) * .55f; mk.Tri(m, at + up * 2.6f, at + up * (2.6f + lift) + d * len + sx, at + up * (2.6f + lift) + d * len - sx, at); mk.Tri(m, at + up * 2.6f, at + up * (2.6f + lift) + d * len - sx, at + up * (2.6f + lift) + d * len + sx, at + up * 9f); }
                    mk.Blob(GlowM(Lilac, 1.4f), at + up * 2.9f, Vector3.one * .4f, rng, .05f); ArtistBits.PostCollider(z.root, at, 3f, .5f); halos.Add(at + up * 3.2f, 3f, Lilac, .3f);
                    break;
                }
                case "cYsmix":
                {
                    var s = Song(z, "Escapism"); var at = z.At(-2f, 0);
                    // a door standing open on its own, with the sky in it, and a bounce on the mat
                    foreach (var e in new[] { -1f, 1f }) { mk.Box(white, at + sd * e * 1.1f + up * 1.7f, new Vector3(.24f, 3.4f, .3f), rot); ArtistBits.PostCollider(z.root, at + sd * e * 1.1f, 3.4f, .2f); }
                    mk.Box(white, at + up * 3.5f, new Vector3(2.44f, .24f, .3f), rot); mk.Box(SongGlow(s, Soda, 1.5f), at + up * 1.7f - fr * .05f, new Vector3(2f, 3.2f, .04f), rot); for (var i = 0; i < 3; i++) mk.Blob(white, at + up * (1.2f + i * .8f) + sd * (i - 1) * .5f, new Vector3(.4f, .22f, .1f), rng, .1f);
                    mk.Box(pinkM, at + sd * 2.1f + up * 1.7f + fr * .9f, new Vector3(.12f, 3.2f, 1.9f), rot);
                    Jelly(z.At(1.6f, 0), 1.4f, -fr * 16f + up * 15f, Soda);
                    break;
                }
                case "Diveo":
                {
                    var s = Song(z, "Summer Trees"); life.summerSong = s; var at = z.At(-1.5f, 0);
                    mk.Prism(Paint(new Color(.6f, .42f, .3f)), at, up, .34f, 2.8f, 7, .7f); for (var i = 0; i < 7; i++) mk.Blob(i == 0 ? SongGlow(s, Yellow, 1.1f) : GlowM(Yellow, .7f), at + up * (3.4f + (i % 3) * .7f) + Quaternion.Euler(0, i * 51f, 0) * fr * (i == 0 ? 0f : 1.5f), new Vector3(1.4f, 1.1f, 1.4f), rng, .2f);
                    ArtistBits.PostCollider(z.root, at, 3f, .4f); halos.Add(at + up * 4f, 4f, Yellow, .28f);
                    break;
                }
                case "Feryquitous":
                {
                    var s = Song(z, "Monochrome Anomaly"); life.monoSong = s; var at = z.At(-1.5f, 0) + up * 2.4f;
                    var m = new MeshKit(); for (var i = 0; i < 27; i++) { var p = new Vector3(i % 3 - 1, i / 3 % 3 - 1, i / 9 - 1) * .82f; m.Box((i % 2 == 0) ? SongGlow(s, Color.white, 1.2f) : Paint(new Color(.03f, .03f, .04f)), p, Vector3.one * .78f, Quaternion.identity); }
                    var go = Loose(m, "The anomaly", at, z.root); go.transform.rotation = Quaternion.Euler(35f, 20f, 35f); life.spinners.Add((go.transform, new Vector3(.2f, 1f, .4f).normalized, 40f)); mk.Prism(Paint(new Color(.03f, .03f, .04f)), z.At(-1.5f, 0), up, .7f, .5f, 8); ArtistBits.PostCollider(z.root, z.At(-1.5f, 0), 4f, 1.4f);
                    break;
                }
                case "Hyleo":
                {
                    var s = Song(z, "Trigger"); var at = z.At(-1.5f, 0); var d = (fr + up * 1.2f).normalized;
                    // a party popper as big as a cannon: pull the string
                    mk.Prism(SongGlow(s, Yellow, 1.2f), at + up * .6f, d, .3f, 3f, 12, 4f); for (var i = 0; i < 3; i++) mk.Prism(i % 2 == 0 ? pinkM : sodaM, at + up * .6f + d * (.6f + i * .9f), d, .36f + i * .32f, .16f, 12, 1.1f);
                    mk.Box(stick, at + up * .3f, new Vector3(1.2f, .6f, 1.2f), rot); BoxCol(z.root, at + up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), rot);
                    var mouth = at + up * .6f + d * 3.2f; var toy = new GameObject("Party popper"); toy.transform.SetParent(tlife.transform, false); toy.transform.position = at + up * 1.2f - fr * .8f;
                    Interact.Toy(toy, Toy.Kind.Custom, "Pull the string", new Vector3(1.6f, 2f, 1.6f)).onUse = _ => { course.onPad?.Invoke(mouth, HyperCourse.Kind.Geyser); course.onPad?.Invoke(mouth + d * 2f, HyperCourse.Kind.Geyser); };
                    break;
                }
                case "Kizuna AI":
                {
                    var s = Song(z, "future base"); var at = z.At(-2.5f, 0);
                    mk.Box(Paint(Ink), at + up * 2.4f, new Vector3(5f, 3f, .2f), rot); mk.Box(SongGlow(s, Pink, 1.3f), at + up * 2.4f + fr * .11f, new Vector3(4.6f, 2.6f, .02f), rot); BoxCol(z.root, at + up * 2f, new Vector3(5f, 4f, .3f), rot);
                    // the ribbon: two loops and a knot
                    foreach (var e in new[] { -1f, 1f }) { mk.Tri(white, at + up * 2.4f + fr * .13f, at + up * 3.2f + sd * e * 1.6f + fr * .13f, at + up * 1.6f + sd * e * 1.6f + fr * .13f, at - fr); mk.Tri(white, at + up * 2.4f + fr * .13f, at + up * 1.6f + sd * e * 1.6f + fr * .13f, at + up * 3.2f + sd * e * 1.6f + fr * .13f, at - fr); }
                    mk.Box(white, at + up * 2.4f + fr * .14f, new Vector3(.5f, .5f, .04f), rot * Quaternion.Euler(0, 0, 45f)); mk.Box(stick, at + up * .45f, new Vector3(5.4f, .9f, 1.4f), rot); BoxCol(z.root, at + up * .45f + fr * .6f, new Vector3(5.4f, .9f, 1.4f), rot);
                    halos.Add(at + up * 2.4f + fr * .5f, 4f, Pink, .25f);
                    break;
                }
                case "Mitsukiyo":
                {
                    var s = Song(z, "Irodori Canvas"); var at = z.At(-2f, 0);
                    foreach (var e in new[] { -1f, 1f }) mk.Box(Paint(new Color(.7f, .5f, .3f)), at + sd * e * .9f + up * 1.6f - fr * .3f, new Vector3(.1f, 3.4f, .1f), rot * Quaternion.Euler(-10f, 0, e * 6f)); mk.Box(Paint(new Color(.7f, .5f, .3f)), at + up * 1.5f - fr * .9f, new Vector3(.1f, 3.2f, .1f), rot * Quaternion.Euler(18f, 0, 0));
                    mk.Box(white, at + up * 2.1f, new Vector3(2.6f, 2f, .08f), rot * Quaternion.Euler(-10f, 0, 0)); for (var i = 0; i < 5; i++) mk.Box(i == 0 ? SongGlow(s, Pink, 1.2f) : GlowM(Sweet[i], 1f), at + up * (1.5f + i * .28f) + sd * (-.8f + i * .4f) + fr * (.1f - i * .05f), new Vector3(1.4f - i * .2f, .2f, .02f), rot * Quaternion.Euler(-10f, 0, 20f - i * 12f));
                    for (var i = 0; i < 4; i++) mk.Prism(GlowM(Sweet[i], .9f), z.At(.4f, -1.2f + i * .8f), up, .22f, .4f, 8); BoxCol(z.root, at + up * 1.6f - fr * .3f, new Vector3(2.8f, 3.4f, 1.4f), rot);
                    break;
                }
                case "Moon Jelly":
                {
                    var s = Song(z, "Lost"); life.nightSongs.Add(s); var at = z.At(-1.5f, 0);
                    mk.Prism(stick, at, up, .09f, 3.2f, 6); for (var i = 0; i < 5; i++) mk.Box(SweetM(i), at + up * (1.4f + i * .4f) + Quaternion.Euler(0, i * 77f, 0) * fr * .6f, new Vector3(.16f, .28f, 1.3f), Quaternion.Euler(0, i * 77f, 0) * rot); ArtistBits.PostCollider(z.root, at, 3.2f, .3f);
                    for (var i = 0; i < 5; i++)
                    {
                        var m = new MeshKit(); m.Prism(i == 0 ? SongGlow(s, Lilac, 1.4f) : GlowM(i % 2 == 0 ? Soda : Lilac, 1.1f), Vector3.zero, up, .7f, .5f, 10, .3f); for (var j = 0; j < 5; j++) m.Box(GlowM(Color.white, .8f), Quaternion.Euler(0, j * 72f, 0) * Vector3.forward * .4f - up * .7f, new Vector3(.05f, 1.4f, .05f), Quaternion.identity);
                        var p = at + Quaternion.Euler(0, i * 72f, 0) * fr * 3.2f + up * (3f + i * .6f); var go = Loose(m, "Jellyfish", p, z.root); life.floaters.Add((go.transform, p, i * 1.3f)); halos.Add(p, 1.6f, Lilac, .25f);
                    }
                    break;
                }
                case "MOTTO MUSIC":
                {
                    var s = Song(z, "Signs of Rain"); life.rainSong = s; var at = z.At(-1.5f, 0);
                    mk.Prism(stick, at, up, .06f, 2.6f, 6); for (var i = 0; i < 8; i++) { var d0 = Quaternion.Euler(0, i * 45f, 0) * fr; var d1 = Quaternion.Euler(0, i * 45f + 45f, 0) * fr; mk.Tri(i % 2 == 0 ? pinkM : white, at + up * 3.2f, at + up * 2.5f + d0 * 2f, at + up * 2.5f + d1 * 2f, at); mk.Tri(i % 2 == 0 ? pinkM : white, at + up * 3.2f, at + up * 2.5f + d1 * 2f, at + up * 2.5f + d0 * 2f, at + up * 9f); }
                    for (var i = 0; i < 4; i++) mk.Blob(SongGlow(s, new Color(.7f, .8f, 1f), .7f), at + up * 6.5f + Quaternion.Euler(0, i * 90f, 0) * fr * 1.2f, new Vector3(1.5f, .9f, 1.5f), rng, .2f); for (var i = 0; i < 9; i++) mk.Box(SongGlow(s, Soda, 1.4f), at + up * (4f + (i % 3) * .7f) + Quaternion.Euler(0, i * 40f, 0) * fr * (2.4f + (i % 2) * .5f), new Vector3(.05f, .4f, .05f), Quaternion.identity);
                    ArtistBits.PostCollider(z.root, at, 2.8f, .3f); Interact.Seat(tlife.transform, at + up * .2f + fr * .8f, fr, "Shelter");
                    break;
                }
                case "OISHII":
                {
                    var s = Song(z, "Onigiri Freeway"); var at = z.At(-1f, 0);
                    // a strip of road with a boost on it, and a rice ball with wheels
                    mk.Box(Paint(new Color(.2f, .22f, .32f)), at + up * .05f, new Vector3(3.4f, .06f, 10f), Quaternion.LookRotation(sd)); for (var i = -2; i <= 2; i++) mk.Box(GlowM(Yellow, 1.2f), at + sd * i * 2f + up * .09f, new Vector3(.2f, .02f, 1f), Quaternion.LookRotation(sd));
                    Boost(mk, G(at + sd * 3.4f), sd, Yellow, 26f);
                    var car = at - sd * 2.4f + up * .5f; mk.Prism(SongGlow(s, Color.white, .7f), car - fr * .6f, fr, 1.1f, 1.2f, 3, 1f, 180f); mk.Box(Paint(new Color(.06f, .1f, .08f)), car - up * .1f + fr * .62f, new Vector3(1f, .7f, .04f), rot); mk.Box(Paint(new Color(.06f, .1f, .08f)), car - up * .1f - fr * .62f, new Vector3(1f, .7f, .04f), rot);
                    foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) mk.Prism(Paint(Ink), car - up * .4f + sd * e * .6f + fr * q * .66f, fr * q, .28f, .14f, 10); BoxCol(z.root, car + up * .2f, new Vector3(1.6f, 1.6f, 2.2f), rot);
                    break;
                }
                case "Project Skylate":
                {
                    var s = Song(z, "Ryokucha"); var at = z.At(-2f, 0);
                    mk.Prism(Paint(new Color(.9f, .92f, .96f)), at, up, 1.5f, 2.2f, 16, 1.25f); z.solid.Prism(white, at, up, 1.7f, 2.2f, 10); mk.Prism(SongGlow(s, new Color(.55f, .8f, .3f), 1.1f), at + up * 2.05f, up, 1.75f, .04f, 16); mk.Prism(Paint(new Color(.3f, .4f, .7f)), at + up * .9f, up, 1.72f, .3f, 16, 1.03f);
                    for (var i = 0; i < 3; i++) { var m = new MeshKit(); m.Blob(GlowM(Color.white, .5f), Vector3.zero, new Vector3(.4f, .6f, .4f), rng, .2f); var p = at + up * (3f + i * .9f) + sd * (i - 1) * .5f; var go = Loose(m, "Steam", p, z.root); life.floaters.Add((go.transform, p, i * 2f)); }
                    var mat = z.At(1.6f, 0); mk.Box(Paint(new Color(.75f, .8f, .5f)), mat + up * .08f, new Vector3(2.4f, .08f, 1.6f), rot); Interact.Seat(tlife.transform, mat + up * .12f, -fr, "Sit for tea");
                    break;
                }
                case "PSYQUI":
                {
                    var s = Song(z, "Endless"); var at = z.At(-1f, 0) + up * 2.6f; var m = SongGlow(s, Soda, 1.5f);
                    for (var i = 0; i < 40; i++) { var t = i / 40f * Mathf.PI * 2f; var den = 1f + Mathf.Sin(t) * Mathf.Sin(t); var p = at + sd * (3.2f * Mathf.Cos(t) / den) + up * (3.2f * Mathf.Sin(t) * Mathf.Cos(t) / den); mk.Blob(i % 5 == 0 ? white : m, p, Vector3.one * .26f, rng, .03f); }
                    foreach (var e in new[] { -1f, 1f }) { mk.Prism(stick, z.At(-1f, e * 1.6f), up, .08f, 2.6f, 6); ArtistBits.PostCollider(z.root, z.At(-1f, e * 1.6f), 2.6f, .2f); } halos.Add(at, 4f, Soda, .25f);
                    break;
                }
                case "Pure 100%":
                {
                    var s = Song(z, "BREATH"); var at = z.At(-1.5f, 0);
                    mk.Prism(cream, at - up * .1f, up, 2.6f, .5f, 18); mk.Prism(SongGlow(s, new Color(.6f, .85f, 1f), 1f), at + up * .41f, up, 2.3f, .02f, 18); BoxCol(z.root, at + up * .2f, new Vector3(5f, .5f, 5f), rot);
                    for (var i = 0; i < 4; i++) { var m = new MeshKit(); m.Blob(GlowM(Color.white, .35f), Vector3.zero, new Vector3(.9f, .4f, .9f), rng, .25f); var p = at + up * (.9f + i * .5f) + Quaternion.Euler(0, i * 90f, 0) * fr * .8f; var go = Loose(m, "Mist", p, z.root); life.floaters.Add((go.transform, p, i * 1.7f)); }
                    life.milkFeet.Add(at + up * .5f);
                    break;
                }
                case "Snail's House":
                {
                    var s = Song(z, "Snailchan Adventure"); var at = z.At(-1.5f, 0);
                    mk.Blob(GlowM(new Color(1f, .85f, .6f), .5f), at + up * .5f + sd * .4f, new Vector3(2.2f, .5f, .8f), rng, .08f); mk.Blob(GlowM(new Color(1f, .85f, .6f), .5f), at + up * 1.2f + sd * 2.1f, new Vector3(.45f, .8f, .45f), rng, .06f);
                    foreach (var e in new[] { -1f, 1f }) { mk.Box(Paint(new Color(1f, .85f, .6f)), at + up * 2.2f + sd * 2.1f + fr * e * .2f, new Vector3(.06f, .7f, .06f), rot); mk.Blob(Paint(Ink), at + up * 2.6f + sd * 2.1f + fr * e * .2f, Vector3.one * .1f, rng, .02f); }
                    // the shell is a little house: walls, a roof, a door, a lit window
                    var h = at + up * 1.5f - sd * .4f; mk.Box(SongGlow(s, Pink, .6f), h, new Vector3(1.8f, 1.6f, 1.6f), rot); mk.GableRoof(Paint(Cherry), h + up * .8f, new Vector3(2.1f, .1f, 1.9f), .9f, rot); mk.Box(Paint(new Color(.6f, .4f, .3f)), h - up * .3f + fr * .81f, new Vector3(.5f, .9f, .02f), rot); mk.Box(GlowM(Yellow, 1.6f), h + up * .3f + fr * .81f + sd * .5f, new Vector3(.36f, .36f, .02f), rot);
                    mk.Prism(stick, h + up * 1.6f - sd * .6f, up, .03f, 1f, 5); mk.Tri(yellowM, h + up * 2.6f - sd * .6f, h + up * 2.35f - sd * .6f + fr * .5f, h + up * 2.1f - sd * .6f, h - fr); mk.Tri(yellowM, h + up * 2.6f - sd * .6f, h + up * 2.1f - sd * .6f, h + up * 2.35f - sd * .6f + fr * .5f, h + fr * 3f);
                    BoxCol(z.root, at + up * 1.2f, new Vector3(1.8f, 2.4f, 4.4f), Quaternion.LookRotation(sd));
                    break;
                }
                case "t+pazolite":
                {
                    var s = Song(z, "Do you wanna know?"); var at = z.At(-1.5f, 0) + up * 2.2f;
                    var m = new MeshKit(); m.Box(SongGlow(s, Yellow, 1.3f), Vector3.zero, Vector3.one * 2f, Quaternion.identity); foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) { var r2 = Quaternion.LookRotation(d); m.Box(Paint(Ink), d * 1.01f + up * .45f, new Vector3(.7f, .16f, .02f), r2); m.Box(Paint(Ink), d * 1.01f + up * .25f + r2 * Vector3.right * .28f, new Vector3(.16f, .5f, .02f), r2); m.Box(Paint(Ink), d * 1.01f - up * .1f, new Vector3(.16f, .4f, .02f), r2); m.Box(Paint(Ink), d * 1.01f - up * .6f, new Vector3(.18f, .18f, .02f), r2); }
                    var go = Loose(m, "Question box", at, z.root); life.spinners.Add((go.transform, up, 30f)); Jelly(z.At(-1.5f, 0), 1.3f, up * 13f, Yellow); halos.Add(at, 2.6f, Yellow, .3f);
                    break;
                }
                case "Tanchiky":
                {
                    var s = Song(z, "Sanshisuimei"); var at = z.At(-2.5f, 0);
                    foreach (var e in new[] { -1f, 1f }) { mk.Prism(cherryM, at + sd * e * 1.9f, up, .2f, 4.2f, 8); ArtistBits.PostCollider(z.root, at + sd * e * 1.9f, 4.2f, .25f); }
                    mk.Box(cherryM, at + up * 4.3f, new Vector3(5.2f, .3f, .4f), rot); mk.Box(Paint(Ink), at + up * 4.55f, new Vector3(5.8f, .2f, .5f), rot); mk.Box(cherryM, at + up * 3.5f, new Vector3(4.2f, .22f, .3f), rot);
                    // purple hills and clear water behind the gate
                    for (var i = 0; i < 3; i++) mk.Prism(i == 1 ? SongGlow(s, new Color(.6f, .4f, .9f), .9f) : GlowM(new Color(.5f, .36f, .8f), .6f), at - fr * (2.4f + i * .3f) + sd * (i - 1) * 1.8f, up, 1.6f - i * .2f, 2.2f + (i % 2) * 1f, 5, .1f); mk.Prism(GlowM(Soda, .8f), at - fr * 1.2f + up * .05f, up, 2.2f, .04f, 16); ArtistBits.PostCollider(z.root, at - fr * 2.6f, 2.4f, 2.4f);
                    break;
                }
                case "technoplanet":
                {
                    var s = Song(z, "Megastructure"); var at = z.At(-2f, 0); var y = 0f;
                    for (var i = 0; i < 9; i++) { var w = 2.4f - i * .2f; var hh = 1.1f + (i % 3) * .5f; mk.Box(i % 3 == 0 ? SongGlow(s, Soda, 1f) : SweetM(k + i), at + up * (y + hh / 2f) + sd * Mathf.Sin(i * 1.7f) * .25f, new Vector3(w, hh, w), rot * Quaternion.Euler(0, i * 14f, 0)); y += hh; }
                    z.solid.Prism(cream, at, up, 1.5f, y, 8); mk.Prism(stick, at + up * y, up, .04f, 1.4f, 5); mk.Blob(GlowM(Cherry, 1.8f), at + up * (y + 1.5f), Vector3.one * .16f, rng, .02f); halos.Add(at + up * (y + 1.5f), 1.4f, Cherry, .5f);
                    break;
                }
                case "tonytonychopper999":
                {
                    var s = Song(z, "ENERGIZE"); life.energizeSong = s; var at = z.At(-2f, 0);
                    mk.Prism(Paint(new Color(.2f, .24f, .5f)), at, up, 1.3f, 4f, 16); mk.Prism(stick, at + up * 4f, up, .5f, .4f, 12); z.solid.Prism(cream, at, up, 1.3f, 4.4f, 10);
                    for (var i = 0; i < 4; i++) mk.Prism(SongGlow(s, Yellow, 1.7f), at + up * (.4f + i * .9f), up, 1.33f, .7f, 16);
                    // a lightning bolt on the front
                    var b = at + fr * 1.36f + up * 2.2f; mk.Tri(GlowM(Color.white, 1.8f), b + up * 1f + sd * .3f, b - up * .1f - sd * .4f, b + up * .1f + sd * .1f, at); mk.Tri(GlowM(Color.white, 1.8f), b - up * 1f - sd * .3f, b + up * .1f + sd * .4f, b - up * .1f - sd * .1f, at); halos.Add(at + up * 2.4f, 3.4f, Yellow, .35f);
                    Jelly(z.At(2f, 0), 1.4f, up * 26f, Yellow);
                    break;
                }
                default:
                    // anyone new: a gift box with a bow, and a jelly beside it
                    var g = z.At(-1.5f, 0); mk.Box(SweetM(k), g + up * 1f, new Vector3(2f, 2f, 2f), rot); mk.Box(white, g + up * 1f, new Vector3(2.04f, 2.04f, .4f), rot); mk.Box(white, g + up * 1f, new Vector3(.4f, 2.04f, 2.04f), rot); foreach (var e in new[] { -1f, 1f }) mk.Blob(white, g + up * 2.3f + sd * e * .45f, new Vector3(.5f, .3f, .25f), rng, .06f);
                    BoxCol(z.root, g + up * 1f, new Vector3(2f, 2f, 2f), rot); Jelly(z.At(2f, 0), 1.3f, up * 15f, Sweet[k % 4]);
                    break;
            }
        }
    }
}
