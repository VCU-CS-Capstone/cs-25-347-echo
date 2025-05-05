using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
/// <summary>
/// Constrains the position of a target Transform to remain within a boundary
/// shaped like a quarter of an ellipsoid, defined in this component's local space.
/// The boundary is defined by ellipsoid radii and two cutting planes.
/// Runs in the editor due to [ExecuteAlways].
/// </summary>
public class QuarterSphereConstraint : MonoBehaviour
{
    /// <summary>
    /// Defines the size of the ellipsoid boundary along each local axis (X, Y, Z).
    /// </summary>
    [Header("Boundary Shape")]
    [Tooltip("Semi‑axes of the ellipsoid (radius along each local axis).")]
    public Vector3 radii = new Vector3(1f, 1f, 1f);
    /// <summary>
    /// Defines the lower boundary plane in local Y coordinates. The target cannot go below this value.
    /// </summary>
    [Tooltip("Forbid any point with local Y < this value.")]
    public float cutPlaneY = 0f;
    /// <summary>
    /// Defines the upper boundary plane in local Z coordinates. The target cannot go above this value.
    /// </summary>
    [Tooltip("Forbid any point with local Z > this value.")]
    public float cutPlaneZ = 0f;

    /// <summary>
    /// The Transform whose world position will be constrained within the defined boundary.
    /// </summary>
    [Header("Constraint Target")]
    [Tooltip("The Transform to clamp inside this quarter‑ellipsoid.")]
    public Transform target;

    // Ensures the constraint is applied after all other position updates.
    void LateUpdate()
    {
        if (target == null) return;

        // to local space
        Vector3 p = transform.InverseTransformPoint(target.position);
        // clamp into ellipsoid & planes
        Vector3 c = ClampToQuarterEllipsoid(p);
        // back to world
        target.position = transform.TransformPoint(c);
    }

    Vector3 ClampToQuarterEllipsoid(Vector3 p)
    {
        float rx = radii.x, ry = radii.y, rz = radii.z;
        float y0 = cutPlaneY, z0 = cutPlaneZ;

        // If already inside ellipsoid & both half‑spaces
        if ((p.x * p.x) / (rx * rx) + (p.y * p.y) / (ry * ry) + (p.z * p.z) / (rz * rz) <= 1f
            && p.y >= y0 && p.z <= z0)
            return p;

        var candidates = new List<Vector3>();

        // 1) Flat horizontal plane Y = y0
        if (p.y < y0)
        {
            var q = new Vector3(p.x, y0, p.z);
            if ((q.x * q.x) / (rx * rx) + (q.y * q.y) / (ry * ry) + (q.z * q.z) / (rz * rz) <= 1f
                && q.z <= z0)
                candidates.Add(q);
        }
        // 2) Flat vertical plane Z = z0
        if (p.z > z0)
        {
            var q = new Vector3(p.x, p.y, z0);
            if ((q.x * q.x) / (rx * rx) + (q.y * q.y) / (ry * ry) + (q.z * q.z) / (rz * rz) <= 1f
                && q.y >= y0)
                candidates.Add(q);
        }
        // 3) Ellipsoid shell (radial projection)
        if (p.y >= y0 && p.z <= z0)
        {
            Vector3 n = new Vector3(p.x / rx, p.y / ry, p.z / rz);
            n.Normalize();
            candidates.Add(new Vector3(n.x * rx, n.y * ry, n.z * rz));
        }
        // 4) Edge‐cases: project plane‑points to ellipsoid
        if (p.y < y0 && p.z <= z0)
        {
            var q = new Vector3(p.x, y0, p.z);
            Vector3 n = new Vector3(q.x / rx, q.y / ry, q.z / rz);
            n.Normalize();
            candidates.Add(new Vector3(n.x * rx, n.y * ry, n.z * rz));
        }
        if (p.z > z0 && p.y >= y0)
        {
            var q = new Vector3(p.x, p.y, z0);
            Vector3 n = new Vector3(q.x / rx, q.y / ry, q.z / rz);
            n.Normalize();
            candidates.Add(new Vector3(n.x * rx, n.y * ry, n.z * rz));
        }
        // 5) Intersection line of both planes: Y=y0 & Z=z0 within ellipsoid
        if (p.y < y0 && p.z > z0)
        {
            float rem = 1f - (y0 * y0) / (ry * ry) - (z0 * z0) / (rz * rz);
            if (rem > 0f)
            {
                float xMax = rx * Mathf.Sqrt(rem);
                float xE = Mathf.Clamp(p.x, -xMax, xMax);
                candidates.Add(new Vector3(xE, y0, z0));
            }
        }

        // Pick the candidate closest to the original p
        Vector3 best = p;
        float bestDist = float.MaxValue;
        foreach (var c in candidates)
        {
            float d = (c - p).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        // Fallback: clamp to planes + radial ellipsoid
        if (candidates.Count == 0)
        {
            Vector3 q = new Vector3(
                p.x,
                Mathf.Max(p.y, y0),
                Mathf.Min(p.z, z0)
            );
            Vector3 n = new Vector3(q.x / rx, q.y / ry, q.z / rz);
            n.Normalize();
            best = new Vector3(n.x * rx, n.y * ry, n.z * rz);
        }

        return best;
    }

    void OnDrawGizmosSelected()
    {
        // Draw wire‑ellipsoid by scaling a unit sphere
        Matrix4x4 oldMat = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(radii);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Vector3.zero, 1f);
        Gizmos.matrix = oldMat;

        // Horizontal cut plane (Y = cutPlaneY)
        Gizmos.color = Color.yellow;
        Vector3 hc = new Vector3(0f, cutPlaneY, 0f);
        Vector3 hs = new Vector3(radii.x * 2f, 0f, radii.z * 2f);
        Gizmos.DrawWireCube(transform.TransformPoint(hc), hs);

        // Vertical cut plane (Z = cutPlaneZ)
        Gizmos.color = Color.magenta;
        Vector3 vc = new Vector3(0f, 0f, cutPlaneZ);
        Vector3 vs = new Vector3(radii.x * 2f, radii.y * 2f, 0f);
        Gizmos.DrawWireCube(transform.TransformPoint(vc), vs);
    }
}
