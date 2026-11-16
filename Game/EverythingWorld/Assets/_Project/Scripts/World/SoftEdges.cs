using UnityEngine;

namespace EverythingWorld.World
{
    /// Soft edges for things laid flat on the ground (paths, clearings, plazas): the edge wanders a little instead of
    /// running dead straight or perfectly round, the way a trodden path or a cleared patch of snow really does. Shared; each land decides where to use it.
    public static class SoftEdges
    {
        /// How far the edge wanders here: a smooth value from 1 - amount to 1 + amount, the same for anything at this
        /// spot (so the two sides of a bend, or a path meeting a clearing, agree).
        public static float Wander(float x, float z, float seed, float amount = .25f, float size = 4f) =>
            1f + (Mathf.PerlinNoise(x / size + seed * 13.1f + 7.3f, z / size + seed * 5.7f + 2.9f) - .5f) * 2f * amount;

        /// Soft path edges in the lands' shared path strips (set by IslandWorldBuilder while a land is built).
        public static bool On;
        /// Lands whose paths are built (pavements, streets, boardwalks, promenades, neon and digital floors): these
        /// stay straight. Every other land's paths are worn, and wander.
        public static readonly System.Collections.Generic.HashSet<string> BuiltPaths = new System.Collections.Generic.HashSet<string>
        {
            "The Everything Hub", "The Game Isles", "Boom Bap Harbour", "Discotopia", "The Post-Punk Docklands", "The Electro Foundry",
            "Garage Rock Row", "The IDM Glitchlands", "The Hair Metal Strip", "The Heartbreak Boardwalk", "The Hip-Hop Boroughs",
            "The Old Jazz Quarter", "The Neo Soul Quarter", "The Pop Republic", "Studio Lot Island", "The Synthwave Coast",
            "The Trip-Hop Undercity", "The Velvet Rooftops", "The Yacht Rock Riviera",
        };
        /// How much one side of a path (seed 1 = left, 2 = right) is widened or narrowed at 'p': footpaths wander,
        /// wide roads (5.5 m and up) stay straight, the way a road is built and a path is worn.
        public static float Edge(Vector3 p, float width, float seed)
        {
            if (!On || width >= 5.5f) return 1f;
            return Wander(p.x, p.z, seed, .28f * Mathf.Clamp01((5.5f - width) / 2f), 3f);
        }
        /// The same for a point across a path laid in lanes ('t' 0 = left edge, 1 = right edge).
        public static float Lane(Vector3 p, float width, float t) => t < .5f ? Edge(p, width, 1f) : t > .5f ? Edge(p, width, 2f) : 1f;
    }
}
