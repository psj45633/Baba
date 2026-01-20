using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextBlock : MonoBehaviour
{
    public Vector2Int pos;

    public TextBlockType type;

    public EntityType entityType;

    public VerbType verbType;

    public EntityState entityState;

    public void SyncPosFromWorld(Grid grid)
    {
        Vector3Int cell = grid.WorldToCell(transform.position);
        pos = new Vector2Int(cell.x, cell.y);

    }
}
