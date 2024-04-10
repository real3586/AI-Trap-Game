using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enums : MonoBehaviour
{
    public enum Directions { North, South, West, East, NorthEast, SouthEast, NorthWest, SouthWest }
    public enum BlockedDirections { NortheastBlocked, NorthwestBlocked, SoutheastBlocked, SouthwestBlocked }

    public static readonly Dictionary<Directions, Vector3> directionToVector = new()
    {
        {Directions.North, Vector3.forward},
        {Directions.South, Vector3.back },
        {Directions.West, Vector3.left },
        {Directions.East, Vector3.right },
        {Directions.NorthEast, Vector3.forward + Vector3.right },
        {Directions.SouthEast, Vector3.back + Vector3.right },
        {Directions.NorthWest, Vector3.forward + Vector3.left },
        {Directions.SouthWest, Vector3.back + Vector3.left }
    };
    public static readonly Dictionary<Directions, float> directionToRotation = new()
    {
        {Directions.North, 0},
        {Directions.South, 180 },
        {Directions.West, 270 },
        {Directions.East, 90 },
        {Directions.NorthEast, 45 },
        {Directions.SouthEast, 135 },
        {Directions.NorthWest, 315 },
        {Directions.SouthWest, 225 }
    };
    public static readonly Dictionary<Vector3, Directions> vectorToDirection = new()
    {
        {Vector3.forward, Directions.North},
        {Vector3.back , Directions.South },
        {Vector3.left , Directions.West },
        {Vector3.right , Directions.East },
        {Vector3.forward + Vector3.right, Directions.NorthEast},
        {Vector3.back + Vector3.right , Directions.SouthEast },
        {Vector3.forward + Vector3.left , Directions.NorthWest },
        {Vector3.back + Vector3.left , Directions.SouthWest }
    };
    public enum Modes { Classic, User, Algo }
}
