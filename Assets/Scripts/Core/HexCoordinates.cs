using System;
using UnityEngine;

namespace SailboatGame.Core
{
    /// <summary>
    /// Represents axial coordinates for a hexagonal grid using the "pointy-top" orientation.
    /// Uses cube coordinates internally for easier pathfinding calculations.
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int Q { get; private set; } // Column
        public int R { get; private set; } // Row
        public int S => -Q - R; // Derived from cube coordinates (q + r + s = 0)

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>
        /// Converts hex coordinates to world position for pointy-top hexagons.
        /// </summary>
        public Vector3 ToWorldPosition(float hexSize)
        {
            float x = hexSize * (Mathf.Sqrt(3) * Q + Mathf.Sqrt(3) / 2 * R);
            float z = hexSize * (3f / 2f * R);
            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Converts world position to hex coordinates.
        /// </summary>
        public static HexCoordinates FromWorldPosition(Vector3 worldPos, float hexSize)
        {
            float q = (Mathf.Sqrt(3) / 3 * worldPos.x - 1f / 3f * worldPos.z) / hexSize;
            float r = (2f / 3f * worldPos.z) / hexSize;
            return RoundToHex(q, r);
        }

        /// <summary>
        /// Rounds fractional cube coordinates to nearest hex.
        /// </summary>
        private static HexCoordinates RoundToHex(float q, float r)
        {
            float s = -q - r;

            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                rq = -rr - rs;
            }
            else if (rDiff > sDiff)
            {
                rr = -rq - rs;
            }

            return new HexCoordinates(rq, rr);
        }

        /// <summary>
        /// Gets the distance between two hex coordinates.
        /// </summary>
        public int DistanceTo(HexCoordinates other)
        {
            return (Mathf.Abs(Q - other.Q) + Mathf.Abs(R - other.R) + Mathf.Abs(S - other.S)) / 2;
        }

        /// <summary>
        /// Gets all six neighbors of this hex coordinate.
        /// </summary>
        public HexCoordinates[] GetNeighbors()
        {
            return new[]
            {
                new HexCoordinates(Q + 1, R),     // E
                new HexCoordinates(Q + 1, R - 1), // NE
                new HexCoordinates(Q, R - 1),     // NW
                new HexCoordinates(Q - 1, R),     // W
                new HexCoordinates(Q - 1, R + 1), // SW
                new HexCoordinates(Q, R + 1)      // SE
            };
        }

        /// <summary>
        /// Gets a specific neighbor by direction index (0-5).
        /// </summary>
        public HexCoordinates GetNeighbor(int direction)
        {
            direction = direction % 6;
            if (direction < 0) direction += 6;

            return direction switch
            {
                0 => new HexCoordinates(Q + 1, R),
                1 => new HexCoordinates(Q + 1, R - 1),
                2 => new HexCoordinates(Q, R - 1),
                3 => new HexCoordinates(Q - 1, R),
                4 => new HexCoordinates(Q - 1, R + 1),
                5 => new HexCoordinates(Q, R + 1),
                _ => this
            };
        }

        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public override string ToString() => $"Hex({Q}, {R})";

        public static bool operator ==(HexCoordinates left, HexCoordinates right) => left.Equals(right);
        public static bool operator !=(HexCoordinates left, HexCoordinates right) => !left.Equals(right);
    }
}


