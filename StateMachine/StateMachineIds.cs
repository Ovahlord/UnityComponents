using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateMachineIds : int
{
    None                    = 0,
    GroundedMovement        = 1,
    JumpingMovement         = 2,
    FallingMovement         = 3,
    SlopeSlidingMovement    = 4
}
