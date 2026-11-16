using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The glass and water that the paper is set against (part of FarKit, 2026-11-16): a clear glass stream running
    /// down beside the Promenade to the sea with paper boats sailing on it ("Águas de Março"); a giant wave of glass
    /// curling over the bottom of the Promenade that you walk under; a ship in a bottle the size of a room lying on the
    /// beach; glass rock pools with a paper starfish, and a message in a bottle to pick up. The paper is soft and matte;
    /// the glass catches the sun and glows.
    public partial class FarKit
    {
        Material glassWater, glassClear, glassDeep; float waveD;
        readonly List<Vector3> stream = new List<Vector3>();

        void GlassMaterials()
        {
            glassWater = new Material(WorldMaterials.Glow(new Color(.45f, .86f, .96f), .55f)) { name = "Far glass water" };
            glassClear = WorldMaterials.Glow(new Color(.72f, .95f, 1f), .5f); glassDeep = WorldMaterials.Glow(new Color(.26f, .64f, .82f), .4f);
            life.glassWater = glassWater;
        }

        bool UnderWave(float d) => roadL > 60f && Mathf.Abs(d - waveD) < 9f;

        /// The stream: from a little glass spring north-west of the Compass, down beside the Promenade, into the sea.
        void GlassStream(LandContext ctx, MeshKit mk)
        {
            stream.Clear(); if (roadL < 60f) return;
            for (var d = 22f; d < roadL + 120f; d += 1.5f)
            {
                var u = 17f + Mathf.Sin(d / 15f) * 2f + Mathf.Sin(d / 6.3f) * .5f; var p = Wp(u, -d);
                if (ctx.Data.IslandAt(p.x, p.z) == null || ctx.Ground(p.x, p.z) < waterY - .4f) break;
                if (InFootprint(ctx, p.x, p.z, 1.5f)) continue;
                stream.Add(ctx.OnGround(p.x, p.z));
            }
            if (stream.Count < 4) return;
            Strip(mk, stream, glassWater, 2.6f, .05f, ctx);
            var rng = new System.Random(77);
            for (var i = 0; i < stream.Count; i += 1)
            {
                var a = stream[Mathf.Min(i + 1, stream.Count - 1)] - stream[Mathf.Max(0, i - 1)]; a.y = 0; var side = Vector3.Cross(Vector3.up, a.normalized);
                if (i % 2 == 0) foreach (var e in new[] { -1f, 1f }) { var q = stream[i] + side * e * (1.6f + (float)rng.NextDouble() * .3f); q.y = ctx.Ground(q.x, q.z); mk.Blob(rng.Next(3) == 0 ? PaperM(PaperTints[2]) : paperEdge, q + Vector3.up * .08f, new Vector3(.28f, .14f, .22f) * (.7f + (float)rng.NextDouble() * .6f), rng, .2f); }
                if (i % 2 == 0) noGrass.Add(new Vector3(stream[i].x, 2.2f, stream[i].z));
            }
            // the spring: a glass basin the stream rises from
            var sp = stream[0]; mk.Prism(paperCream, sp - Vector3.up * .2f, Vector3.up, 2.4f, .45f, 16); mk.Prism(glassWater, sp + Vector3.up * .22f, Vector3.up, 2.1f, .04f, 16); ArtistBits.PostCollider(ctx.Root, sp, .3f, 2.4f);
            halos.Add(sp + Vector3.up * .5f, 2.6f, new Color(.5f, .9f, 1f), .4f); noGrass.Add(new Vector3(sp.x, 3.4f, sp.z));
            // paper boats sailing down it
            life.stream = stream;
            for (var k = 0; k < 6; k++) { var bm = new MeshKit(); PaperBoat(bm, Vector3.zero, .7f, PaperM(PaperTints[(k * 3) % PaperTints.Length])); life.streamBoats.Add(bm.BuildSingle("Paper boat", life.transform, false).transform); }
            Debug.Log($"The Far Shores: glass stream {stream.Count * 1.5f:0} m with 6 paper boats");
        }

        /// A giant wave of glass curling over the bottom of the Promenade: it rises beside the walk on the north side and
        /// breaks over it in a curl of paper foam, so you walk under it on the way to the beach.
        void GlassWave(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (roadL <= 60f) return;
            Vector2[] prof = { new Vector2(8.6f, -.3f), new Vector2(8.9f, 2.5f), new Vector2(8.3f, 5f), new Vector2(6.6f, 7.2f), new Vector2(3.9f, 8.4f), new Vector2(1.1f, 8.2f), new Vector2(-1.5f, 7f), new Vector2(-2.9f, 5.4f), new Vector2(-2.7f, 4.3f), new Vector2(-1.8f, 3.9f) };
            const int seg = 8; const float span = 14f;
            Vector3 P(float u, float y, float s) { var g = G(ctx, 0f, -waveD + s); return Wp(u, -waveD + s) + Vector3.up * (g.y - center.y + y); }
            for (var j = 0; j < seg; j++)
            {
                float s0 = -span / 2f + span * j / seg, s1 = -span / 2f + span * (j + 1) / seg;
                for (var i = 0; i + 1 < prof.Length; i++)
                {
                    Vector3 a = P(prof[i].x, prof[i].y, s0), b = P(prof[i + 1].x, prof[i + 1].y, s0), c = P(prof[i + 1].x, prof[i + 1].y, s1), d = P(prof[i].x, prof[i].y, s1);
                    var nrm = Vector3.Cross(b - a, d - a).normalized; var mid = (a + c) / 2f; var m = i < 3 ? glassDeep : i < 6 ? glassWater : glassClear;
                    mk.Quad(m, a, b, c, d, mid - nrm); mk.Quad(m, a, b, c, d, mid + nrm);
                }
            }
            // paper foam along the crest and the curl
            var rng = new System.Random(12);
            for (var s = -span / 2f + .4f; s < span / 2f; s += .7f) for (var i = 4; i < prof.Length; i++) if (rng.NextDouble() < .7) mk.Blob(paperEdge, P(prof[i].x, prof[i].y + .15f, s + ((float)rng.NextDouble() - .5f) * .4f), new Vector3(.35f, .22f, .35f) * (.7f + (float)rng.NextDouble() * .6f), rng, .3f);
            var foot = P(8.7f, 1.5f, 0f); solid.Box(paperCream, foot, new Vector3(span, 3f, 1.4f), Quaternion.identity);
            var inCurl = P(2f, 5.5f, 0f); ArtistBits.PointLight(ctx.Root, inCurl, new Color(.5f, .9f, 1f), 18f, 2.2f); halos.Add(inCurl, 5f, new Color(.5f, .9f, 1f), .35f);
            noGrass.Add(new Vector3(foot.x, 6f, foot.z));
            Shot(ctx, "the glass wave", P(-1f, 1.7f, span / 2f + 9f), P(2f, 5f, 0f));
        }

        /// On the beach: a ship in a bottle the size of a room, glass rock pools, a message in a bottle.
        void BeachGlass(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (roadL < 30f) return;
            var up = Vector3.up; var c = ctx.OnGround(beach2.x, beach2.y); var rng = new System.Random(91);
            // the bottle, lying along the shore: glass ribs and rings, a neck and a cork, and a paper ship inside
            var o = c - U * 13f + R * 9f; o.y = ctx.Ground(o.x, o.z); const float Rb = 1.7f, L0 = -4.5f, L1 = 3f; var ax = o + up * (Rb + .05f);
            for (var k = 0; k < 12; k++)
            {
                var a = k * 30f * Mathf.Deg2Rad; var off = (R * Mathf.Cos(a) + up * Mathf.Sin(a)) * Rb;
                mk.Prism(glassClear, ax + off + U * L0, U, .07f, L1 - L0, 5);
                var sh = ax + off + U * L1; var neck = ax + (R * Mathf.Cos(a) + up * Mathf.Sin(a)) * .5f + U * (L1 + 1.4f); mk.Prism(glassClear, sh, (neck - sh).normalized, .06f, Vector3.Distance(sh, neck), 5);
            }
            foreach (var z in new[] { L0, -1.5f, 1.5f, L1 }) for (var k = 0; k < 20; k++) { var a = k * 18f * Mathf.Deg2Rad; var p = ax + U * z + (R * Mathf.Cos(a) + up * Mathf.Sin(a)) * Rb; mk.Box(glassWater, p, new Vector3(.1f, .1f, .34f), Quaternion.LookRotation(Vector3.Cross(U, (p - ax - U * z).normalized), (p - ax - U * z).normalized)); }
            mk.Prism(glassDeep, ax + U * L0 - U * .05f, U, Rb, .1f, 20); mk.Prism(glassWater, ax + U * (L1 + 1.4f), U, .52f, 1.3f, 12); mk.Prism(woodM, ax + U * (L1 + 2.65f), U, .44f, .6f, 10);
            var hull = ax - up * .75f; mk.Box(paperCream, hull, new Vector3(1.1f, .5f, 4.2f), Quaternion.LookRotation(U)); mk.Box(PaperM(PaperTints[3]), hull + up * .28f, new Vector3(1.16f, .08f, 4.26f), Quaternion.LookRotation(U));
            foreach (var (z, h) in new[] { (-1.2f, 2f), (.6f, 2.3f) }) { mk.Prism(woodM, hull + U * z, up, .04f, h, 5); Card(mk, paperEdge, paperEdge, hull + U * z + up * .4f + R * .02f, U, R, new[] { new Vector2(-.6f, 0f), new Vector2(.6f, 0f), new Vector2(.45f, h * .7f), new Vector2(-.45f, h * .7f) }, .03f); }
            mk.Tri(PaperM(PaperTints[3]), hull + U * .6f + up * 2.3f, hull + U * .6f + up * 2f, hull + U * 1.2f + up * 2.15f, hull + R);
            solid.Box(paperCream, ax + U * ((L0 + L1 + 2.6f) / 2f), new Vector3(Rb * 2.1f, Rb * 2.1f, L1 - L0 + 3.4f), Quaternion.LookRotation(U));
            halos.Add(ax, 3.2f, new Color(.6f, .95f, 1f), .3f); ArtistBits.PointLight(ctx.Root, ax + up * .5f, new Color(.6f, .95f, 1f), 10f, 1.4f); Mark(o, 6f);
            // rock pools of glass, edged with paper pebbles; a paper starfish in one
            var pools = new List<Vector3>();
            foreach (var (u, r, rad) in new[] { (-6f, -17f, 1.6f), (6f, -19f, 1.3f), (14f, -15f, 1.8f) })
            {
                var p = c + U * u + R * r; if (ctx.Ground(p.x, p.z) < waterY + .2f || InFootprint(ctx, p.x, p.z, 2f)) continue; p.y = ctx.Ground(p.x, p.z);
                mk.Prism(glassDeep, p - up * .02f, up, rad, .07f, 14); for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; mk.Blob(k % 3 == 0 ? PaperM(PaperTints[1]) : paperEdge, p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (rad + .15f) + up * .08f, new Vector3(.24f, .14f, .2f), rng, .2f); }
                pools.Add(p); halos.Add(p + up * .3f, rad + .6f, new Color(.4f, .85f, 1f), .3f);
            }
            if (pools.Count > 0)
            {
                var sf = pools[0] + up * .1f; var coral = PaperM(PaperTints[3]);
                for (var k = 0; k < 5; k++) { var a = k * 72f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var pp = Vector3.Cross(Vector3.up, d); mk.Tri(coral, sf + d * .45f, sf + pp * .14f, sf - pp * .14f, sf - up); }
                // a message in a bottle, washed up by the last pool
                var bm = new MeshKit(); bm.Prism(glassWater, Vector3.zero, Vector3.forward, .07f, .26f, 8); bm.Prism(glassWater, new Vector3(0, 0, .26f), Vector3.forward, .03f, .08f, 6); bm.Prism(woodM, new Vector3(0, 0, .34f), Vector3.forward, .028f, .04f, 6); bm.Box(paperEdge, new Vector3(0, 0, .12f), new Vector3(.05f, .05f, .16f), Quaternion.identity);
                var at = pools[pools.Count - 1] + R * 2.2f + up * .08f; Thing(bm, "Message in a bottle", "a message in a bottle (it says: wish you were here)", at, Quaternion.Euler(0, 40f, 0), new Vector3(.16f, .16f, .4f), .3f, false);
            }
        }
    }
}
