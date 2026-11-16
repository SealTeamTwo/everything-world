using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The record's label as a whole canal town (part of FusionKit, 2026-11-16): besides the first canal (with the Piano
    /// Bridge, the Rooftop and the café), three more canals run east-west across the label, each between two quays and
    /// two rows of the narrow brick houses, with lamps, strings of lights over the water, plank bridges and a houseboat.
    /// One straight avenue runs north-south through the spire's square, crossing every canal on a stone bridge; the
    /// streets between the canals lead out to the record's spokes.
    public partial class FusionKit
    {
        /// The other canals: how far north of the square (negative = south), and their half-length.
        static readonly (float off, float L)[] ExtraCanals = { (-44f, 116f), (104f, 72f), (-104f, 72f) };
        const float AvenueW = 5f;

        /// Cut one canal into the levelled ground: the quays at the quay height, the channel 2.8 m below.
        void CutCanal(IslandWorldData data, int index, Vector2 c2, float L)
        {
            var half = CanalHalf + QuayW + HouseD + 4f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var rel = new Vector2(x, z) - c2; var al = Mathf.Abs(Vector2.Dot(rel, A2)); var pe = Mathf.Abs(Vector2.Dot(rel, N2));
                    if (al > L + 34f || pe > half + 16f) continue;
                    if (al < L && pe < CanalHalf + 1.2f) data.heights[i] = Mathf.Lerp(quayY - 2.8f, data.heights[i], Ease(CanalHalf - .2f, CanalHalf + 1.2f, pe));
                }
            }
        }

        bool InExtraTown(float x, float z, float pad)
        {
            foreach (var (off, L) in ExtraCanals)
            {
                var rel = new Vector2(x, z) - (heart2 + N2 * off);
                if (Mathf.Abs(Vector2.Dot(rel, A2)) < L + 14f + pad && Mathf.Abs(Vector2.Dot(rel, N2)) < CanalHalf + QuayW + HouseD + 3f + pad) return true;
            }
            return false;
        }

        void LabelTown(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var n = 100; foreach (var (off, L) in ExtraCanals) { ExtraCanal(ctx, mk, solid, off, L, n); n += 40; }
            Avenue(ctx, mk);
        }

        void ExtraCanal(LandContext ctx, MeshKit mk, MeshKit solid, float off, float L, int seed)
        {
            var up = Vector3.up; var cc = new Vector3(center.x, quayY, center.z) + Nrm * off; var along = Quaternion.LookRotation(A); var across = Quaternion.LookRotation(Nrm);
            Vector3 Q(float al, float pe, float y = 0f) => cc + A * al + Nrm * pe + up * y;
            float[] bridges = { 0f, -L * .62f, L * .62f }; float[] widths = { AvenueW + .4f, 3f, 3f };
            bool AtBridge(float al, float pad) { for (var k = 0; k < bridges.Length; k++) if (Mathf.Abs(al - bridges[k]) < widths[k] / 2f + pad) return true; return false; }
            mk.Box(waterM, Q(0, 0, -1.25f), new Vector3(CanalHalf * 2f + .4f, .1f, L * 2f), along);
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(brickDark, Q(0, e * (CanalHalf + .3f), -1.4f), new Vector3(.6f, 2.9f, L * 2f + 1f), along); mk.Box(stoneM, Q(0, e * (CanalHalf + .3f), .09f), new Vector3(.8f, .18f, L * 2f + 1f), along);
                mk.Box(paving, Q(0, e * (CanalHalf + .6f + QuayW / 2f), .06f), new Vector3(QuayW, .14f, L * 2f + 14f), along);
                for (var al = -L - 4f; al <= L + 4f; al += 3f) Remember(Q(al, e * (CanalHalf + 3.6f), .1f), 2);
                mk.Box(brickDark, Q(e * L, 0, -1.4f), new Vector3(CanalHalf * 2f + 1.2f, 2.9f, .7f), along); mk.Box(stoneM, Q(e * L, 0, .09f), new Vector3(CanalHalf * 2f + 1.6f, .18f, .9f), along);
                Col(Q(e * (L + .3f), 0, .6f), new Vector3(CanalHalf * 2f + 1.6f, 1.2f, .12f), along); mk.Box(iron, Q(e * (L + .3f), 0, 1.08f), new Vector3(CanalHalf * 2f + 1.4f, .06f, .07f), along);
                // railings, open where the bridges land
                var start = -L - .4f;
                for (var al = -L - .4f; al <= L + .41f; al += .8f)
                {
                    var gap = AtBridge(al, .5f); var last = al + .8f > L + .41f;
                    if (!gap) mk.Prism(iron, Q(al, e * (CanalHalf + .62f), .1f), up, .03f, 1f, 4);
                    if (gap || last) { var end = gap ? al - .8f : al; if (end - start > .5f) { var mid = (start + end) / 2f; mk.Box(iron, Q(mid, e * (CanalHalf + .62f), 1.08f), new Vector3(.07f, .06f, end - start), along); mk.Box(iron, Q(mid, e * (CanalHalf + .62f), .6f), new Vector3(.04f, .04f, end - start), along); Col(Q(mid, e * (CanalHalf + .62f), .6f), new Vector3(.12f, 1.2f, end - start + .3f), along); } start = al + .8f; }
                }
            }
            // lamps on both quays, lights strung across between them
            var k2 = 0; for (var al = -L + 10f; al <= L - 10f; al += 18f, k2++)
            {
                if (AtBridge(al, 3f)) continue;
                foreach (var e in new[] { -1f, 1f }) BrassLamp(ctx, mk, Q(al, e * (CanalHalf + 1.5f), .12f), Nrm * e, k2 % 2 == 0);
                BulbString(mk, Q(al, -(CanalHalf + 1.5f), 3.85f), Q(al, CanalHalf + 1.5f, 3.85f), k2 + seed);
            }
            // the avenue's stone bridge, and two plank bridges
            var span = CanalHalf * 2f + 2.4f; mk.Box(stoneM, Q(0, 0, -.05f), new Vector3(widths[0], .4f, span), across); solid.Box(stoneM, Q(0, 0, -.05f), new Vector3(widths[0], .4f, span), across);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(stoneM, Q(e * (widths[0] / 2f - .15f), 0, .55f), new Vector3(.3f, 1f, span), across); mk.Box(brassM, Q(e * (widths[0] / 2f - .15f), 0, 1.08f), new Vector3(.34f, .06f, span), across); Col(Q(e * (widths[0] / 2f - .15f), 0, .6f), new Vector3(.3f, 1.2f, span), across); }
            for (var k = 1; k < 3; k++)
            {
                var al = bridges[k]; mk.Box(woodM, Q(al, 0, .02f), new Vector3(widths[k], .24f, span), across); solid.Box(woodM, Q(al, 0, .02f), new Vector3(widths[k], .24f, span), across);
                foreach (var e in new[] { -1f, 1f }) { mk.Box(iron, Q(al + e * (widths[k] / 2f - .06f), 0, 1.08f), new Vector3(.07f, .06f, span), across); for (var s = -span / 2f; s <= span / 2f + .01f; s += span / 8f) mk.Prism(iron, Q(al + e * (widths[k] / 2f - .06f), s, .1f), up, .03f, 1f, 4); Col(Q(al + e * (widths[k] / 2f - .06f), 0, .65f), new Vector3(.12f, 1.2f, span), across); }
                for (var pe = -span / 2f - 2f; pe <= span / 2f + 2f; pe += 2f) Remember(Q(al, pe, .15f), 2);
            }
            // the houses, both quays, a gap for the avenue
            var back = CanalHalf + .6f + QuayW + HouseD / 2f; var i = seed;
            foreach (var e in new[] { 1f, -1f })
                for (var al = -L + 8f; al <= L - 7f; al += 12f, i++)
                {
                    if (Mathf.Abs(al) < AvenueW / 2f + 6.3f) continue;
                    RowHouse(ctx, mk, solid, new PlaceFrame(Q(al, e * back), -Nrm * e), i, false);
                }
            // a houseboat moored against the north quay
            var hb = -L * .3f; var o = Q(hb, CanalHalf - 2f, 0); var teal = WorldMaterials.Surface(Paints[(seed / 40) % Paints.Length], Surface.Painted);
            mk.Box(teal, o - up * .8f, new Vector3(3.2f, 1.1f, 11f), along); solid.Box(teal, o - up * .8f, new Vector3(3.2f, 1.1f, 11f), along); mk.Box(brassM, o - up * .28f, new Vector3(3.3f, .06f, 11.1f), along);
            mk.Box(WorldMaterials.Surface(Cream, Surface.Painted), o + up * .5f - A * 1.2f, new Vector3(2.6f, 1.5f, 5f), along); mk.Box(woodM, o + up * 1.3f - A * 1.2f, new Vector3(2.8f, .1f, 5.2f), along); solid.Box(teal, o + up * .5f - A * 1.2f, new Vector3(2.6f, 1.5f, 5f), along);
            for (var k = 0; k < 3; k++) mk.Box(windowM, o + up * .6f - A * (2.8f - k * 1.5f) - Nrm * 1.31f, new Vector3(.04f, .6f, .8f), along);
            ArtistBits.PointLight(ctx.Root, o + up * 2.2f, Brass, 10f, 1.4f);
            Shot(ctx, $"the canal {off:0} m", Q(-L * .5f, -(CanalHalf + 4f), 2.2f), Q(L * .3f, 3f, 3f));
        }

        /// The avenue: north-south through the square, over every canal, from one edge of the label to the other.
        void Avenue(LandContext ctx, MeshKit mk)
        {
            var spans = new List<(float, float)>();
            // where the avenue is already laid (Brass Lane and the Piano Bridge, between the square and the first canal's
            // north houses) it isn't laid again; elsewhere it stops at each canal's water for the bridge
            var cuts = new List<(float, float)> { (-18.5f, CanalOff + CanalHalf + QuayW + HouseD + 2f) };
            foreach (var (off, _) in ExtraCanals) cuts.Add((off - CanalHalf - .6f, off + CanalHalf + .6f));
            cuts.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            var at = -LabelR + 1f; foreach (var (a, b) in cuts) { if (a > at + 1f) spans.Add((at, a)); at = Mathf.Max(at, b); } if (at < LabelR - 1f) spans.Add((at, LabelR - 1f));
            var n = 0;
            foreach (var (z0, z1) in spans)
            {
                var pts = new List<Vector3>(); for (var z = z0; z <= z1; z += 2f) pts.Add(ctx.OnGround(center.x + Nrm.x * z, center.z + Nrm.z * z)); pts.Add(ctx.OnGround(center.x + Nrm.x * z1, center.z + Nrm.z * z1));
                Strip(mk, pts, paving, AvenueW, .15f, ctx); foreach (var p in pts) Remember(p, 1);
                for (var z = z0 + 6f; z < z1 - 3f; z += 14f, n++) { if (InTown(center.x + Nrm.x * z, center.z + Nrm.z * z, -2f) || InExtraTown(center.x + Nrm.x * z, center.z + Nrm.z * z, -2f)) continue; var e = n % 2 == 0 ? 1f : -1f; BrassLamp(ctx, mk, ctx.OnGround(center.x + A.x * e * 3.6f + Nrm.x * z, center.z + A.z * e * 3.6f + Nrm.z * z), -A * e, true); }
            }
        }
    }
}
