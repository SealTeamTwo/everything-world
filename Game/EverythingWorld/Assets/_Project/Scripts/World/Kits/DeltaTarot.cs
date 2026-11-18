using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Delta Crossroads as a tarot deck (part of DeltaKit):
    /// - every artist is one of the twenty-two great cards: their place is seen through a tall gold frame with the
    ///   card's number and name over the way in and its sign standing on top (CardGate);
    /// - the cards left over are places: the Tower, the Hanged Man, the Fool and the World stand on the long way
    ///   round; Death is the graveyard, Judgement its bell, Temperance the landing;
    /// - the four roads are the four suits, marked along their length (Suits);
    /// - each road's end bends away and comes back as the next road's end (LongWayRound), so every way out of the
    ///   crossroads brings you back to it, from another side, and the corners have changed while you were gone
    ///   (Faces). One thing never changes: the crooked lamp;
    /// - a card reader sits on the north-west corner under a great card that turns with every song (Reader,
    ///   DeltaCards); DeltaLife makes the night do what the card says.
    public partial class DeltaKit
    {
        static readonly string[] Roman = { "0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX", "XXI" };
        static readonly string[] CardNames = { "THE FOOL", "THE MAGICIAN", "THE HIGH PRIESTESS", "THE EMPRESS", "THE EMPEROR", "THE HIEROPHANT", "THE LOVERS", "THE CHARIOT", "STRENGTH", "THE HERMIT", "WHEEL OF FORTUNE", "JUSTICE", "THE HANGED MAN", "DEATH", "TEMPERANCE", "THE DEVIL", "THE TOWER", "THE STAR", "THE MOON", "THE SUN", "JUDGEMENT", "THE WORLD" };
        const int Cups = 100, Swords = 101, Wands = 102, Coins = 103, Nimbus = 104;

        /// Which great card an artist is.
        static int CardOf(string artist)
        {
            switch (artist)
            {
                case "Robert Johnson": return 15; case "Howlin' Wolf": return 18; case "Muddy Waters": return 17; case "B.B. King": return 4; case "Etta James": return 3;
                case "John Lee Hooker": return 1; case "Albert King": return 10; case "Sister Rosetta Tharpe": return 2; case "Lead Belly": return 11; case "Big Mama Thornton": return 8;
                case "Fats Domino": return 6; case "Gary Clark Jr.": return 19; case "Stevie Ray Vaughan": return 7; case "R.L. Burnside": return 9; case "Junior Kimbrough": return 5;
                default: return new[] { 14, 20, 13, 12, 16, 0, 21 }[Mathf.Abs(artist.GetHashCode()) % 7];
            }
        }

        /// The card that is turned when one of an artist's songs comes on (their n-th song): one of the eight the night answers to.
        static int TurnedFor(string artist, int n)
        {
            switch (artist)
            {
                case "Robert Johnson": return 15; case "Howlin' Wolf": case "Etta James": return 18; case "Muddy Waters": case "Stevie Ray Vaughan": case "Junior Kimbrough": return 17;
                case "Gary Clark Jr.": case "Fats Domino": case "Big Mama Thornton": return 19; case "Lead Belly": return 13; case "B.B. King": return 9;
                case "Sister Rosetta Tharpe": case "Albert King": return 16; case "John Lee Hooker": return 10; case "R.L. Burnside": return n % 2 == 0 ? 9 : 10;
                default: return DeltaCards.Deck[Mathf.Abs(artist.GetHashCode() + n) % DeltaCards.Deck.Length];
            }
        }

        /// A card's sign in line: 'c' its middle, 'right' and 'upv' its plane, 's' half its height, 'th' the line's weight.
        static void Emblem(MeshKit mk, Material m, int card, Vector3 c, Vector3 right, Vector3 upv, float s, float th)
        {
            Vector3 P(float x, float y) => c + right * (x * s) + upv * (y * s);
            void L(params float[] v) { for (var i = 0; i + 3 < v.Length; i += 2) { Vector3 a = P(v[i], v[i + 1]), b = P(v[i + 2], v[i + 3]); var d = b - a; if (d.sqrMagnitude > 1e-6f) mk.Prism(m, a - d.normalized * th * .5f, d, th, d.magnitude + th, 4); } }
            void Arc(float cx, float cy, float r, float a0, float a1, int n = 14, float ry = -1f) { if (ry < 0f) ry = r; var v = new float[(n + 1) * 2]; for (var i = 0; i <= n; i++) { var a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; v[i * 2] = cx + Mathf.Cos(a) * r; v[i * 2 + 1] = cy + Mathf.Sin(a) * ry; } L(v); }
            void Ring(float cx, float cy, float r, int n = 16) => Arc(cx, cy, r, 0f, 360f, n);
            void Star(float cx, float cy, float r, int pts, float inner = .42f, float turn = 90f) { var v = new float[(pts * 2 + 1) * 2]; for (var i = 0; i <= pts * 2; i++) { var a = (turn + i * 180f / pts) * Mathf.Deg2Rad; var rr = i % 2 == 0 ? r : r * inner; v[i * 2] = cx + Mathf.Cos(a) * rr; v[i * 2 + 1] = cy + Mathf.Sin(a) * rr; } L(v); }
            void Rays(float cx, float cy, float r0, float r1, int n, float turn = 0f) { for (var i = 0; i < n; i++) { var a = (turn + i * 360f / n) * Mathf.Deg2Rad; L(cx + Mathf.Cos(a) * r0, cy + Mathf.Sin(a) * r0, cx + Mathf.Cos(a) * r1, cy + Mathf.Sin(a) * r1); } }
            void Eight(float cx, float cy, float w) { const int n = 20; var v = new float[(n + 1) * 2]; for (var i = 0; i <= n; i++) { var t = i / (float)n * Mathf.PI * 2f; v[i * 2] = cx + w * Mathf.Cos(t); v[i * 2 + 1] = cy + w * .45f * Mathf.Sin(2f * t); } L(v); }
            switch (card)
            {
                case 0: L(-.7f, -.9f, .5f, .5f); Ring(.55f, .62f, .24f, 10); Ring(-.5f, .5f, .2f, 10); Rays(-.5f, .5f, .28f, .42f, 8); break;                                  // the Fool: a bundle on a stick, a small sun
                case 1: Eight(0, .6f, .55f); L(0, -.9f, 0, .25f); L(-.14f, .25f, .14f, .25f); L(-.14f, -.9f, .14f, -.9f); break;                                              // the Magician: a wand under the sign of forever
                case 2: L(-.65f, -.9f, -.65f, .5f); L(.65f, -.9f, .65f, .5f); L(-.82f, .5f, -.48f, .5f); L(.48f, .5f, .82f, .5f); Arc(0, 0, .38f, 60f, 300f); Arc(.17f, 0, .3f, 75f, 285f); break;   // the High Priestess: two pillars, a crescent
                case 3: Ring(0, .35f, .4f); L(0, -.05f, 0, -.9f); L(-.35f, -.5f, .35f, -.5f); break;                                                                          // the Empress
                case 4: L(-.7f, -.4f, -.7f, .5f, -.35f, .05f, 0, .7f, .35f, .05f, .7f, .5f, .7f, -.4f, -.7f, -.4f); L(-.7f, -.62f, .7f, -.62f); break;                      // the Emperor: a crown
                case 5: L(0, -.9f, 0, .9f); L(-.25f, .6f, .25f, .6f); L(-.42f, .3f, .42f, .3f); L(-.6f, 0, .6f, 0); break;                                                    // the Hierophant: the triple cross
                case 6: Ring(-.28f, -.1f, .45f); Ring(.28f, -.1f, .45f); Star(0, .75f, .17f, 4, .4f); break;                                                                  // the Lovers
                case 7: Ring(-.45f, -.55f, .3f, 10); Ring(.45f, -.55f, .3f, 10); L(-.75f, -.25f, .75f, -.25f, .75f, .35f, -.75f, .35f, -.75f, -.25f); Star(0, .68f, .2f, 8); break;   // the Chariot
                case 8: Eight(0, .7f, .5f); Ring(0, -.2f, .4f); Rays(0, -.2f, .48f, .72f, 10); break;                                                                         // Strength: the lion
                case 9: L(-.3f, -.6f, .3f, -.6f, .42f, .2f, 0, .6f, -.42f, .2f, -.3f, -.6f); Star(0, -.15f, .26f, 6); Arc(0, .6f, .2f, 0f, 180f, 8); break;                   // the Hermit: a lantern
                case 10: Ring(0, 0, .85f, 20); Ring(0, 0, .25f, 10); Rays(0, 0, .25f, .85f, 8); break;                                                                        // the Wheel of Fortune
                case 11: L(0, -.9f, 0, .75f); L(-.7f, .55f, .7f, .55f); L(-.7f, .55f, -.95f, -.1f, -.45f, -.1f, -.7f, .55f); L(.7f, .55f, .45f, -.1f, .95f, -.1f, .7f, .55f); L(-.3f, -.9f, .3f, -.9f); break;   // Justice: scales
                case 12: L(-.7f, .85f, .7f, .85f); L(0, .85f, 0, -.3f); L(0, .3f, .3f, .05f, .05f, -.05f); Ring(0, -.52f, .22f, 10); Rays(0, -.52f, .28f, .42f, 8); break;    // the Hanged Man
                case 13: L(-.35f, -.9f, .25f, .8f); L(.25f, .8f, -.2f, .75f, -.55f, .5f, -.75f, .15f); L(-.75f, .15f, -.4f, .45f, .2f, .6f); break;                           // Death: a scythe
                case 14: L(-.8f, .7f, -.3f, .7f, -.45f, .25f, -.65f, .25f, -.8f, .7f); L(.3f, -.3f, .8f, -.3f, .65f, -.75f, .45f, -.75f, .3f, -.3f); L(-.45f, .45f, -.1f, .2f, .2f, .1f, .5f, -.3f); break;   // Temperance: one cup poured into another
                case 15: Star(0, -.05f, .8f, 5, .382f, -90f); L(-.55f, .95f, -.35f, .6f); L(.55f, .95f, .35f, .6f); break;                                                    // the Devil: the star upside down, horns
                case 16: L(-.35f, -.9f, -.35f, .4f, -.5f, .4f, -.5f, .65f, -.2f, .65f, -.2f, .5f, .2f, .5f, .2f, .65f, .5f, .65f, .5f, .4f, .35f, .4f, .35f, -.9f); L(.95f, .98f, .45f, .72f, .7f, .62f, .3f, .3f); break;   // the Tower, and what strikes it
                case 17: Star(0, .12f, .75f, 8); Star(-.7f, -.72f, .16f, 4, .4f); Star(.7f, -.72f, .16f, 4, .4f); Star(0, -.86f, .13f, 4, .4f); break;                        // the Star
                case 18: Arc(0, .15f, .6f, 50f, 310f); Arc(.22f, .15f, .5f, 65f, 295f); L(-.5f, -.6f, -.5f, -.8f); L(0, -.7f, 0, -.9f); L(.5f, -.6f, .5f, -.8f); break;       // the Moon
                case 19: Ring(0, 0, .42f); Rays(0, 0, .52f, .88f, 12); break;                                                                                                 // the Sun
                case 20: L(-.8f, -.5f, .5f, .3f); L(.5f, .05f, .5f, .55f, .8f, .7f, .8f, -.1f, .5f, .05f); L(-.2f, -.05f, -.2f, -.6f, .25f, -.6f, .25f, .2f); break;          // Judgement: a trumpet and its flag
                case 21: Arc(0, 0, .6f, 0f, 360f, 18, .88f); Star(-.75f, .82f, .13f, 4, .4f); Star(.75f, .82f, .13f, 4, .4f); Star(-.75f, -.82f, .13f, 4, .4f); Star(.75f, -.82f, .13f, 4, .4f); break;   // the World: a wreath
                case Cups: Arc(0, .35f, .5f, 180f, 360f, 10); L(-.5f, .35f, .5f, .35f); L(0, -.15f, 0, -.7f); L(-.35f, -.7f, .35f, -.7f); break;
                case Swords: L(0, -.5f, 0, .95f); L(-.35f, -.5f, .35f, -.5f); L(0, -.5f, 0, -.9f); break;
                case Wands: L(0, -.95f, 0, .9f); L(0, .5f, .3f, .75f); L(0, .2f, -.3f, .45f); L(0, -.1f, .3f, .15f); break;
                case Coins: Ring(0, 0, .8f, 16); Star(0, 0, .5f, 5, .382f); break;
                case Nimbus: Ring(0, 0, .6f, 14); Rays(0, 0, .72f, 1f, 12); break;
            }
        }

        /// A card standing up: a deep blue face in a gold border, its number above, its sign in the middle, its name
        /// below. 'c' is the card's middle, 'h' its height. The back is a lattice.
        void CardFace(MeshKit m, Transform labels, Vector3 c, Quaternion rot, float h, int card)
        {
            var w = h * .58f; Vector3 rt = rot * Vector3.right, upv = rot * Vector3.up, fw = rot * Vector3.forward; var th = Mathf.Max(.018f, h * .008f);
            m.Box(cardM, c, new Vector3(w, h, .06f), rot);
            void Border(Vector3 o, float inset)
            {
                float x = w / 2f - inset, y = h / 2f - inset;
                foreach (var (a, b) in new[] { (new Vector2(-x, -y), new Vector2(x, -y)), (new Vector2(x, -y), new Vector2(x, y)), (new Vector2(x, y), new Vector2(-x, y)), (new Vector2(-x, y), new Vector2(-x, -y)) })
                { var p = o + rt * a.x + upv * a.y; var q = o + rt * b.x + upv * b.y; m.Prism(goldM, p, q - p, th, (q - p).magnitude, 4); }
            }
            var f = c + fw * .04f; var bk = c - fw * .04f;
            Border(f, 0f); Border(f, h * .045f); Border(bk, 0f); Border(bk, h * .045f);
            Emblem(m, goldM, card, f - upv * h * .02f, rt, upv, h * .24f, th * 1.3f);
            // the back: a lattice of diagonals
            for (var k = -2; k <= 2; k++) foreach (var sg in new[] { -1f, 1f })
            {
                var o = bk + upv * (k * h * .17f); Vector3 p = o - rt * (w * .4f) - upv * (sg * w * .4f), q = o + rt * (w * .4f) + upv * (sg * w * .4f);
                p = bk + Clamp(p - bk, rt, upv, w * .42f, h * .44f); q = bk + Clamp(q - bk, rt, upv, w * .42f, h * .44f); if ((q - p).sqrMagnitude > .01f) m.Prism(goldM, p, q - p, th * .7f, (q - p).magnitude, 3);
            }
            if (card < 22)
            {
                ArtistBits.Label(labels, Roman[card], c + upv * h * .39f + fw * .035f, fw, h * .075f, Gold, TextAnchor.MiddleCenter);
                ArtistBits.Label(labels, CardNames[card], c - upv * h * .4f + fw * .035f, fw, h * (CardNames[card].Length > 12 ? .042f : .055f), Gold, TextAnchor.MiddleCenter);
            }
        }
        static Vector3 Clamp(Vector3 v, Vector3 rt, Vector3 upv, float mx, float my) => rt * Mathf.Clamp(Vector3.Dot(v, rt), -mx, mx) + upv * Mathf.Clamp(Vector3.Dot(v, upv), -my, my);

        /// A card on a post beside a place: 'g' on the ground, read from the side 'face' points to.
        void CardSign(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int card, float h = 2.6f)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face);
            mk.Box(plankDark, g + up * .6f, new Vector3(.14f, 1.4f, .14f), rot); CardFace(mk, rootT, g + up * (1.2f + h / 2f), rot, h, card);
            ArtistBits.PostCollider(ctx.Root, g, 1.2f + h, .3f); halos.Add(g + up * (1.2f + h / 2f) + face * .3f, h * .5f, Gold, .22f); fixtures.Add(new Vector3(g.x, .8f, g.z)); Mark(g, 1.2f);
        }

        /// The tall gold frame an artist's place is seen through, across the way in: two posts, the card's number and
        /// name on the beam over your head (read from both sides), the card's sign standing on top.
        void CardGate(Plot z, int card)
        {
            var up = Vector3.up; var reach = Foot(z.a); var w = Mathf.Clamp(reach * .8f, 7f, 10f); var h = w * 1.05f; var frame = WorldMaterials.Surface(new Color(.14f, .15f, .36f), Surface.Painted);
            var c = z.c + z.front * (reach + 1.4f); var topY = z.c.y + h;
            foreach (var e in new[] { -1f, 1f })
            {
                var p = c + z.side * e * (w / 2f); var gy = z.ctx.Ground(p.x, p.z) - .3f;
                z.mk.Box(frame, new Vector3(p.x, (gy + topY) / 2f, p.z), new Vector3(.55f, topY - gy, .55f), z.Rot); z.mk.Box(frame, new Vector3(p.x, gy + .75f, p.z), new Vector3(.9f, .9f, .9f), z.Rot);
                ArtistBits.PostCollider(z.root, new Vector3(p.x, gy + .3f, p.z), topY - gy, .38f);
            }
            var beam = new Vector3(c.x, topY + 1.2f, c.z); z.mk.Box(frame, beam, new Vector3(w + 1.3f, 2.4f, .55f), z.Rot); z.mk.Box(frame, beam + up * 1.3f, new Vector3(w + 1.9f, .22f, .8f), z.Rot);
            foreach (var e in new[] { -1f, 1f })
            {
                var fd = z.front * e;
                ArtistBits.Label(z.labels, Roman[card], beam + up * .62f + fd * .29f, fd, .62f, Gold, TextAnchor.MiddleCenter);
                ArtistBits.Label(z.labels, CardNames[card], beam - up * .45f + fd * .29f, fd, CardNames[card].Length > 12 ? .5f : .68f, Gold, TextAnchor.MiddleCenter);
            }
            Emblem(z.mk, goldM, card, beam + up * 3.2f, z.side, up, 1.55f, .07f); halos.Add(beam + up * 3.2f, 3.4f, Gold, .3f);
            noGrass.Add(new Vector3(c.x, 1.2f, c.z));
        }

        // ---------------------------------------------------------------- the one thing that never changes
        void CrookedLamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var p = g - up * .3f; var dirs = new[] { (up + R * .22f).normalized, (up - R * .34f - U * .12f).normalized, (up + R * .2f + U * .3f).normalized, (up - U * .2f).normalized }; var lens = new[] { 1.7f, 1.3f, 1.2f, .9f };
            for (var k = 0; k < 4; k++) { mk.Prism(iron, p, dirs[k], .09f - k * .012f, lens[k] + .05f, 5, .9f); p += dirs[k] * lens[k]; }
            var arm = (U - R).normalized; mk.Prism(iron, p, (arm * -1f + up * .2f).normalized, .03f, 1f, 4); var lp = p + (arm * -1f + up * .2f).normalized * 1f - up * .3f;
            mk.Cone(tin, lp + up * .12f, .34f, .2f, 6); mk.Blob(lampM, lp - up * .06f, new Vector3(.13f, .17f, .13f), new System.Random(4), 0f); halos.Add(lp, 2.2f, Bulb, .7f);
            ArtistBits.PointLight(ctx.Root, lp - up * .4f, Bulb, 20f, 2.4f); ArtistBits.PostCollider(ctx.Root, g, 4.6f, .16f); fixtures.Add(new Vector3(g.x, .4f, g.z));
            string[] lamp = { "Bent like that since anybody can remember. Everything else here moves about. Not this.", "The post is warm. The light doesn't flicker, even when the others do.", "If you ever come back and this lamp is straight, keep walking." }; var ln = 0;
            Spot("The crooked lamp", g + up * 1.3f, "Touch the crooked lamp", new Vector3(.9f, 2.4f, .9f)).onUse = _ => Say(tlife.transform, g + up * 3f, lamp[ln++ % lamp.Length], 5f);
        }

        // ---------------------------------------------------------------- the deal
        /// When the Devil is turned and he is standing at the head of the shadow, you can hear his offer, and shake on
        /// it. Take it and you carry a mark your friends can see (PlayerOrb.DealMade), until the graveyard's bell is rung.
        void Offer(Vector3 at)
        {
            var up = Vector3.up; var asked = -99f;
            var spot = Spot("The one at the crossroads", at + up * 1.4f, "Hear his offer", new Vector3(1.6f, 2.8f, 1.6f));
            spot.onUse = _ =>
            {
                if (EverythingWorld.Player.PlayerOrb.DealMade) { Say(tlife.transform, at + up * 3.4f, "You have shaken already. He touches his hat. They say the bell in the graveyard undoes it, if you want it undone.", 6.5f); return; }
                if (Time.time - asked > 25f) { asked = Time.time; spot.verb = "Shake his hand"; Say(tlife.transform, at + up * 3.4f, "He doesn't speak. You understand anyway: he will tune you, the way he tuned the others, and you will carry his mark. He holds out a hand.", 8f); return; }
                EverythingWorld.Player.PlayerOrb.DealMade = true; life.played = Time.time; spot.verb = "Hear his offer"; asked = -99f;
                Say(tlife.transform, at + up * 3.4f, "His hand is dry and very cold. It is done. Something shows on you now, for anyone to see.", 7f);
            };
            spot.gameObject.SetActive(false); life.offer = spot.gameObject;
        }

        // ---------------------------------------------------------------- the corners, which are not the same twice
        /// Three dressings of the crossroads' corners. Only one is there at a time, and it is swapped while you are
        /// out of sight of it (DeltaCards): you leave by one road, come back down another, and it is not as you left it.
        void Faces(LandContext ctx)
        {
            var up = Vector3.up; var pk = G(ctx, -8.5f, 8.5f); var pr = Quaternion.LookRotation((U + R * .4f).normalized); var pf = pr * Vector3.forward; var ps = pr * Vector3.right; var om = G(ctx, -6.5f, 5.2f);
            Mark(pk, 3f); keepOut.Add(new Vector3(pk.x, 2.8f, pk.z));
            cards = life.gameObject.AddComponent<DeltaCards>(); cards.center = center; cards.johnson = life.johnson; life.cards = cards;
            for (var k = 0; k < 3; k++)
            {
                var go = new GameObject("Crossroads face " + k).transform; go.SetParent(life.transform, false); var m = new MeshKit();
                void Box(Vector3 c, Vector3 s, Quaternion r) => BoxCol(go, c, s, r);
                void CrateAt(Vector3 g, Vector3 face, bool seat) { var rot = Quaternion.LookRotation(face); m.Box(plank, g + up * .23f, new Vector3(.6f, .46f, .6f), rot); m.Box(plankDark, g + up * .23f, new Vector3(.64f, .08f, .64f), rot); Box(g + up * .23f, new Vector3(.6f, .46f, .6f), rot); if (seat) Interact.Seat(go, g + up * .48f, face).seatHeight = .46f; }
                if (k == 0)
                {
                    // a rusting pickup on the south-east corner; the old man on a crate, a spare crate beside him
                    m.Box(rustM, pk + up * .75f, new Vector3(1.7f, .6f, 4.4f), pr); m.Box(rustM, pk + up * 1.35f + pf * .5f, new Vector3(1.6f, .7f, 1.5f), pr); m.Box(WorldMaterials.Surface(new Color(.2f, .22f, .24f), Surface.Glass), pk + up * 1.4f + pf * .5f, new Vector3(1.64f, .4f, 1.3f), pr); m.Box(plankDark, pk + up * 1.15f - pf * 1.3f, new Vector3(1.6f, .3f, 1.7f), pr);
                    foreach (var e in new[] { -1f, 1f }) foreach (var s in new[] { -1.4f, 1.4f }) m.Prism(dark, pk + up * .38f + ps * e * .8f + pf * s, ps * e, .38f, .2f, 10); Box(pk + up * .9f, new Vector3(1.8f, 1.6f, 4.5f), pr);
                    Interact.Seat(go, pk + up * 1.05f - pf * 2.1f, -pf, "Sit on the tailgate").seatHeight = .9f;
                    CrateAt(om, -R, false); CrateAt(om + U * 1.3f + R * .3f, -R, true);
                    var old = Local(); old.hat = Residents.Hat.Straw; old.hair = new Color(.8f, .8f, .78f);
                    Residents.Make(go, om + up * .46f, -R, old, Resident.Pose.Sit, prng, new[] { "Four ways to go and every one of them brings you back. Sit a while instead.", "They say a man can learn to play here, round midnight, if he don't mind who's teaching.", "That shadow on the west road? Been there since before the road. Don't nobody step on it.", "It's always midnight here. Some songs, it gets later than that.", "Things move about when you ain't looking. Truck, wagon, piano. Not that lamp, though. Never that lamp." }, null, .46f);
                }
                else if (k == 1)
                {
                    // a cotton wagon where the truck was; a black dog where the old man sat, his hat left on the crate
                    m.Box(plank, pk + up * .9f, new Vector3(1.7f, .1f, 3.2f), pr); foreach (var e in new[] { -1f, 1f }) { m.Box(plankDark, pk + up * 1.3f + ps * e * .85f, new Vector3(.06f, .8f, 3.2f), pr); foreach (var q in new[] { -1.1f, 1.1f }) m.Prism(plankDark, pk + up * .55f + ps * e * .9f + pf * q, ps * e, .55f, .08f, 10); }
                    m.Blob(cottonM, pk + up * 1.5f, new Vector3(.8f, .5f, 1.5f), prng, .3f); m.Box(plankDark, pk + up * .7f + pf * 2.3f, new Vector3(.08f, .08f, 1.6f), pr * Quaternion.Euler(14f, 0, 0)); Box(pk + up * .9f, new Vector3(1.9f, 1.8f, 3.3f), pr);
                    CrateAt(om, -R, false); m.Prism(WorldMaterials.Surface(new Color(.8f, .72f, .5f), Surface.Cloth), om + up * .47f, up, .3f, .03f, 10); m.Prism(WorldMaterials.Surface(new Color(.8f, .72f, .5f), Surface.Cloth), om + up * .49f, up, .15f, .13f, 10, .8f);
                    var dg = om + U * 1.5f + R * .2f; var dm = new MeshKit(); dm.Blob(shadowM, new Vector3(0, .3f, 0), new Vector3(.2f, .3f, .26f), prng, 0f); dm.Blob(shadowM, new Vector3(0, .66f, .12f), Vector3.one * .16f, prng, 0f); dm.Box(shadowM, new Vector3(0, .62f, .3f), new Vector3(.1f, .1f, .2f), Quaternion.identity); foreach (var e in new[] { -.1f, .1f }) { dm.Cone(shadowM, new Vector3(e, .78f, .1f), .05f, .12f, 4); dm.Blob(goldM, new Vector3(e * .7f, .7f, .26f), Vector3.one * .022f, prng, 0f); }
                    var dgo = dm.BuildSingle("The black dog", go, false); dgo.transform.SetPositionAndRotation(dg, Quaternion.LookRotation(-R)); string[] dog = { "It lets you. Its coat is cold, like a stone in shade.", "The dog looks past you at the west road, and wags once.", "Its eyes are the wrong colour for a dog. It seems to know you've noticed." }; var dn = 0;
                    Interact.Toy(dgo, Toy.Kind.Custom, "Pat the black dog", new Vector3(.8f, 1f, 1f)).onUse = _ => Say(tlife.transform, dg + up * 1.8f, dog[dn++ % dog.Length], 4.5f);
                }
                else
                {
                    // an upright piano on the corner, its stool; a rocking chair across the road, nobody in either
                    var pn = pk; var pq = Quaternion.LookRotation(-pf); m.Box(plankDark, pn + up * .65f, new Vector3(1.5f, 1.3f, .6f), pq); m.Box(white, pn + up * .82f - pf * .36f, new Vector3(1.3f, .05f, .2f), pq); m.Box(plankDark, pn + up * .74f - pf * .34f, new Vector3(1.5f, .1f, .28f), pq); Box(pn + up * .65f, new Vector3(1.5f, 1.3f, .7f), pq);
                    m.Prism(plankDark, pn - pf * .95f, up, .2f, .5f, 8); Interact.Seat(go, pn - pf * .95f + up * .52f, pf, "Sit at the piano").seatHeight = .5f;
                    string[] chord = { "One chord. It hangs over the four roads a long time.", "The keys are cold and exactly in tune, which out here ought to worry you.", "You play three notes of something you don't know. The lamp hums the fourth." }; var cn = 0;
                    var sp = new GameObject("The crossroads piano"); sp.transform.SetParent(go, false); sp.transform.position = pn + up * 1.1f - pf * .4f; Interact.Toy(sp, Toy.Kind.Custom, "Play a chord", new Vector3(1.4f, .8f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, pn + up * 2.6f, chord[cn++ % chord.Length], 4.5f); };
                    var rc = om; var face = -R; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
                    m.Box(plank, rc + up * .42f, new Vector3(.5f, .05f, .5f), rot); m.Box(plank, rc + up * .85f - face * .24f, new Vector3(.5f, .85f, .05f), rot * Quaternion.Euler(-8f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { m.Box(plankDark, rc + up * .04f + side * e * .24f, new Vector3(.05f, .05f, .9f), rot); m.Box(plankDark, rc + up * .22f + side * e * .24f + face * .2f, new Vector3(.04f, .4f, .04f), rot); m.Box(plankDark, rc + up * .6f + side * e * .27f, new Vector3(.04f, .04f, .5f), rot); }
                    Interact.Seat(go, rc + up * .48f, face, "Sit in the rocking chair");
                }
                GoldLine(m, m); var built = m.BuildSingle("Corner", go, false); if (built != null) built.isStatic = false;
                cards.corner[k] = go.gameObject;
            }
        }

        // ---------------------------------------------------------------- the four suits
        void Suits(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var n = 0;
            void Post(Vector3 g, Vector3 face, int suit, float h, float s)
            {
                var rot = Quaternion.LookRotation(face); mk.Box(plankDark, g + up * (h / 2f - .15f), new Vector3(.16f, h + .3f, .16f), rot); mk.Box(plankDark, g + up * h, new Vector3(.5f, .08f, .5f), rot);
                Emblem(mk, goldM, suit, g + up * (h + s + .12f), rot * Vector3.right, up, s, s * .07f); halos.Add(g + up * (h + s), s * 2.4f, Gold, .3f);
                ArtistBits.PostCollider(ctx.Root, g, h, .14f); fixtures.Add(new Vector3(g.x, .3f, g.z)); n++;
            }
            // swords north, cups east, wands south, coins west: on the side of each road the telegraph poles and the lanes are not
            var roads = new[] { (U, northL, -R, Swords), (R, eastL - 24f, -U, Cups), (-U, southL, -R, Wands), (-R, westL, U, Coins) };
            foreach (var (d, len, s, suit) in roads)
            {
                var m0 = center + d * 13.5f + s * (RoadW / 2f + 1.6f); Post(ctx.OnGround(m0.x, m0.z), -s, suit, 4.4f, .8f);
                for (var dist = 40f; dist <= len - 6f; dist += 34f)
                {
                    var p = center + d * dist + s * (RoadW / 2f + 2.2f);
                    if (!ctx.IsLand(p.x, p.z, 3f) || OnRail(p.x, p.z, 2.5f) || InFootprint(ctx, p.x, p.z, 1.5f) || InNoGrass(p.x, p.z, 1f)) continue;
                    Post(ctx.OnGround(p.x, p.z), -s, suit, 2.9f, .5f);
                }
            }
            // the cards that are places already: Death at the graveyard's gate, Judgement by its bell, Temperance on the landing
            var gc = ctx.OnGround(grave2.x, grave2.y); CardSign(ctx, mk, gc + U * 12.6f - R * 4.2f, U, 13); CardSign(ctx, mk, gc + U * 13.4f + R * 9.4f, U, 20, 2.2f);
            CardSign(ctx, mk, G(ctx, 4.6f, eastL - 20f), -R, 14);
            Debug.Log($"The Delta Crossroads: {n} suit posts");
        }

        // ---------------------------------------------------------------- the long way round
        /// Each road's end bends away through a place and comes back as the next road's end.
        void LongWayRound(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var ends = new[] { (U, northL), (-R, westL), (-U, southL), (R, Mathf.Min(eastL - 46f, 200f)) }; var total = 0f;
            for (var q = 0; q < 4; q++)
            {
                var (d0, l0) = ends[q]; var (d1, l1) = ends[(q + 1) % 4]; if (l0 < 50f || l1 < 50f) continue;
                const int N = 150; var rad = new float[N + 1];
                Vector3 Dir(int i) { var a = i / (float)N * 90f * Mathf.Deg2Rad; return d0 * Mathf.Cos(a) + d1 * Mathf.Sin(a); }
                bool Ok(Vector3 p) => ctx.IsLand(p.x, p.z, 12f) && !InFootprint(ctx, p.x, p.z, 6f) && !InNoGrass(p.x, p.z, 4f);
                for (var i = 0; i <= N; i++)
                {
                    var t = i / (float)N; var r = Mathf.Lerp(l0, l1, Mathf.SmoothStep(0f, 1f, t)) * (1f + .07f * Mathf.Sin(t * Mathf.PI * 3f) * Mathf.Sin(t * Mathf.PI));
                    var dir = Dir(i); for (var k = 0; k < 40 && r > 44f && !Ok(center + dir * r); k++) r -= 4f;
                    rad[i] = r;
                }
                rad[0] = l0; rad[N] = l1;
                for (var pass = 0; pass < 8; pass++) { var s = (float[])rad.Clone(); for (var i = 1; i < N; i++) { var v = (s[i - 1] + s[i] * 2f + s[i + 1]) / 4f; if (v <= s[i] || Ok(center + Dir(i) * v)) rad[i] = v; } }
                var pts = new List<Vector3>(); for (var i = 0; i <= N; i++) { var p = center + Dir(i) * rad[i]; pts.Add(ctx.OnGround(p.x, p.z)); }
                Strip(mk, pts, dustM, PathW + .8f, .12f, ctx); foreach (var p in pts) Remember(p, 6 + q);
                for (var i = 1; i <= N; i++)
                {
                    total += Vector3.Distance(pts[i - 1], pts[i]);
                    // planks where it crosses the railroad
                    var z0 = pts[i - 1].z - (center.z + RailOff); var z1 = pts[i].z - (center.z + RailOff);
                    if (z0 * z1 <= 0f && OnRail(pts[i].x, pts[i].z, 2f)) mk.Box(plankDark, new Vector3(pts[i].x, ctx.Ground(pts[i].x, center.z + RailOff) + .3f, center.z + RailOff), new Vector3(PathW + 1.6f, .16f, 2.6f), Quaternion.LookRotation(R));
                }
                // a lamp now and then on the outside of the bend
                for (var i = 14; i < N - 8; i += 22) { if (Mathf.Abs(i - N / 2) < 8) continue; var lp = pts[i] + Dir(i) * (PathW / 2f + 1.5f); if (!ctx.IsLand(lp.x, lp.z, 2f) || OnRail(lp.x, lp.z, 2f) || InFootprint(ctx, lp.x, lp.z, 1f)) continue; var lg = ctx.OnGround(lp.x, lp.z); BulbPole(ctx, mk, lg, -Dir(i), (i / 22) % 2 == 0); fixtures.Add(new Vector3(lg.x, .3f, lg.z)); }
                // the place half way round, on the inside of the bend (or the outside, where the inside has no room)
                var mid = pts[N / 2]; var outw = Dir(N / 2); var spot = mid - outw * 10f; var sgn = -1f;
                if (!Ok(spot)) { spot = mid + outw * 10f; sgn = 1f; if (!ctx.IsLand(spot.x, spot.z, 8f)) { spot = mid - outw * 10f; sgn = -1f; } }
                spot = ctx.OnGround(spot.x, spot.z); var toPath = -outw * sgn; var along = Vector3.Cross(up, outw);
                var sign = mid + outw * sgn * (PathW / 2f + 2f) + along * 5.5f; sign = ctx.OnGround(sign.x, sign.z);
                switch (q)
                {
                    case 0: TheTower(ctx, mk, solid, spot, toPath); CardSign(ctx, mk, sign, toPath, 16); break;
                    case 1: TheHangedMan(ctx, mk, solid, spot, toPath); CardSign(ctx, mk, sign, toPath, 12); break;
                    case 2: TheFool(ctx, mk, solid, spot, toPath, outw); CardSign(ctx, mk, sign, toPath, 0); break;
                    default: TheWorld(ctx, mk, solid, spot, toPath); CardSign(ctx, mk, sign, toPath, 21); break;
                }
                Mark(spot, 6.5f); keepOut.Add(new Vector3(spot.x, 7f, spot.z));
                Shot(ctx, "the long way round, " + new[] { "the Tower", "the Hanged Man", "the Fool", "the World" }[q], ctx.OnGround(pts[N / 2 - 12].x, pts[N / 2 - 12].z) + up * 2.2f - outw * sgn * 3f, spot + up * 4f);
            }
            Debug.Log($"The Delta Crossroads: the long way round is {total:0} m");
        }

        /// XVI: a water tower by the line, struck. Split down the side, its cap knocked off, the water still coming out.
        void TheTower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rng = new System.Random(16); var side = Vector3.Cross(up, face); var tilt = Quaternion.AngleAxis(9f, side);
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = g + new Vector3(x * 2.2f, -.3f, zz * 2.2f); var top = g + new Vector3(x * 1.7f, 9f, zz * 1.7f); mk.Box(plankDark, (foot + top) / 2f, new Vector3(.26f, (top - foot).magnitude, .26f), Quaternion.FromToRotation(up, (top - foot).normalized)); ArtistBits.PostCollider(ctx.Root, foot, 9f, .2f); }
            for (var k = 1; k < 3; k++) mk.Box(plankDark, g + up * k * 3f, new Vector3(4.4f - k * .3f, .14f, 4.4f - k * .3f), Quaternion.identity);
            var tank = g + up * 9f; mk.Prism(plank, tank, tilt * up, 2.9f, 4.4f, 8); for (var k = 0; k < 3; k++) mk.Prism(iron, tank + tilt * up * (.5f + k * 1.6f), tilt * up, 2.95f, .12f, 8);
            // the cap, knocked off and lying on the ground; the split, burning gold; the bolt that did it
            var cap = g + face * 6f + side * 3.4f; cap = ctx.OnGround(cap.x, cap.z); var capM = new MeshKit(); capM.Cone(tin, Vector3.zero, 3.2f, 1.5f, 8); var cgo = capM.BuildSingle("The Tower's cap", ctx.Root, false); cgo.transform.SetPositionAndRotation(cap + up * .9f, Quaternion.Euler(64f, 30f, 0)); BoxCol(ctx.Root, cap + up * .8f, new Vector3(4.6f, 1.6f, 4.6f), Quaternion.identity);
            var crack = tank + tilt * up * 4.4f + face * 2.2f; var prev = crack; for (var k = 1; k <= 6; k++) { var nx = tank + tilt * up * (4.4f - k * .72f) + face * 2.96f + side * ((k % 2 == 0 ? .5f : -.4f) + (float)rng.NextDouble() * .2f); mk.Prism(lampM, prev, nx - prev, .07f, (nx - prev).magnitude + .05f, 4); prev = nx; }
            var bolt = tank + tilt * up * 4.6f; var bp = bolt; for (var k = 1; k <= 9; k++) { var nx = bolt + up * (k * 5.2f) + side * ((k % 2 == 0 ? 2.4f : -2f) + (float)rng.NextDouble() * 1.2f) + face * ((float)rng.NextDouble() * 2f - 1f); mk.Prism(goldM, bp, nx - bp, .1f + k * .02f, (nx - bp).magnitude + .1f, 4); bp = nx; }
            halos.Add(bolt + up * 1f, 6f, Gold, .5f); halos.Add(bolt + up * 22f, 9f, Gold, .25f); ArtistBits.PointLight(ctx.Root, tank + face * 4f + up * 2f, Gold, 26f, 2.2f);
            // the water still pouring from the split into a pool
            var water = new Material(WorldMaterials.Glow(new Color(.35f, .55f, 1f), .9f)) { name = "Delta tower water" }; for (var k = 0; k < 3; k++) { var from = prev + side * (k * .22f - .22f); mk.Prism(water, from, -up, .06f, from.y - g.y, 4); }
            var pool = g + face * 3.1f; mk.Prism(water, pool - up * .05f, up, 2.6f, .1f, 12); halos.Add(pool + up * .3f, 3f, Blues, .3f);
            string[] say = { "It was struck once and has been being struck ever since. You can smell the lightning.", "The water runs out and the tank never empties. Nobody drinks it.", "Whatever was at the top of this isn't at the top of anything now." }; var sn = 0;
            Spot("The Tower", g + face * 4.5f + up * 1.2f, "Stand under the Tower", new Vector3(2.4f, 2.4f, 2.4f)).onUse = _ => Say(tlife.transform, g + face * 4.5f + up * 3f, say[sn++ % say.Length], 5f);
        }

        /// XII: a great dead tree, and a scarecrow hung from it by one foot, quite calm, a ring of light round its head.
        void TheHangedMan(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rng = new System.Random(12); var side = Vector3.Cross(up, face); DeadTree(ctx, mk, g, 2.7f, rng);
            var bough = g + up * 6.2f; mk.Box(trunkM, bough + face * 2.2f, new Vector3(.3f, .3f, 4.6f), Quaternion.LookRotation(face)); var hang = bough + face * 4f;
            mk.Prism(white, hang, -up, .025f, 1.5f, 4);
            // upside down: one leg tied, the other bent across it, arms behind, the head lowest
            var rot = Quaternion.LookRotation(face); var cloth = WorldMaterials.Surface(new Color(.22f, .24f, .46f), Surface.Cloth); var sack = WorldMaterials.Surface(new Color(.76f, .7f, .56f), Surface.Cloth); var top = hang - up * 1.5f;
            mk.Box(cloth, top - up * .55f, new Vector3(.16f, 1.1f, .16f), rot); mk.Box(cloth, top - up * .75f + side * .22f, new Vector3(.5f, .14f, .14f), rot * Quaternion.Euler(0, 0, 28f)); mk.Box(cloth, top - up * 1.6f, new Vector3(.6f, 1f, .26f), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(cloth, top - up * 1.6f + side * e * .34f - face * .1f, new Vector3(.12f, .7f, .12f), rot * Quaternion.Euler(0, 0, e * 24f));
            var head = top - up * 2.36f; mk.Blob(sack, head, Vector3.one * .2f, rng, .08f); Emblem(mk, goldM, Nimbus, head, side, up, .52f, .022f); halos.Add(head, 1.5f, Gold, .5f); ArtistBits.PointLight(ctx.Root, head + face * 1.2f, Gold, 14f, 1.6f);
            var hat = g + face * 4f; hat.y = ctx.Ground(hat.x, hat.z); mk.Prism(dark, hat + up * .02f, up, .36f, .03f, 8); mk.Prism(dark, hat + up * .04f, up, .2f, .2f, 8, .8f);
            string[] say = { "From here the whole Delta is the right way up and you are not. He seems fine about it.", "He isn't being punished. He's waiting. There's a difference, he'd tell you, if he had a mouth.", "You tilt your head. The roads look like they go somewhere, this way round." }; var sn = 0;
            Spot("The Hanged Man", hat + up * 1.3f, "Look at it his way", new Vector3(1.6f, 2.6f, 1.6f)).onUse = _ => Say(tlife.transform, head + up * .9f, say[sn++ % say.Length], 5.5f);
        }

        /// 0: the Fool has been and gone: a bundle on a stick against a signpost that points straight up, a small white
        /// dog waiting, and footprints going off the road toward the edge of the land.
        void TheFool(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, Vector3 outw)
        {
            var up = Vector3.up; var rng = new System.Random(22); var side = Vector3.Cross(up, face);
            mk.Box(plankDark, g + up * 1.7f, new Vector3(.14f, 3.6f, .14f), Quaternion.LookRotation(face)); mk.Box(white, g + up * 3.8f, new Vector3(.3f, 1.1f, .05f), Quaternion.LookRotation(face)); mk.Cone(white, g + up * 4.35f, .32f, .5f, 4, 45f); Sign(g + up * 3.8f + face * .04f, face, "O\nN", .2f, Ink); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .14f);
            var st = g + side * .5f; mk.Prism(trunkM, st, (up + side * .28f).normalized, .025f, 2f, 4); mk.Blob(WorldMaterials.Surface(new Color(.82f, .76f, .5f), Surface.Cloth), st + (up + side * .28f).normalized * 1.9f, Vector3.one * .24f, rng, .2f);
            var dm = new MeshKit(); var fur = new Material(WorldMaterials.Glow(new Color(.95f, .93f, .86f), .35f)); dm.Blob(fur, new Vector3(0, .22f, 0), new Vector3(.16f, .22f, .2f), rng, 0f); dm.Blob(fur, new Vector3(0, .5f, .1f), Vector3.one * .13f, rng, 0f); dm.Box(fur, new Vector3(0, .47f, .24f), new Vector3(.08f, .08f, .14f), Quaternion.identity); foreach (var e in new[] { -.08f, .08f }) dm.Cone(fur, new Vector3(e, .6f, .08f), .04f, .1f, 4); dm.Prism(fur, new Vector3(0, .2f, -.18f), new Vector3(0, .6f, -.5f), .03f, .26f, 4);
            var dp = g + face * 1.6f - side * 1.2f; dp.y = ctx.Ground(dp.x, dp.z); var dgo = dm.BuildSingle("The white dog", tlife.transform, false); dgo.transform.SetPositionAndRotation(dp, Quaternion.LookRotation(outw)); string[] dog = { "It wags, and looks out toward the edge of the land, and back at you, and out again.", "The dog would like you to know that he went that way, and that it was a wonderful idea.", "It has a patience you don't. It has been waiting since before the lamps." }; var dn = 0;
            Interact.Toy(dgo, Toy.Kind.Custom, "Pat the white dog", new Vector3(.8f, .9f, 1f)).onUse = _ => Say(tlife.transform, dp + up * 1.6f, dog[dn++ % dog.Length], 5f);
            // footprints, pale, going out and not coming back
            var print = new Material(WorldMaterials.Glow(new Color(.9f, .86f, .7f), .5f)) { name = "Delta footprints" };
            for (var k = 0; k < 22; k++) { var p = g + outw * (2f + k * .8f) + Vector3.Cross(up, outw) * ((k % 2 == 0 ? .16f : -.16f) + Mathf.Sin(k * .5f) * .5f); if (!ctx.IsLand(p.x, p.z, 0f)) break; p.y = ctx.Ground(p.x, p.z) + .03f; mk.Box(print, p, new Vector3(.11f, .02f, .26f), Quaternion.LookRotation(outw)); }
            halos.Add(g + up * 3.8f, 1.6f, Gold, .3f); ArtistBits.PointLight(ctx.Root, g + face * 1.5f + up * 2.4f, Bulb, 14f, 1.5f);
        }

        /// XXI: a paddle wheel off some lost boat, standing in a pond, that still turns if you push it.
        void TheWorld(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); const float rad = 4.2f; var axle = g + up * (rad + .5f);
            var water = new Material(WorldMaterials.Glow(new Color(.3f, .5f, 1f), .7f)) { name = "Delta wheel pond" }; mk.Prism(water, g - up * .04f, up, 5.6f, .1f, 14); halos.Add(g + up * .4f, 5f, Blues, .25f);
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) { var foot = g + side * e * 2.3f + face * f * 2.8f - up * .3f; var top = axle + side * e * 2.3f; mk.Box(plankDark, (foot + top) / 2f, new Vector3(.28f, (top - foot).magnitude, .28f), Quaternion.FromToRotation(up, (top - foot).normalized)); ArtistBits.PostCollider(ctx.Root, foot, 2f, .2f); }
            mk.Prism(iron, axle - side * 2.6f, side, .16f, 5.2f, 8);
            var wm = new MeshKit(); for (var k = 0; k < 12; k++) { var q = Quaternion.Euler(k * 30f, 0, 0); wm.Box(plank, q * new Vector3(0, rad - .2f, 0), new Vector3(3.6f, .08f, .9f), q); foreach (var e in new[] { -1.7f, 1.7f }) wm.Box(plankDark, q * new Vector3(e, rad / 2f, 0), new Vector3(.1f, rad, .1f), q); }
            foreach (var e in new[] { -1.7f, 1.7f }) for (var k = 0; k < 12; k++) { var a0 = Quaternion.Euler(k * 30f, 0, 0) * new Vector3(e, rad - .5f, 0); var a1 = Quaternion.Euler(k * 30f + 30f, 0, 0) * new Vector3(e, rad - .5f, 0); wm.Prism(goldM, a0, a1 - a0, .05f, (a1 - a0).magnitude, 4); }
            GoldLine(wm, wm); var wgo = wm.BuildSingle("The World's wheel", tlife.transform, false); wgo.transform.SetPositionAndRotation(axle, Quaternion.LookRotation(face)); wgo.isStatic = false;
            string[] say = { "It goes round once, dripping, and comes back to where it was. So does everything.", "The wheel turns. Somewhere a card is the other way up now.", "Round it goes. You could swear the roads moved a little." }; var sn = 0;
            var toy = Interact.Toy(wgo, Toy.Kind.Spin, "Give the wheel a turn", new Vector3(4f, 8.6f, 8.6f)); toy.axis = Vector3.right; toy.amount = 360f; toy.seconds = 5f; toy.onUse = _ => Say(tlife.transform, axle + up * (rad + 1.4f), say[sn++ % say.Length], 5f);
            BoxCol(ctx.Root, axle, new Vector3(3.8f, rad * 1.6f, rad * 1.6f), Quaternion.LookRotation(face)); ArtistBits.PointLight(ctx.Root, axle + face * 5f, Blues, 20f, 1.6f);
        }

        // ---------------------------------------------------------------- the card reader
        void Reader(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = G(ctx, 11f, -11f); var face = FaceTo(g, center); var side = Vector3.Cross(up, face); var rng = new System.Random(78);
            // a round table under a cream cloth, three cards face down, a candle
            mk.Prism(white, g, up, .75f, .8f, 12); mk.Prism(white, g + up * .8f, up, .82f, .04f, 12); ArtistBits.PostCollider(ctx.Root, g, .8f, .78f);
            for (var k = -1; k <= 1; k++) mk.Box(cardM, g + up * .86f + side * k * .32f + face * .12f, new Vector3(.2f, .012f, .32f), Quaternion.LookRotation(face) * Quaternion.Euler(0, k * 9f, 0));
            for (var k = -1; k <= 1; k++) mk.Box(goldM, g + up * .868f + side * k * .32f + face * .12f, new Vector3(.12f, .004f, .22f), Quaternion.LookRotation(face) * Quaternion.Euler(0, k * 9f, 0));
            var cd = g + up * .86f - side * .5f - face * .2f; mk.Prism(white, cd, up, .03f, .16f, 6); mk.Blob(lampM, cd + up * .22f, new Vector3(.03f, .06f, .03f), rng, 0f); halos.Add(cd + up * .22f, .7f, Bulb, .5f); ArtistBits.PointLight(ctx.Root, cd + up * .6f, Bulb, 9f, 1.4f);
            // the reader behind it, facing the crossroads; two stools for whoever comes
            var seat = g - face * 1.15f; mk.Prism(plankDark, seat, up, .24f, .46f, 8); ArtistBits.PostCollider(ctx.Root, seat, .46f, .24f);
            var her = Local(); her.top = new Color(.16f, .12f, .3f); her.bottom = new Color(.1f, .09f, .2f); her.hat = Residents.Hat.None; her.longHair = true; her.hair = new Color(.82f, .82f, .86f);
            Folk(seat + up * .46f, face, Resident.Pose.Sit, new[] { "Sit. I turn one with every song. I don't choose them and neither do you.", "Twenty-two great cards. Fifteen of them live out there and play guitar. The rest are places.", "Look up. That's the one that's turned. The night will do what it says until the song is over.", "Every road out of here is one of the four suits, and every one of them comes home. Go and see.", "If he stands up at the crossroads and offers you something, you needn't take it. People do, though." }, her, null, .46f);
            foreach (var e in new[] { -.6f, .6f }) { var sp = g + face * 1.2f + side * e; mk.Prism(plankDark, sp, up, .22f, .46f, 8); ArtistBits.PostCollider(ctx.Root, sp, .46f, .22f); Interact.Seat(tlife.transform, sp + up * .48f, -face, "Sit for a reading").seatHeight = .46f; }
            // the card that is turned, hanging in the air over the table: one face for each card the night answers to
            var pivot = new GameObject("The turned card").transform; pivot.SetParent(life.transform, false);   // (built at the origin, then lifted into place, so the faces and their words are its own)
            foreach (var card in DeltaCards.Deck)
            {
                var go = new GameObject("Card " + Roman[card]).transform; go.SetParent(pivot, false); var m = new MeshKit(); CardFace(m, go, Vector3.zero, Quaternion.identity, 5.2f, card);
                var built = m.BuildSingle("Face", go, false); if (built != null) { built.isStatic = false; foreach (var r in built.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                go.gameObject.SetActive(false); cards.faces[card] = go.gameObject;
            }
            pivot.SetPositionAndRotation(g + up * 5.3f, Quaternion.LookRotation(face)); cards.pivot = pivot; halos.Add(g + up * 5.3f, 4.2f, Gold, .28f);
            Spot("The turned card", g + up * 1.5f + face * .2f, "Ask what the card means", new Vector3(1.4f, 1.4f, 1.4f)).onUse = _ => Say(tlife.transform, g + up * 2.6f, cards.Meaning(), 7f);
            Mark(g, 3f); keepOut.Add(new Vector3(g.x, 3.6f, g.z)); fixtures.Add(new Vector3(g.x, 2.2f, g.z));
            Shot(ctx, "the card reader", G(ctx, -3f, -5f) + up * 2.1f, g + up * 3.6f);
        }

        // ---------------------------------------------------------------- the moon
        /// A big storybook moon standing over Howlin' Wolf's side of the land. It swells when the Moon is turned.
        void Moon(LandContext ctx)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Delta moon"; Object.Destroy(go.GetComponent<Collider>()); go.transform.SetParent(life.transform, false);   // (a true round: a faceted one looked like crumpled paper)
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = moonM; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; life.moonSize = 110f;
            go.transform.position = center + (U * 92f - R * 48f).normalized * 760f + Vector3.up * 235f; go.SetActive(false); life.moon = go.transform; life.moonM = moonM;
            // the false dawn the Sun brings comes up in the east (DeltaLife makes the glow)
            life.dawnAt = center + R * 820f;
        }
    }

    /// The card that is turned with each song, and the crossroads' corners, which change while nobody is looking.
    public class DeltaCards : MonoBehaviour
    {
        const string Land = "The Delta Crossroads";
        /// The cards the night answers to (DeltaLife): the Moon, the Tower, the Star, the Sun, Death, the Hermit, the Wheel, the Devil.
        public static readonly int[] Deck = { 18, 16, 17, 19, 13, 9, 10, 15 };
        /// For looking at one card's night without waiting for its song (-1 = off).
        public static int Forced = -1;
        public Transform pivot; public GameObject[] faces = new GameObject[22]; public GameObject[] corner = new GameObject[3];
        public HashSet<string> johnson; public Vector3 center;
        public readonly Dictionary<string, int> bySong = new Dictionary<string, int>();
        /// The card showing now.
        public int Card { get; private set; } = 17;
        int target = -1, face = -1, cornerNow = -1; float flip = 1f; Quaternion rest; bool got;

        static readonly Dictionary<int, string> Meanings = new Dictionary<int, string>
        {
            { 18, "The Moon. Things aren't what they look like tonight. It will come down close to have a look at you." },
            { 16, "The Tower. What was built too tall comes down. Mind the sky." },
            { 17, "The Star. Hope, after. Look up: it's raining light." },
            { 19, "The Sun. Don't believe it. It never gets past the edge of the fields." },
            { 13, "Death. It doesn't mean dying. It means everything holds still for a while." },
            { 9, "The Hermit. One light is enough. Find the one that's still lit." },
            { 10, "The Wheel of Fortune. What's up comes down, and round again. Feel that wind." },
            { 15, "The Devil. He's on the west road, standing up. He only wants to talk." },
        };
        public string Meaning() => Meanings.TryGetValue(Card, out var s) ? s : "It's face down. Wait for the next song.";

        void Update()
        {
            if (pivot != null && !got) { got = true; rest = pivot.rotation; }
            var d = MusicDirector.Instance; var id = MusicDirector.ForcedTrackId ?? (d != null && d.CurrentSlot.IsValid ? d.CurrentSlot.track.id : null);
            // the card for this song: its own if it has one (every Delta song does), else drawn by the song, the same for everyone
            var want = 17;
            if (Forced >= 0) want = Forced;
            else if (id != null)
            {
                if (johnson != null && johnson.Contains(id)) want = 15;
                else if (!bySong.TryGetValue(id, out want)) want = Deck[(int)(SharedChance.Key(id) % (ulong)(Deck.Length - 1))];
            }
            if (want != target) { target = want; flip = face < 0 ? 1f : 0f; if (face < 0) Show(want); }
            if (flip < 1f)
            {
                flip = Mathf.Min(1f, flip + Time.deltaTime / 1.6f);
                if (flip >= .5f && face != target) Show(target);
            }
            if (pivot != null)
            {
                // it turns over edge-on and comes round showing the new card; between songs it sways a little
                var turn = flip < 1f ? Mathf.SmoothStep(0f, 1f, flip) * 180f + (flip >= .5f ? 180f : 0f) : 0f;
                pivot.rotation = rest * Quaternion.Euler(0f, turn + Mathf.Sin(Time.time * .5f) * 5f, Mathf.Sin(Time.time * .37f) * 1.5f);
            }
            // the corners: a new face every so often (the same for everyone), put on only while you are too far off to see it happen
            var wantCorner = (int)((SharedChance.Now / 110.0) % 3.0); var cam = Camera.main;
            var far = cam == null || (cam.transform.position - center).sqrMagnitude > 80f * 80f;
            if (cornerNow < 0 || (far && wantCorner != cornerNow))
            {
                cornerNow = cornerNow < 0 && !far ? 0 : wantCorner;
                for (var k = 0; k < corner.Length; k++) if (corner[k] != null && corner[k].activeSelf != (k == cornerNow)) corner[k].SetActive(k == cornerNow);
            }
        }

        void Show(int card)
        {
            face = card; Card = card;
            for (var k = 0; k < faces.Length; k++) if (faces[k] != null && faces[k].activeSelf != (k == card)) faces[k].SetActive(k == card);
        }
    }
}
