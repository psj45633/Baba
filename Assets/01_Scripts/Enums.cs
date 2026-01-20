using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TextBlockType
{
    Object,
    Verb,
    Property
}

public enum EntityType
{
    None,
    Baba,
    Rock,
    Wall,

}

public enum VerbType
{
    None,
    Is,

}

[Flags]
public enum EntityState
{
    None = 0,
    You = 1 << 0,
    Push = 1 << 1,
    Stop = 1 << 2,
    Win = 1 << 3,
    Sink = 1 << 4,
    Defeat = 1 << 5,
}