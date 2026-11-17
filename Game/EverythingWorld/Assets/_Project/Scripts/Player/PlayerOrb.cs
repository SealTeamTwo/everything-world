using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.Player
{
    /// A player's body: a glowing orb floating at eye level, with a very faint beam of the same colour running
    /// down to the ground and a soft glow where it touches, so you can feel the ground you're following. It bobs
    /// gently and carries a little light. Your own orb is around your eyes, so you don't see it (only its shadow,
    /// its light, the beam and the glow below you); other players will see the whole thing (set isLocal false and
    /// call Place each frame, for the multiplayer to come). The colour is picked at the hub's colour station and
    /// remembered.
    public class PlayerOrb : MonoBehaviour
    {
        /// The colours on offer, with a word for each. Spread over brightness as well as hue, so they tell apart
        /// even for colour-blind friends.
        public static readonly (string name, Color color)[] Palette =
        {
            ("Moonlight", new Color(.92f, .94f, 1f)), ("Gold", new Color(1f, .78f, .25f)), ("Ember", new Color(1f, .42f, .12f)), ("Rose", new Color(1f, .3f, .45f)),
            ("Magenta", new Color(.95f, .25f, .95f)), ("Violet", new Color(.55f, .35f, 1f)), ("Ocean", new Color(.2f, .45f, 1f)), ("Ice", new Color(.35f, .9f, 1f)),
            ("Mint", new Color(.35f, 1f, .65f)), ("Forest", new Color(.08f, .42f, .2f)), ("Lime", new Color(.7f, 1f, .2f)),
        };

        public bool isLocal = true;
        /// A showcase orb (the hub's colour station): just the orb, its pattern and wisps; no beam, glow or light.
        public bool display;
        public Transform eye;                       // the local player's camera pivot (the orb sits there)
        public float radius = .24f;

        /// What an orb can wear and the face it can have: each player picks one of each in the menu, and friends see it.
        /// A hat and a face sit on the side the player is looking toward, so from any angle you can tell where a friend
        /// is looking (from behind you see the back of the hat and no face).
        public static readonly string[] Hats = { "No hat", "Cap", "Top hat", "Cowboy hat", "Beanie", "Crown", "Party hat", "Headphones", "Halo", "Wizard hat" };
        public static readonly string[] EyeKinds = { "Dots", "Wide", "Sleepy", "Cross", "Sparkle", "Wink", "Tall", "No eyes" };
        public static readonly string[] MouthKinds = { "Smile", "Grin", "Oh", "Flat", "Frown", "Tongue", "Cat", "No mouth" };
        public int Hat { get; private set; }
        public int Eyes { get; private set; }
        public int Mouth { get; private set; }
        Transform look;   // the hat and the face, turned the way the player looks

        public static PlayerOrb Local { get; private set; }
        /// Other players' orbs: the way they're looking (degrees, like a compass) and whether their sunglasses are on.
        public float facing; public bool shades;
        Transform glasses; float shadesT, shownFacing; bool facingSet;
        public Color Color { get; private set; } = new Color(.92f, .94f, 1f);
        /// Which palette colour (shared with other players as one small number).
        public int Index { get; private set; }

        Transform orb, beam, glowDisc, pattern, pattern2, beacon; Material orbMat, coreMat, beamMat, discMat, lineMat, lineMat2, beaconMat; Light lamp; MeshRenderer orbR, coreR; ParticleSystem wisps;
        /// A friend (another player's orb, not a showcase): drawn bigger and brighter than the world's lanterns, with a
        /// stronger beam to the ground and, from a way off, a tall column of their colour rising into the sky.
        bool Friend => !isLocal && !display;
        Vector3 at; float seed;

        void Awake()
        {
            if (isLocal) Local = this;
            seed = Random.value * 10f;
            if (Friend) radius *= 1.35f;
            var root = new GameObject(isLocal ? "My orb" : "Orb").transform; root.SetParent(null, false);
            if (!isLocal) root.SetParent(transform, false);
            orb = root;
            // the orb: a soft outer shell and a brighter core
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(shell.GetComponent<Collider>()); shell.name = "Shell"; shell.transform.SetParent(orb, false); shell.transform.localScale = Vector3.one * radius * 2f;
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(core.GetComponent<Collider>()); core.name = "Core"; core.transform.SetParent(orb, false); core.transform.localScale = Vector3.one * radius * 1.1f;
            orbMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, .9f)) { name = "Orb shell" };
            coreMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, 3f)) { name = "Orb core" };
            orbR = shell.GetComponent<MeshRenderer>(); orbR.sharedMaterial = orbMat; coreR = core.GetComponent<MeshRenderer>(); coreR.sharedMaterial = coreMat;
            // your own orb is round your eyes: you only see its shadow
            if (isLocal) { orbR.shadowCastingMode = ShadowCastingMode.ShadowsOnly; coreR.shadowCastingMode = ShadowCastingMode.Off; coreR.enabled = false; }
            else { orbR.shadowCastingMode = ShadowCastingMode.On; coreR.shadowCastingMode = ShadowCastingMode.Off; }
            // the pattern: thin flowing lines in a deeper, more saturated shade, on two shells turning different ways
            lineMat = Blended("Orb lines", LinesTexture(7, 0)); lineMat2 = Blended("Orb lines 2", LinesTexture(4, 1));
            pattern = Shell("Lines", lineMat, radius * 2.06f); pattern2 = Shell("Lines 2", lineMat2, radius * 2.12f);
            if (isLocal) { pattern.gameObject.SetActive(false); pattern2.gameObject.SetActive(false); }
            // the wisps: threads of light that rise and curl off the orb and trail behind it (never on your own orb)
            if (!isLocal) wisps = Wisps();
            lamp = new GameObject("Orb light").AddComponent<Light>(); lamp.transform.SetParent(orb, false); lamp.type = LightType.Point; lamp.range = 5f; lamp.intensity = .7f; lamp.shadows = LightShadows.None;
            if (isLocal) lamp.transform.localPosition = new Vector3(0, .2f, .3f);
            if (Friend) { lamp.range = 9f; lamp.intensity = 1.6f; }   // a friend lights up the ground round them
            // the faint beam to the ground, and the glow where it lands
            beamMat = Additive("Orb beam", BeamTexture()); discMat = Additive("Orb glow", DiscTexture());
            beam = Quad("Beam", beamMat, root.parent == null ? null : root.parent); glowDisc = Quad("Ground glow", discMat, root.parent == null ? null : root.parent);
            if (display) { lamp.enabled = false; beam.gameObject.SetActive(false); glowDisc.gameObject.SetActive(false); }
            if (!isLocal) glasses = Glasses(root.parent);
            if (!isLocal) { look = new GameObject("Hat and face").transform; look.SetParent(root.parent, false); }
            if (isLocal) { Hat = Mathf.Clamp(PlayerPrefs.GetInt("EW_OrbHat", 0), 0, Hats.Length - 1); Eyes = Mathf.Clamp(PlayerPrefs.GetInt("EW_OrbEyes", 0), 0, EyeKinds.Length - 1); Mouth = Mathf.Clamp(PlayerPrefs.GetInt("EW_OrbMouth", 0), 0, MouthKinds.Length - 1); }
            else Rebuild();
            if (Friend) { beaconMat = Additive("Friend beacon", BeaconTexture()); beacon = Quad("Beacon", beaconMat, root.parent); beacon.gameObject.SetActive(false); }
            SetColor(isLocal ? Load() : Color);
        }

        Transform Shell(string name, Material m, float size)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = name; s.transform.SetParent(orb, false); s.transform.localScale = Vector3.one * size;
            var r = s.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; return s.transform;
        }

        /// A deeper, richer version of a colour for the lines (pale colours get a deep tint of their own).
        static Color Deep(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            if (s < .15f) return new Color(.32f, .38f, .78f);
            return Color.HSVToRGB(h, Mathf.Min(1f, s + .35f), Mathf.Min(v, .85f));
        }

        ParticleSystem Wisps()
        {
            var go = new GameObject("Wisps"); go.transform.SetParent(orb, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(.02f, .08f); main.startSize = new ParticleSystem.MinMaxCurve(.012f, .03f);
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = radius * .9f; sh.radiusThickness = .1f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(.18f, .38f); vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f); vel.orbitalY = new ParticleSystem.MinMaxCurve(.9f, 1.6f); vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f); vel.radial = new ParticleSystem.MinMaxCurve(.02f, .08f);
            var noise = ps.noise; noise.enabled = true; noise.strength = .25f; noise.frequency = 1.2f; noise.scrollSpeed = .4f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.7f, .2f), new GradientAlphaKey(.3f, .6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, .6f, 1f, 1.4f));
            // each mote leaves a thin ribbon that fades, so they read as threads of light curling away
            var tr = ps.trails; tr.enabled = true; tr.ratio = 1f; tr.lifetime = new ParticleSystem.MinMaxCurve(.35f, .6f); tr.minVertexDistance = .015f;
            tr.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f)); tr.inheritParticleColor = true; tr.dieWithParticles = true; tr.sizeAffectsWidth = true;
            var tg = new Gradient(); tg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(.95f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorOverTrail = tg;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = Additive("Orb wisps", DiscTexture()); r.trailMaterial = Additive("Orb wisp trails", RibbonTexture()); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        void OnDestroy() { if (orb != null) Destroy(orb.gameObject); if (beam != null) Destroy(beam.gameObject); if (glowDisc != null) Destroy(glowDisc.gameObject); if (beacon != null) Destroy(beacon.gameObject); }

        Color Load()
        {
            Index = Mathf.Clamp(PlayerPrefs.GetInt("EW_OrbColour", 0), 0, Palette.Length - 1);
            return Palette[Index].color;
        }

        /// Takes one of the palette's colours (and remembers it, for your own orb).
        public void Choose(int index)
        {
            index = Mathf.Clamp(index, 0, Palette.Length - 1); Index = index;
            if (isLocal) PlayerPrefs.SetInt("EW_OrbColour", index);
            SetColor(Palette[index].color);
        }

        /// Puts on a hat and a face (and remembers them, for your own orb).
        public void Dress(int hat, int eyes, int mouth)
        {
            hat = Mathf.Clamp(hat, 0, Hats.Length - 1); eyes = Mathf.Clamp(eyes, 0, EyeKinds.Length - 1); mouth = Mathf.Clamp(mouth, 0, MouthKinds.Length - 1);
            if (hat == Hat && eyes == Eyes && mouth == Mouth) return;
            Hat = hat; Eyes = eyes; Mouth = mouth;
            if (isLocal) { PlayerPrefs.SetInt("EW_OrbHat", hat); PlayerPrefs.SetInt("EW_OrbEyes", eyes); PlayerPrefs.SetInt("EW_OrbMouth", mouth); }
            Rebuild();
        }

        static Transform Bit(Transform parent, PrimitiveType kind, Material m, Vector3 at, Vector3 size, Quaternion? rot = null)
        {
            var b = GameObject.CreatePrimitive(kind); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(parent, false);
            b.transform.localPosition = at; b.transform.localScale = size; if (rot.HasValue) b.transform.localRotation = rot.Value;
            var r = b.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; return b.transform;
        }

        /// Builds the hat and the face again (nothing for your own orb: it is round your eyes).
        void Rebuild()
        {
            if (look == null) return;
            for (var i = look.childCount - 1; i >= 0; i--) Destroy(look.GetChild(i).gameObject);
            var M = EverythingWorld.World.WorldMaterials.Solid(new Color(.05f, .05f, .08f)); var white = EverythingWorld.World.WorldMaterials.Glow(Color.white, .7f);
            Material Paint(float r, float g, float b) => EverythingWorld.World.WorldMaterials.Solid(new Color(r, g, b)); Material Shine(float r, float g, float b, float s = 1.4f) => EverythingWorld.World.WorldMaterials.Glow(new Color(r, g, b), s);
            const float R = .24f; var cube = PrimitiveType.Cube; var ball = PrimitiveType.Sphere; var cyl = PrimitiveType.Cylinder;
            // a point on the front of the orb, a little proud of it
            Vector3 On(float x, float y) => new Vector3(x, y, Mathf.Sqrt(Mathf.Max(.0001f, R * R - x * x - y * y)) + .004f);

            // ---- the eyes
            var face = new GameObject("Face").transform; face.SetParent(look, false);
            foreach (var s in new[] { -1f, 1f })
            {
                var e = On(s * .078f, .05f); var wink = Eyes == 5 && s > 0f;
                switch (wink ? 2 : Eyes == 5 ? 0 : Eyes)
                {
                    case 0: Bit(face, ball, M, e, new Vector3(.05f, .05f, .02f)); Bit(face, ball, white, e + new Vector3(-.01f, .012f, .008f), Vector3.one * .014f); break;
                    case 1: Bit(face, ball, white, e, new Vector3(.095f, .095f, .02f)); Bit(face, ball, M, e + new Vector3(0, 0, .008f), new Vector3(.048f, .048f, .014f)); Bit(face, ball, white, e + new Vector3(-.012f, .014f, .016f), Vector3.one * .016f); break;
                    case 2: Bit(face, cube, M, e - Vector3.up * .01f, new Vector3(.07f, .016f, .012f), Quaternion.Euler(0, 0, s * -6f)); break;
                    case 3: Bit(face, cube, M, e, new Vector3(.08f, .022f, .012f), Quaternion.Euler(0, 0, s * 24f)); Bit(face, ball, M, e - Vector3.up * .028f, new Vector3(.036f, .036f, .014f)); break;
                    case 4: Bit(face, cube, white, e, new Vector3(.075f, .018f, .012f)); Bit(face, cube, white, e, new Vector3(.018f, .075f, .012f)); Bit(face, cube, white, e, new Vector3(.04f, .04f, .012f), Quaternion.Euler(0, 0, 45f)); break;
                    case 6: Bit(face, ball, M, e, new Vector3(.04f, .1f, .02f)); Bit(face, ball, white, e + new Vector3(-.006f, .024f, .008f), Vector3.one * .014f); break;
                }
            }
            // ---- the mouth
            var mo = On(0f, -.065f);
            void Arc(float up, float width, int n) { for (var i = 0; i < n; i++) { var k = i / (n - 1f) * 2f - 1f; var q = On(k * width, -.065f + (k * k - .5f) * up); Bit(face, cube, M, q, new Vector3(width * 2.4f / n, .014f, .012f), Quaternion.Euler(0, 0, k * 34f * Mathf.Sign(up))); } }
            switch (Mouth)
            {
                case 0: Arc(.03f, .055f, 5); break;
                case 1: Bit(face, cube, white, mo, new Vector3(.12f, .045f, .012f)); Bit(face, cube, M, mo + new Vector3(0, .026f, .002f), new Vector3(.13f, .012f, .012f)); Bit(face, cube, M, mo + new Vector3(0, 0, .004f), new Vector3(.01f, .045f, .008f)); break;
                case 2: Bit(face, ball, M, mo, new Vector3(.05f, .06f, .02f)); break;
                case 3: Bit(face, cube, M, mo, new Vector3(.085f, .015f, .012f)); break;
                case 4: Arc(-.03f, .055f, 5); break;
                case 5: Arc(.03f, .055f, 5); Bit(face, ball, Shine(1f, .45f, .6f, .5f), mo + new Vector3(.012f, -.03f, .004f), new Vector3(.042f, .05f, .016f)); break;
                case 6: foreach (var s in new[] { -1f, 1f }) { Bit(face, cube, M, On(s * .02f, -.07f), new Vector3(.04f, .014f, .012f), Quaternion.Euler(0, 0, s * -32f)); Bit(face, cube, M, On(s * .05f, -.062f), new Vector3(.035f, .014f, .012f), Quaternion.Euler(0, 0, s * 28f)); } break;
            }

            // ---- the hat (its front is +z, the way the player looks)
            if (Hat == 0) return;
            var hat = new GameObject("Hat").transform; hat.SetParent(look, false); hat.localPosition = new Vector3(0, R * .78f, -.01f); hat.localRotation = Quaternion.Euler(-7f, 0, 0);
            switch (Hat)
            {
                case 1:   // cap: a crown and a long brim out front
                    Bit(hat, ball, Paint(.12f, .2f, .55f), new Vector3(0, .0f, 0), new Vector3(.4f, .26f, .4f)); Bit(hat, cube, Paint(.9f, .2f, .2f), new Vector3(0, .01f, .24f), new Vector3(.26f, .022f, .24f)); Bit(hat, ball, white, new Vector3(0, .13f, 0), Vector3.one * .04f); break;
                case 2:   // top hat, with a red band and a buckle at the front
                    Bit(hat, cyl, M, new Vector3(0, .02f, 0), new Vector3(.46f, .01f, .46f)); Bit(hat, cyl, M, new Vector3(0, .19f, 0), new Vector3(.29f, .17f, .29f)); Bit(hat, cyl, Paint(.8f, .15f, .2f), new Vector3(0, .06f, 0), new Vector3(.296f, .03f, .296f)); Bit(hat, cube, Shine(1f, .8f, .2f), new Vector3(0, .06f, .15f), new Vector3(.05f, .05f, .01f)); break;
                case 3:   // cowboy hat: a wide brim, a pinched crown
                    Bit(hat, cyl, Paint(.55f, .36f, .2f), new Vector3(0, .02f, 0), new Vector3(.58f, .012f, .5f)); Bit(hat, ball, Paint(.55f, .36f, .2f), new Vector3(0, .1f, 0), new Vector3(.3f, .26f, .32f)); Bit(hat, cyl, Paint(.25f, .15f, .08f), new Vector3(0, .05f, 0), new Vector3(.31f, .015f, .33f)); Bit(hat, cube, Shine(1f, .85f, .3f), new Vector3(0, .05f, .168f), new Vector3(.04f, .03f, .01f)); break;
                case 4:   // beanie with a bobble
                    Bit(hat, ball, Paint(.85f, .2f, .25f), new Vector3(0, -.02f, 0), new Vector3(.43f, .36f, .43f)); Bit(hat, cyl, Paint(.95f, .95f, .9f), new Vector3(0, -.04f, 0), new Vector3(.44f, .03f, .44f)); Bit(hat, ball, white, new Vector3(0, .18f, 0), Vector3.one * .1f); break;
                case 5:   // crown: a gold band, five points, a jewel at the front
                    Bit(hat, cyl, Shine(1f, .8f, .2f, 1f), new Vector3(0, .04f, 0), new Vector3(.34f, .035f, .34f));
                    for (var i = 0; i < 5; i++) { var q = Quaternion.Euler(0, i * 72f, 0); Bit(hat, cube, Shine(1f, .8f, .2f, 1f), q * new Vector3(0, .12f, .15f), new Vector3(.07f, .1f, .02f), q * Quaternion.Euler(0, 0, 45f)); }
                    Bit(hat, ball, Shine(.9f, .15f, .3f, 1.6f), new Vector3(0, .05f, .175f), Vector3.one * .05f); break;
                case 6:   // party hat: a striped cone with a bobble
                    for (var i = 0; i < 6; i++) Bit(hat, cyl, i % 2 == 0 ? Paint(1f, .35f, .7f) : Paint(.3f, .8f, 1f), new Vector3(0, .02f + i * .06f, 0), new Vector3(.3f - i * .048f, .03f, .3f - i * .048f));
                    Bit(hat, ball, Shine(1f, .85f, .3f), new Vector3(0, .4f, 0), Vector3.one * .07f); break;
                case 7:   // headphones: a band over the top, a cup each side
                    Bit(hat, cube, M, new Vector3(0, .07f, 0), new Vector3(.4f, .03f, .06f)); foreach (var s in new[] { -1f, 1f }) { Bit(hat, cube, M, new Vector3(s * .215f, -.03f, 0), new Vector3(.03f, .22f, .06f), Quaternion.Euler(0, 0, s * 12f)); Bit(hat, cyl, M, new Vector3(s * .255f, -.17f, 0), new Vector3(.17f, .03f, .17f), Quaternion.Euler(0, 0, 90f)); Bit(hat, cyl, Shine(.3f, .9f, 1f), new Vector3(s * .29f, -.17f, 0), new Vector3(.1f, .005f, .1f), Quaternion.Euler(0, 0, 90f)); } break;
                case 8:   // halo: a ring of light floating over the orb
                    for (var i = 0; i < 14; i++) { var q = Quaternion.Euler(0, i * 25.7f, 0); Bit(hat, cube, Shine(1f, .9f, .5f, 2.2f), q * new Vector3(0, .14f, .17f), new Vector3(.08f, .022f, .03f), q); } break;
                case 9:   // wizard hat: a tall bent cone with a brim and a star
                    Bit(hat, cyl, Paint(.3f, .2f, .6f), new Vector3(0, .02f, 0), new Vector3(.54f, .012f, .54f));
                    for (var i = 0; i < 7; i++) Bit(hat, cyl, Paint(.3f, .2f, .6f), new Vector3(0, .05f + i * .065f, -i * i * .006f), new Vector3(.32f - i * .042f, .034f, .32f - i * .042f), Quaternion.Euler(-i * 4f, 0, 0));
                    Bit(hat, cube, Shine(1f, .9f, .4f), new Vector3(0, .1f, .155f), new Vector3(.05f, .05f, .01f), Quaternion.Euler(0, 0, 45f)); break;
            }
        }

        public void SetColor(Color c)
        {
            Color = c;
            var glow = Friend ? 1.1f : .65f;   // friends glow brighter than a lantern, so bloom picks them out
            orbMat.SetColor("_BaseColor", c); orbMat.SetColor("_EmissionColor", c * 1.2f * glow);
            coreMat.SetColor("_BaseColor", c); coreMat.SetColor("_EmissionColor", Color.Lerp(c, Color.white, .35f) * 3f * glow);
            lamp.color = c;
            beamMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, Friend ? .4f : .16f)); discMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, .5f));
            var d = Deep(c); lineMat.SetColor("_BaseColor", new Color(d.r, d.g, d.b, .95f)); lineMat2.SetColor("_BaseColor", new Color(d.r, d.g, d.b, .6f));
            if (wisps != null) { var m = wisps.main; m.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(c, Color.white, .4f), Color.Lerp(c, d, .5f)); }
        }

        /// Puts the orb at an eye position (for other players' orbs; your own follows your eyes).
        public void Place(Vector3 eyePosition) => at = eyePosition;

        void LateUpdate()
        {
            if (isLocal && eye != null) at = eye.position;
            var t = Time.time + seed;
            var p = at + Vector3.up * (isLocal ? 0f : Mathf.Sin(t * 1.6f) * .06f);
            orb.position = p; orb.rotation = Quaternion.Euler(0, t * 20f, 0);
            if (pattern != null) { pattern.localRotation = Quaternion.Euler(18f, t * 14f, 8f); pattern2.localRotation = Quaternion.Euler(-24f, -t * 23f, -12f); }
            var pulse = 1f + Mathf.Sin(t * 2.2f) * .04f; orb.localScale = Vector3.one * pulse;
            if (!isLocal)
            {
                // the hat, the face and the sunglasses all sit on the side the player is looking toward
                shownFacing = facingSet ? Mathf.LerpAngle(shownFacing, facing, 1f - Mathf.Exp(-10f * Time.deltaTime)) : facing; facingSet = true;
                if (look != null) { look.SetPositionAndRotation(p, Quaternion.Euler(0f, shownFacing, 0f)); look.localScale = Vector3.one * (radius / .24f) * pulse; }
            }
            if (glasses != null) PlaceGlasses(p);
            // the beam: from just under the orb to the ground (or 30 m down), always turned to the camera
            var ground = p - Vector3.up * 30f; var hitSomething = false;
            foreach (var h in Physics.RaycastAll(p - Vector3.up * (radius + .02f), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (!hitSomething || h.point.y > ground.y) { ground = h.point; hitSomething = true; }
            }
            var top = p - Vector3.up * radius * .7f; var len = Mathf.Max(.01f, top.y - ground.y);
            var cam = Camera.main; var face = cam != null ? cam.transform.position - (top + ground) / 2f : Vector3.forward; face.y = 0; if (face.sqrMagnitude < .001f) face = Vector3.forward;
            beam.SetPositionAndRotation((top + ground) / 2f, Quaternion.LookRotation(face.normalized)); beam.localScale = new Vector3(Friend ? .2f : .12f, len, 1f);
            glowDisc.gameObject.SetActive(hitSomething);
            if (hitSomething) { glowDisc.SetPositionAndRotation(ground + Vector3.up * .03f, Quaternion.Euler(90f, 0, 0)); glowDisc.localScale = Vector3.one * (Friend ? 1.5f : .9f) * (1f + Mathf.Sin(t * 2.2f) * .05f); }
            if (beacon != null) Beacon(p, cam);
        }

        /// The column of light over a friend: none close by (their orb is enough), fading in from 25 m to 70 m, and
        /// widening with distance so it stays a few pixels wide however far they are. It breathes slowly.
        void Beacon(Vector3 p, Camera cam)
        {
            if (cam == null) { beacon.gameObject.SetActive(false); return; }
            var to = p - cam.transform.position; var dist = to.magnitude;
            var fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 70f, dist));
            beacon.gameObject.SetActive(fade > .01f);
            if (fade <= .01f) return;
            const float height = 160f;
            var face = -to; face.y = 0f; if (face.sqrMagnitude < .001f) face = Vector3.forward;
            beacon.SetPositionAndRotation(p + Vector3.up * (radius + height / 2f), Quaternion.LookRotation(face.normalized));
            beacon.localScale = new Vector3(Mathf.Clamp(dist * .005f, .3f, 6f), height, 1f);
            var c = Color.Lerp(Color, Color.white, .25f);
            beaconMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, fade * (.75f + .15f * Mathf.Sin((Time.time + seed) * 1.3f))));
        }

        // ---------------------------------------------------------------- sunglasses on other players' orbs
        /// When a friend puts their sunglasses on, a pair drops onto the front of their orb (the side they're
        /// looking toward), dark lenses with a bright glint; when they take them off, the pair lifts away.
        void PlaceGlasses(Vector3 p)
        {
            shadesT = Mathf.MoveTowards(shadesT, shades ? 1f : 0f, Time.deltaTime / .45f);
            glasses.gameObject.SetActive(shadesT > 0f);
            if (shadesT <= 0f) return;
            var turn = Quaternion.Euler(0f, shownFacing, 0f); var e = Mathf.SmoothStep(0f, 1f, shadesT);
            glasses.SetPositionAndRotation(p + turn * new Vector3(0f, radius * .18f + (1f - e) * .35f, radius * .93f), turn * Quaternion.Euler((1f - e) * -40f, 0f, 0f));
            glasses.localScale = Vector3.one * Mathf.Lerp(.6f, 1f, e) * (radius / .24f);
        }

        static Transform Glasses(Transform parent)
        {
            var g = new GameObject("Sunglasses").transform; g.SetParent(parent, false);
            var lens = EverythingWorld.World.WorldMaterials.Solid(new Color(.04f, .04f, .06f));
            var frame = EverythingWorld.World.WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            var glint = EverythingWorld.World.WorldMaterials.Glow(new Color(.75f, .9f, 1f), 1.2f);
            void Bit(Material m, Vector3 at, Vector3 size, float tilt = 0f)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(g, false); b.transform.localPosition = at; b.transform.localScale = size; b.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                var r = b.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var s in new[] { -1f, 1f })
            {
                Bit(lens, new Vector3(s * .085f, 0f, 0f), new Vector3(.15f, .095f, .02f), s * -4f);              // a lens
                Bit(frame, new Vector3(s * .085f, .052f, 0f), new Vector3(.16f, .018f, .026f));                   // its top rim
                Bit(glint, new Vector3(s * .085f - .03f, .022f, -.012f), new Vector3(.045f, .01f, .004f), 28f);   // a glint
                Bit(frame, new Vector3(s * .16f, .035f, -.09f), new Vector3(.014f, .014f, .18f));                  // an arm back to the "ear"
            }
            Bit(frame, new Vector3(0f, .03f, 0f), new Vector3(.03f, .014f, .018f));   // the bridge
            g.gameObject.SetActive(false);
            return g;
        }

        // ---------------------------------------------------------------- pieces
        static Transform Quad(string name, Material m, Transform parent)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = name; if (parent != null) q.transform.SetParent(parent, false);
            var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return q.transform;
        }

        static Material Blended(string name, Texture2D tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 2f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent + 5;
            return m;
        }

        /// Thin flowing lines round a sphere (its UVs: around and up): wavy bands that drift like currents.
        /// 'bands' sets how many; 'kind' 1 gives a second, looser pattern.
        static Texture2D LinesTexture(int bands, int kind)
        {
            const int w = 256, h = 128; var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                float u = (x + .5f) / w, v = (y + .5f) / h; var a2 = u * Mathf.PI * 2f;
                var wave = kind == 0 ? Mathf.Sin(a2 * 2f + v * 9f) * .09f + Mathf.Sin(a2 * 5f) * .025f : Mathf.Sin(a2 * 3f - v * 6f) * .14f;
                var f = (v + wave) * bands; var d = Mathf.Abs(f - Mathf.Round(f));
                var line = Mathf.Clamp01(1f - d / (kind == 0 ? .07f : .05f));
                var pole = Mathf.Clamp01(Mathf.Sin(v * Mathf.PI) * 3f);
                var a = line * line * pole * (kind == 0 ? 1f : .8f + .2f * Mathf.Sin(a2 * 7f));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        static Material Additive(string name, Texture2D tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        /// Soft across, fading out toward the bottom (the ground end) and right under the orb.
        static Texture2D BeamTexture()
        {
            const int w = 16, h = 64; var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var across = 1f - Mathf.Abs((x + .5f) / w * 2f - 1f); var v = (y + .5f) / h;
                var a = across * across * Mathf.Lerp(.25f, 1f, v) * Mathf.Clamp01((1f - v) * 12f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        /// The friend's sky column: soft across, brightest at the bottom, fading out high up (and just over the orb).
        static Texture2D BeaconTexture()
        {
            const int w = 16, h = 128; var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var across = 1f - Mathf.Abs((x + .5f) / w * 2f - 1f); var v = (y + .5f) / h;
                var a = Mathf.Pow(across, 1.5f) * Mathf.Pow(1f - v, 1.4f) * Mathf.Clamp01(v * 60f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        /// Soft across the ribbon, so a trail reads as a thin glowing thread.
        static Texture2D RibbonTexture()
        {
            const int w = 8, h = 32; var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) { var across = 1f - Mathf.Abs((y + .5f) / h * 2f - 1f); t.SetPixel(x, y, new Color(1, 1, 1, across * across)); }
            t.Apply(); return t;
        }

        static Texture2D DiscTexture()
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f)));
            }
            t.Apply(); return t;
        }
    }
}
