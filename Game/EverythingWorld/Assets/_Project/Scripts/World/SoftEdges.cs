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
    }
}
