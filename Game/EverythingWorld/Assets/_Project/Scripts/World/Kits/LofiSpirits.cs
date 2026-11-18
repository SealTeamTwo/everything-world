using System;
using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills as a rest stop for spirits on a rainy dusk (part of LofiKit): quiet, kind and a little strange.
    /// What makes it look like nowhere else:
    /// - a world in ink where only light has colour: everything that isn't a light is turned to greys and
    ///   blue-blacks, like brushed ink on wet paper (InkOf, MeshKit.Remap); the only colour is what glows. It reads
    ///   by brightness;
    /// - umbrellas where the trees should be, of every size, a dry circle under each with something in it, and one
    ///   enormous umbrella over the bus stop;
    /// - the spirits are lost things: a glove, a scarf, a coin, a cassette, a ticket, a key, each on small legs
    ///   with a quiet life of its own (LofiSpirits);
    /// - puddles that show somewhere else: a small bright day, seen in the ground;
    /// - things the wrong size: a row of doors from knee-high to three storeys, a stool like a thimble beside one
    ///   a metre tall, a bus as big as a house and one that would fit in a shoebox;
    /// - the rest stop itself: a bathhouse steaming in the rain, a noodle counter, and a bus that pulls in with
    ///   nobody driving, waits, and leaves again.
    public partial class LofiKit
    {
        readonly Dictionary<Material, Material> inked = new Dictionary<Material, Material>(); LofiSpirits spirits;
        static readonly Color[] Soft = { new Color(1f, .74f, .46f), new Color(.6f, .82f, 1f), new Color(1f, .62f, .74f), new Color(.66f, .95f, .72f), new Color(.84f, .76f, 1f) };

        /// A colour as ink: its brightness kept (in a dozen steps), its hue thrown away, a little blue in the darks.
        static Color InkColor(Color c)
        {
            var t = Mathf.Round(Mathf.Pow(Mathf.Clamp01(c.grayscale), .85f) * 12f) / 12f;
            return Color.Lerp(new Color(.06f, .07f, .11f), new Color(.68f, .7f, .75f), t);
        }

        /// The ink version of a material; a light keeps its colour (anything whose glow is a good part of its brightness).
        Material InkOf(Material m)
        {
            if (m == null) return null; if (inked.TryGetValue(m, out var r)) return r;
            if (!m.HasProperty("_BaseColor") || m.mainTexture != null && m.shader != null && m.shader.name.Contains("Unlit")) return inked[m] = m;
            var c = m.GetColor("_BaseColor"); var glow = m.IsKeywordEnabled("_EMISSION") ? m.GetColor("_EmissionColor") : Color.black;
            var g = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b)); var b = Mathf.Max(.01f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
            if (g / b > .36f) return inked[m] = m;
            var ink = InkColor(c); return inked[m] = g > 0f ? WorldMaterials.Glow(ink, .12f) : WorldMaterials.Solid(ink);
        }

        /// Turns everything in a kit that is not a light to ink.
        void ToInk(MeshKit k) => k.Remap(InkOf, c => InkColor(c.gamma).linear);

        // ---------------------------------------------------------------- umbrellas where the trees should be
        void Umbrella(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool pale)
        {
            var up = Vector3.up; var canopy = WorldMaterials.Glow(pale ? new Color(.62f, .64f, .7f) : new Color(.16f, .17f, .22f), .12f); var rib = WorldMaterials.Glow(new Color(.34f, .36f, .42f), .12f);
            float h = 2.7f * s, r = 2f * s; mk.Prism(iron, g - up * .2f, up, .045f * s + .02f, h + .9f * s, 6); ArtistBits.PostCollider(ctx.Root, g, Mathf.Min(h, 3f), .08f * s + .05f);
            mk.Cone(canopy, g + up * h, r, .78f * s, 8, 22.5f); mk.Prism(iron, g + up * (h + .78f * s), up, .03f * s + .01f, .3f * s, 4);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * Vector3.forward; mk.Blob(rib, g + up * (h - .02f) + d * r, Vector3.one * (.05f * s + .02f), rng, 0f); }
            // the dry circle under it, and something left there
            var dry = WorldMaterials.Glow(new Color(.5f, .52f, .58f), .1f); mk.Prism(dry, g + up * .02f, up, r * .86f, .02f, 12); noGrass.Add(new Vector3(g.x, r * .8f, g.z));
            if (s < .7f) return; var what = rng.Next(7); var at = g + Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (r * .42f); at.y = ctx.Ground(at.x, at.z);
            switch (what)
            {
                case 0: case 1:   // a lantern: the one colour for a long way round
                    var col = Soft[rng.Next(Soft.Length)]; mk.Prism(iron, at, up, .03f, .5f, 4); mk.Blob(WorldMaterials.Glow(col, 1.7f), at + up * .62f, new Vector3(.14f, .2f, .14f), rng, 0f); halos.Add(at + up * .62f, 1.5f + s * .5f, col, .5f);
                    if (s > 1.5f) ArtistBits.PointLight(ctx.Root, at + up * 1.2f, col, 6f + s * 2f, 1.3f); break;
                case 2: mk.Prism(woodM, at, up, .2f, .44f, 8); Interact.Seat(tlife.transform, at + up * .46f, FaceTo(at, g), "Sit out of the rain").seatHeight = .44f; break;
                case 3: Cat(mk, at, FaceTo(at, g), true, false); break;
                case 4: mk.Box(woodM, at + up * .3f, new Vector3(.7f, .5f, .24f), Quaternion.Euler(0, (float)rng.NextDouble() * 90f, 0)); mk.Box(iron, at + up * .6f, new Vector3(.2f, .06f, .04f), Quaternion.identity); break;   // a suitcase
                case 5: foreach (var e in new[] { -.1f, .1f }) { mk.Box(dark, at + Vector3.right * e + up * .1f, new Vector3(.1f, .2f, .26f), Quaternion.identity); } break;                                                     // a pair of boots
            }
        }

        // ---------------------------------------------------------------- the rest stop
        void RestStop(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(88);
            spirits = new GameObject("Lo-Fi spirits").AddComponent<LofiSpirits>(); spirits.transform.SetParent(life.transform, false); spirits.ground = (x, z) => ctx.Ground(x, z);
            // a free spot near the Corner for each place, the nearest first; each marks its ground so the next keeps off it
            bool Site(float pad, out Vector3 at) { for (var dist = 27f; dist < 110f; dist += 7f) for (var k = 0; k < 16; k++) { var a = (k * 22.5f + dist * 3f) * Mathf.Deg2Rad; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * dist; if (Open(ctx, p.x, p.z, pad) && ctx.IsLand(p.x, p.z, pad + 6f)) { var hi = float.MinValue; var lo = float.MaxValue; for (var q = 0; q < 8; q++) { var y = ctx.Ground(p.x + Mathf.Cos(q * .785f) * pad * .7f, p.z + Mathf.Sin(q * .785f) * pad * .7f); hi = Mathf.Max(hi, y); lo = Mathf.Min(lo, y); } if (hi - lo > 1.6f) continue; at = ctx.OnGround(p.x, p.z); at.y = hi; Mark(at, pad); return true; } } at = default; return false; }

            // ---- the great umbrella over the bus stop: the Corner stays dry
            var pole = c + S * 14.5f; pole.y = ctx.Ground(pole.x, pole.z); var canopyM = WorldMaterials.Glow(new Color(.13f, .14f, .19f), .12f);
            mk.Prism(iron, pole - up * .3f, up, .34f, 15.5f, 8); ArtistBits.PostCollider(ctx.Root, pole, 4f, .4f); var top = new Vector3(c.x, pole.y + 13f, c.z);
            mk.Cone(canopyM, top, 20f, 5f, 12, 15f); mk.Prism(iron, pole + up * 15f, (top + up * 4.4f - (pole + up * 15f)).normalized, .22f, Vector3.Distance(top + up * 4.4f, pole + up * 15f), 6);
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f + 15f, 0) * Vector3.forward; mk.Prism(iron, top + up * 4.6f, (d * 20f - up * 4.8f).normalized, .07f, 20.6f, 4); for (var q = 1; q <= 4; q++) { var bp = top + up * 4.6f + (d * 20f - up * 4.8f) * (q / 4.4f) - up * .3f; mk.Blob(lampM, bp, Vector3.one * .1f, rng, 0f); } }
            halos.Add(top + up * 1f, 14f, Lamp, .16f); IndoorSpaces.Add(new Vector3(c.x, c.y + 6f, c.z), Quaternion.identity, new Vector3(27f, 12f, 27f));

            // ---- the bathhouse, steaming in the rain: a low wide building, a curtain over each door (one door for the small, one for the tall)
            if (Site(11f, out var bg))
            {
                var f = new PlaceFrame(bg, FaceTo(bg, c)); const float W = 15f, Dp = 9f, H = 4f; var wall = Paint(new Color(.78f, .76f, .7f)); var roof = Paint(new Color(.2f, .2f, .26f));
                mk.Box(wall, f.P(0, H / 2f - .5f, 0), new Vector3(W, H + 1f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .5f, 0), new Vector3(W, H + 1f, Dp), f.r); mk.GableRoof(roof, f.P(0, H, 0), new Vector3(W + 1.6f, 0, Dp + 1.6f), 2.2f, f.r);
                foreach (var (x, dh) in new[] { (-3.6f, 1.25f), (3.2f, 3.3f) }) { mk.Box(dark, f.P(x, dh / 2f, Dp / 2f + .02f), new Vector3(1.5f, dh, .06f), f.r); for (var k = -1; k <= 1; k++) mk.Box(Paint(new Color(.2f, .24f, .4f)), f.P(x + k * .5f, dh - .35f, Dp / 2f + .12f), new Vector3(.46f, .7f, .03f), f.r); mk.Blob(GlowM(Soft[0], 2.2f), f.P(x, dh - .34f, Dp / 2f + .16f), new Vector3(.12f, .12f, .02f), rng, 0f); }
                for (var k = -2; k <= 2; k++) mk.Box(windowM, f.P(k * 2.6f, H - .7f, Dp / 2f + .03f), new Vector3(1.5f, .6f, .05f), f.r); Sign(f.P(0, H + .9f, Dp / 2f + .9f), f.Fwd, "BATHS", .7f, Cream);
                var ch = f.P(W / 2f - 1.6f, 0, -Dp / 2f + 1.6f); mk.Prism(Paint(new Color(.5f, .48f, .46f)), ch + up * H, up, .7f, 7f, 8, .8f); spirits.steam.Add(ch + up * (H + 7.2f)); spirits.steam.Add(f.P(-2f, H + 2.3f, 0));
                Bench(tlife.transform, mk, f.P(0, 0, Dp / 2f + 1.2f), f.Fwd, 2.4f); for (var k = 0; k < 3; k++) mk.Prism(woodM, f.P(5.6f + (k % 2) * .5f, k / 2 * .34f, Dp / 2f + 1f), up, .24f, .32f, 8);
                ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, Dp / 2f + 2.4f), Lamp, 16f, 1.7f); halos.Add(f.P(0, H - .7f, Dp / 2f + .6f), 6f, Lamp, .2f);
                Folk(f.P(-6.2f, 0, Dp / 2f + 1.3f), f.Fwd, Resident.Pose.Stand, new[] { "Baths are for anybody who's been left somewhere. That's most of us, if we're honest.", "Small door for the small ones. Tall door for the tall. Mind your head, or mind your knees.", "They come in off the bus cold and they go back out warm. That's the whole business." });
                Shot(ctx, "the bathhouse", f.P(3f, 2f, Dp / 2f + 13f), f.P(0, 3f, 0));
            }
            // ---- the noodle counter: a stall, four stools, a red lantern, a pot that never stops
            if (Site(6f, out var ng))
            {
                var f = new PlaceFrame(ng, FaceTo(ng, c)); mk.Box(woodM, f.P(0, .5f, 0), new Vector3(4.6f, 1f, .8f), f.r); Col(f.P(0, .5f, 0), new Vector3(4.6f, 1f, .8f), f.r); mk.Box(woodM, f.P(0, 1.02f, .1f), new Vector3(4.9f, .06f, 1.1f), f.r);
                foreach (var e in new[] { -2.2f, 2.2f }) { mk.Prism(woodM, f.P(e, 0, -.9f), up, .06f, 2.7f, 5); mk.Prism(woodM, f.P(e, 0, .9f), up, .06f, 2.4f, 5); } mk.Box(Paint(new Color(.2f, .2f, .26f)), f.P(0, 2.6f, 0), new Vector3(5.2f, .06f, 2.6f), f.r * Quaternion.Euler(-7f, 0, 0));
                var red = new Color(1f, .34f, .26f); mk.Blob(GlowM(red, 2.6f), f.P(2.3f, 2f, 1f), new Vector3(.22f, .3f, .22f), rng, 0f); halos.Add(f.P(2.3f, 2f, 1f), 2.6f, red, .55f); ArtistBits.PointLight(ctx.Root, f.P(1.6f, 2f, 1.4f), red, 9f, 1.5f);
                mk.Prism(iron, f.P(-1.2f, 1.05f, -.1f), up, .3f, .34f, 10); spirits.steam.Add(f.P(-1.2f, 1.5f, -.1f)); for (var k = 0; k < 3; k++) mk.Prism(Paint(Cream), f.P(.2f + k * .7f, 1.05f, .2f), up, .13f, .1f, 8, 1.4f);
                for (var k = 0; k < 4; k++) { var sp = f.P(-1.7f + k * 1.15f, 0, 1.15f); mk.Prism(woodM, sp, up, .17f, .62f, 8); Interact.Seat(tlife.transform, sp + up * .64f, -f.Fwd, "Sit at the counter").seatHeight = .62f; }
                // the wrong-size stools: one a metre tall, one like a thimble
                mk.Prism(woodM, f.P(3.6f, 0, 1.2f), up, .26f, 1.06f, 8); Interact.Seat(tlife.transform, f.P(3.6f, 1.08f, 1.2f), -f.Fwd, "Climb onto the tall stool").seatHeight = 1.06f; mk.Prism(woodM, f.P(4.1f, 0, 1.3f), up, .03f, .06f, 6);
                var cook = Local(Residents.Tool.Spatula, true); Folk(f.P(.4f, 0, -.9f), f.Fwd, Resident.Pose.Work, new[] { "One size of bowl. Everybody finishes it, whatever size they are. I don't ask how.", "The glove always has the same thing. It can't hold chopsticks. It manages.", "Sit. The bus isn't for a while. It's never for a while." }, cook);
                Shot(ctx, "the noodle counter", f.P(2f, 1.8f, 6f), f.P(0, 1.4f, 0));
            }
            // ---- the row of doors: knee-high to three storeys, all of them shut, light under every one
            if (Site(9f, out var dg))
            {
                var f = new PlaceFrame(dg, FaceTo(dg, c)); float x = -6.4f; float[] hs = { .7f, 1.3f, 2.1f, 3.6f, 6f }; var frame = Paint(new Color(.2f, .2f, .26f));
                for (var k = 0; k < hs.Length; k++)
                {
                    var hh = hs[k]; var w = hh * .46f; x += w / 2f + .5f; mk.Box(frame, f.P(x, hh / 2f, 0), new Vector3(w + hh * .1f, hh + hh * .05f, .2f), f.r); mk.Box(Paint(new Color(.7f, .7f, .74f)), f.P(x, hh / 2f, .08f), new Vector3(w, hh, .1f), f.r); Col(f.P(x, hh / 2f, 0), new Vector3(w + hh * .1f, hh, .24f), f.r);
                    var sc = Soft[k % Soft.Length]; mk.Blob(GlowM(sc, 2f), f.P(x + w * .34f, hh * .48f, .15f), Vector3.one * hh * .03f, rng, 0f); mk.Box(GlowM(sc, 2.2f), f.P(x, .02f, .16f), new Vector3(w * .9f, .03f, .1f), f.r); halos.Add(f.P(x, .1f, .5f), w * 1.2f + .4f, sc, .4f);
                    x += w / 2f;
                }
                string[] knock = { "You knock. Something the size of a button says \"occupied\".", "This one is warm. Somebody tall is having a bath behind it.", "Locked, politely.", "The handle is level with your knee. You feel enormous.", "The handle is over your head. You feel like a lost glove." }; var kn = 0;
                var kg = new GameObject("The row of doors"); kg.transform.SetParent(tlife.transform, false); kg.transform.position = f.P(0, 1.2f, .8f); Interact.Toy(kg, Toy.Kind.Custom, "Knock on a door", new Vector3(13f, 2.4f, 1.6f)).onUse = _ => Say(tlife.transform, f.P(0, 3.2f, .6f), knock[kn++ % knock.Length], 5f);
                Shot(ctx, "the row of doors", f.P(-1f, 1.8f, 11f), f.P(1f, 2.4f, 0));
            }
            // ---- the bus as big as a house, parked with its lights on; and the one that would fit in a shoebox, which is still running
            if (Site(12f, out var hg))
            {
                var f = new PlaceFrame(hg, FaceTo(hg, c)); BusMesh(mk, f.P(0, 0, 0), f.r, 3.2f, rng); Col(f.P(0, 4.6f, 0), new Vector3(8.2f, 9.2f, 26f), f.r);
                ArtistBits.PointLight(ctx.Root, f.P(6f, 4f, 0), Lamp, 18f, 1.6f); spirits.tiny = TinyBus(f.P(7.5f, 0, 9f), rng);
                Shot(ctx, "the bus as big as a house", f.P(15f, 2f, 15f), f.P(0, 5f, 0));
            }
            // ---- the bus that pulls in at the stop with nobody driving, waits, and leaves
            var bm = new MeshKit(); BusMesh(bm, Vector3.zero, Quaternion.identity, 1f, rng); ToInk(bm); var bgo = bm.BuildSingle("The bus nobody drives", spirits.transform, false); bgo.isStatic = false;
            spirits.bus = bgo.transform; spirits.stop = c + D * 15.5f; spirits.along = S; spirits.busLight = ArtistBits.PointLight(spirits.transform, Vector3.zero, new Color(1f, .9f, .7f), 14f, 0f);

            // ---- puddles that show somewhere else, beside the lanes
            var shown = 0; for (var k = 7; k < walkPoints.Count && shown < 16; k += 11) { var p = walkPoints[k].p + Quaternion.Euler(0, k * 37f, 0) * Vector3.forward * 2.6f; if (!ctx.IsLand(p.x, p.z, 3f) || InFootprint(ctx, p.x, p.z, 1.5f) || InNoGrass(p.x, p.z, 1.6f) || NearPath(p.x, p.z, 1.5f)) continue; Elsewhere(mk, ctx.OnGround(p.x, p.z), 1.1f + (k % 3) * .35f, k, rng); shown++; }
            // ---- the spirits: lost things, each near something
            var made = 0; for (var k = 3; k < walkPoints.Count && made < 26; k += 13) { var p = walkPoints[k].p + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * 3.4f; if (!ctx.IsLand(p.x, p.z, 4f) || InFootprint(ctx, p.x, p.z, 1f)) continue; Spirit(made, ctx.OnGround(p.x, p.z), rng); made++; }
            Debug.Log($"The Lo-Fi Hills: {made} lost things about, {shown} puddles that show somewhere else");
        }

        /// A puddle that shows a bright day somewhere else: blue sky, a sun, the edge of a green hill.
        void Elsewhere(MeshKit mk, Vector3 g, float r, int i, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(GlowM(new Color(.5f, .8f, 1f), 1.5f), g + up * .03f, up, r, .012f, 12); var d = Quaternion.Euler(0, i * 40f, 0) * Vector3.forward;
            mk.Prism(GlowM(new Color(1f, .9f, .4f), 2.4f), g + up * .045f + d * r * .3f, up, r * .2f, .01f, 10); mk.Prism(GlowM(new Color(.4f, .86f, .4f), 1.3f), g + up * .04f - d * r * .52f, up, r * .44f, .01f, 8); mk.Blob(GlowM(Color.white, 1.4f), g + up * .05f + Quaternion.Euler(0, 90f, 0) * d * r * .4f, new Vector3(r * .22f, .004f, r * .1f), rng, 0f);
            halos.Add(g + up * .3f, r * 1.8f, new Color(.7f, .9f, 1f), .3f); noGrass.Add(new Vector3(g.x, r, g.z));
        }

        /// A bus, 's' times life size (the one at the stop is 1; the parked one is as big as a house).
        void BusMesh(MeshKit m, Vector3 o, Quaternion rot, float s, System.Random rng)
        {
            Vector3 P(float x, float y, float z) => o + rot * (new Vector3(x, y, z) * s); var body = Paint(new Color(.74f, .76f, .8f)); var band = Paint(new Color(.3f, .32f, .4f));
            m.Box(body, P(0, 1.55f, 0), new Vector3(2.5f, 2.5f, 8f) * s, rot); m.Box(band, P(0, .7f, 0), new Vector3(2.54f, .5f, 8.04f) * s, rot); m.Box(band, P(0, 2.86f, 0), new Vector3(2.3f, .14f, 7.6f) * s, rot);
            foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k < 5; k++) m.Box(windowM, P(e * 1.26f, 1.95f, -2.9f + k * 1.4f), new Vector3(.04f, .9f, 1.1f) * s, rot); foreach (var zz in new[] { -2.6f, 2.6f }) m.Prism(dark, P(e * 1.1f, .44f, zz), rot * Vector3.right * e, .44f * s, .3f * s, 10); }
            m.Box(windowM, P(0, 2f, 4.02f), new Vector3(2.1f, 1f, .04f) * s, rot); m.Box(lampM, P(0, 2.66f, 4.02f), new Vector3(1.4f, .22f, .04f) * s, rot); foreach (var e in new[] { -.9f, .9f }) m.Blob(lampM, P(e, .8f, 4.04f), Vector3.one * .12f * s, rng, 0f);
            m.Box(dark, P(1.27f, 1.2f, 2.9f), new Vector3(.04f, 1.9f, .9f) * s, rot);   // the door, open, nobody in the seat behind it
        }

        Transform TinyBus(Vector3 at, System.Random rng)
        {
            var m = new MeshKit(); BusMesh(m, Vector3.zero, Quaternion.identity, .06f, rng); ToInk(m); var go = m.BuildSingle("The bus that would fit in a shoebox", spirits.transform, false); go.isStatic = false; go.transform.position = at; spirits.tinyHome = at; return go.transform;
        }

        static readonly string[] Lost = { "glove", "scarf", "coin", "cassette", "ticket", "key", "sock", "button" };
        static readonly string[][] LostSay =
        {
            new[] { "A left glove, lost on the 43 one February. It waves, which is the one thing it is good at.", "The glove pats the air beside it, where the other one would be, and seems all right about it." },
            new[] { "A scarf somebody's grandmother made. It wraps once round your ankle, to see if you're cold, and lets go.", "The scarf is on its way to the baths. It walks like it has all night. It has." },
            new[] { "A coin that rolled under a seat in 1987. It spins once, lands the way it wanted, and bows.", "The coin has saved up for a bowl of noodles. It is the coin. It hasn't worked that part out." },
            new[] { "A cassette with one side recorded. It hums the three seconds it remembers best.", "The cassette rewinds itself with one small foot, thinking." },
            new[] { "A ticket, clipped once, for a train that went without it. It shows you the hole, quite proud.", "The ticket is waiting for the bus. It has the wrong ticket. It is the wrong ticket." },
            new[] { "A key to a door nobody has. It goes along the row of doors every evening, trying.", "The key jingles, though there is only one of it." },
            new[] { "One sock. It has stopped looking for the other and taken up walking.", "The sock steps round the puddle, then gives in and steps in it." },
            new[] { "A coat button, still holding a thread. It nods to you, being round, by rolling slightly.", "The button counts the umbrellas as it goes. It has four holes to count with." },
        };

        /// A lost thing on two small legs, glowing a little in a colour of its own.
        void Spirit(int i, Vector3 at, System.Random rng)
        {
            var kind = i % Lost.Length; var col = Soft[i % Soft.Length]; var body = new Material(WorldMaterials.Glow(col, .9f)) { name = "Lost " + Lost[kind] }; var m = new MeshKit(); var up = Vector3.up; const float y = .26f;
            switch (kind)
            {
                case 0: m.Blob(body, new Vector3(0, y + .2f, 0), new Vector3(.16f, .2f, .07f), rng, 0f); for (var k = 0; k < 4; k++) m.Prism(body, new Vector3(-.11f + k * .074f, y + .34f, 0), up, .03f, .16f + (k == 1 || k == 2 ? .04f : 0f), 5); m.Prism(body, new Vector3(-.16f, y + .16f, 0), new Vector3(-.6f, .6f, 0), .03f, .13f, 5); break;
                case 1: for (var k = 0; k < 5; k++) m.Box(body, new Vector3(Mathf.Sin(k * 1.3f) * .05f, y + .08f + k * .13f, 0), new Vector3(.2f, .13f, .05f), Quaternion.Euler(0, 0, Mathf.Sin(k * 1.3f) * 12f)); m.Box(body, new Vector3(.12f, y + .1f, .03f), new Vector3(.12f, .3f, .04f), Quaternion.Euler(0, 0, -20f)); break;
                case 2: m.Prism(body, new Vector3(0, y + .22f, -.02f), Vector3.forward, .22f, .04f, 14); m.Prism(dark, new Vector3(0, y + .22f, .021f), Vector3.forward, .15f, .004f, 12); break;
                case 3: m.Box(body, new Vector3(0, y + .16f, 0), new Vector3(.42f, .27f, .06f), Quaternion.identity); foreach (var e in new[] { -.1f, .1f }) m.Prism(dark, new Vector3(e, y + .17f, .031f), Vector3.forward, .05f, .004f, 8); break;
                case 4: m.Box(body, new Vector3(0, y + .2f, 0), new Vector3(.22f, .4f, .02f), Quaternion.identity); m.Prism(dark, new Vector3(.06f, y + .3f, .011f), Vector3.forward, .025f, .004f, 6); break;
                case 5: m.Prism(body, new Vector3(0, y + .38f, -.015f), Vector3.forward, .09f, .03f, 10); m.Box(body, new Vector3(0, y + .16f, 0), new Vector3(.04f, .34f, .03f), Quaternion.identity); m.Box(body, new Vector3(.05f, y + .04f, 0), new Vector3(.1f, .04f, .03f), Quaternion.identity); m.Box(body, new Vector3(.04f, y + .12f, 0), new Vector3(.08f, .04f, .03f), Quaternion.identity); break;
                case 6: m.Box(body, new Vector3(0, y + .24f, 0), new Vector3(.15f, .36f, .1f), Quaternion.identity); m.Box(body, new Vector3(.08f, y + .06f, 0), new Vector3(.28f, .13f, .1f), Quaternion.identity); break;
                default: m.Prism(body, new Vector3(0, y + .2f, -.02f), Vector3.forward, .17f, .04f, 12); foreach (var (x, yy) in new[] { (-.05f, -.05f), (.05f, -.05f), (.05f, .05f), (-.05f, .05f) }) m.Prism(dark, new Vector3(x, y + .2f + yy, .021f), Vector3.forward, .022f, .004f, 6); break;
            }
            foreach (var e in new[] { -.06f, .06f }) { m.Prism(dark, new Vector3(e, .04f, 0), up, .016f, y, 4); m.Box(dark, new Vector3(e, .02f, .03f), new Vector3(.05f, .04f, .1f), Quaternion.identity); }
            var go = m.BuildSingle("Lost " + Lost[kind], spirits.transform, false); go.isStatic = false; var sc = 1.3f + (i % 3) * .35f; go.transform.localScale = Vector3.one * sc; go.transform.position = at;
            var n = 0; var lines = LostSay[kind]; Interact.Toy(go, Toy.Kind.Custom, "Say hello to the lost " + Lost[kind], new Vector3(.8f, 1f, .8f)).onUse = _ => Say(tlife.transform, go.transform.position + up * (1.2f * sc), lines[n++ % lines.Length], 6f);
            spirits.Add(go.transform, at, 5f + (i % 4) * 3f, i);
        }
    }

    /// The lost things walking about, the bus that nobody drives, the tiny bus on its round, the steam.
    public class LofiSpirits : MonoBehaviour
    {
        const string Land = "The Lo-Fi Hills";
        class One { public Transform t; public Vector3 home, from, to; public float reach, wait, walk, seed; }
        readonly List<One> all = new List<One>();
        public Func<float, float, float> ground; public readonly List<Vector3> steam = new List<Vector3>();
        public Transform bus, tiny; public Vector3 stop, along, tinyHome; public Light busLight;
        ParticleSystem puffs; float nextPuff;

        public void Add(Transform t, Vector3 home, float reach, int i) => all.Add(new One { t = t, home = home, from = home, to = home, reach = reach, wait = 1f + i % 5, seed = i * 1.7f });

        void Start()
        {
            var go = new GameObject("Steam"); go.transform.SetParent(transform, false); puffs = go.AddComponent<ParticleSystem>(); puffs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = puffs.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(.7f, 1.6f); main.startColor = new Color(.86f, .88f, .94f, .16f); main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            var e = puffs.emission; e.rateOverTime = 0f; var sz = puffs.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .6f, 1f, 2.6f));
            var cl = puffs.colorOverLifetime; cl.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) }); cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); } tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Steam" }; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex); var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime; var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            // the bus: one comes every seventy seconds, the same for everyone; it rolls in, stands at the stop with its door open and its lights on, and goes
            if (bus != null)
            {
                var u = (float)((SharedChance.Now % 70.0) / 70.0); float s;   // where along its line, in metres from the stop
                if (u < .3f) s = Mathf.Lerp(-150f, 0f, 1f - (1f - u / .3f) * (1f - u / .3f)); else if (u < .48f) s = 0f; else if (u < .8f) { var k = (u - .48f) / .32f; s = 150f * k * k; } else s = 9999f;
                var on = s < 9000f; if (bus.gameObject.activeSelf != on) bus.gameObject.SetActive(on);
                if (on) { var p = stop + along * s; p.y = (ground != null ? ground(p.x, p.z) : stop.y) + .1f; bus.SetPositionAndRotation(p, Quaternion.LookRotation(along)); if (busLight != null) { busLight.transform.position = p + Vector3.up * 2.4f; busLight.intensity = 1.6f; } } else if (busLight != null) busLight.intensity = 0f;
            }
            if (!here) return;
            // the bus that would fit in a shoebox, going round and round a spot the size of a plate
            if (tiny != null) { var a = t * .9f; var p = tinyHome + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .9f; p.y = (ground != null ? ground(p.x, p.z) : tinyHome.y) + .02f; tiny.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)))); }
            // the lost things: a little way, a long pause, a little way back
            foreach (var o in all)
            {
                if (o.t == null) continue;
                if (o.wait > 0f) { o.wait -= dt; if (o.wait <= 0f) { var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); var r = UnityEngine.Random.Range(1.5f, o.reach); o.from = o.t.position; o.to = o.home + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; o.walk = 0f; } continue; }
                var dist = Mathf.Max(.1f, Vector3.Distance(new Vector3(o.from.x, 0, o.from.z), new Vector3(o.to.x, 0, o.to.z))); o.walk += dt * .55f / dist;
                var p = Vector3.Lerp(o.from, o.to, Mathf.SmoothStep(0f, 1f, o.walk)); p.y = (ground != null ? ground(p.x, p.z) : o.home.y) + Mathf.Abs(Mathf.Sin(t * 7f + o.seed)) * .05f;
                var dir = o.to - o.from; dir.y = 0; o.t.position = p; if (dir.sqrMagnitude > .01f) o.t.rotation = Quaternion.Slerp(o.t.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, Mathf.Sin(t * 7f + o.seed) * 5f), dt * 5f);
                if (o.walk >= 1f) o.wait = UnityEngine.Random.Range(2.5f, 8f);
            }
            if (puffs != null && t >= nextPuff && steam.Count > 0) { nextPuff = t + .12f; var at = steam[UnityEngine.Random.Range(0, steam.Count)]; var cam = Camera.main; if (cam == null || (cam.transform.position - at).sqrMagnitude < 140f * 140f) puffs.Emit(new ParticleSystem.EmitParams { position = at + UnityEngine.Random.insideUnitSphere * .3f, velocity = new Vector3(.2f, 1.1f, .1f) }, 1); }
        }
    }
}
