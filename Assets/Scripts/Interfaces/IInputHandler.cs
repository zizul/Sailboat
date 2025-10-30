using System;
using UnityEngine;
using SailboatGame.Core;

namespace SailboatGame.Interfaces
{
    /// <summary>
    /// Abstract base class for input handling systems.
    /// Allows switching between mouse, touch, gamepad, AI control, etc.
    /// </summary>
    public abstract class IInputHandler : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when a tile is clicked/selected.
        /// </summary>
        public abstract event Action<HexCoordinates> OnTileClicked;

        /// <summary>
        /// Event triggered when a world position is clicked.
        /// </summary>
        public abstract event Action<Vector3> OnWorldPositionClicked;

        /// <summary>
        /// Enables or disables input handling.
        /// </summary>
        public abstract void SetEnabled(bool enabled);
    }
}

