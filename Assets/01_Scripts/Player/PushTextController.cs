using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PushTextController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap collisionTilemap;  // 벽/막힘 타일맵
    [SerializeField] private LayerMask pushableMask;    // 밀 수 있는 오브젝트 레이어

    public Grid Grid => grid;

    private void Awake()
    {
        if (!grid) grid = FindFirstObjectByType<Grid>();
    }

    // playerWorldPos 기준으로 dir 방향으로 한 칸 이동할 때,
    // 연쇄 밀기가 포함된 이동 계획을 계산한다.
    public bool TryPlanStep(
        Vector3 playerWorldPos,
        Vector2Int dir,
        out Vector3 playerTargetWorldPos,
        List<Transform> movingObjects,
        List<Vector3> movingTargets
    )
    {
        movingObjects.Clear();
        movingTargets.Clear();

        if (dir == Vector2Int.zero)
        {
            playerTargetWorldPos = playerWorldPos;
            return false;
        }

        Vector3Int curCell = grid.WorldToCell(playerWorldPos);
        Vector3Int nextCell = curCell + (Vector3Int)dir;

        // 플레이어가 갈 칸이 벽이면 불가
        if (IsBlocked(nextCell))
        {
            playerTargetWorldPos = playerWorldPos;
            return false;
        }

        // nextCell부터 같은 방향으로 "연속된 밀 오브젝트"가 있는지 스캔해서
        // 끝의 빈칸을 찾는다.
        Vector3Int scan = nextCell;

        // 첫 칸에 밀 오브젝트가 없으면 플레이어만 이동하면 됨
        Transform first = FindPushableAtCell(scan);
        if (first == null)
        {
            playerTargetWorldPos = grid.GetCellCenterWorld(nextCell);
            return true;
        }

        // 연쇄 밀기: 빈칸이 나올 때까지 전진
        while (true)
        {
            // 스캔 중 벽이면 실패
            if (IsBlocked(scan))
            {
                playerTargetWorldPos = playerWorldPos;
                return false;
            }

            Transform obj = FindPushableAtCell(scan);
            if (obj == null) break; // 빈칸 발견

            scan += (Vector3Int)dir;
        }

        // scan은 "빈칸"이므로, 뒤에서부터 한 칸씩 이동 목표를 만든다
        Vector3Int moveTo = scan;
        Vector3Int moveFrom = scan - (Vector3Int)dir;

        while (moveFrom != nextCell - (Vector3Int)dir)
        {
            Transform obj = FindPushableAtCell(moveFrom);
            if (obj != null)
            {
                movingObjects.Add(obj);
                movingTargets.Add(grid.GetCellCenterWorld(moveTo));
            }

            moveTo = moveFrom;
            moveFrom -= (Vector3Int)dir;
        }

        playerTargetWorldPos = grid.GetCellCenterWorld(nextCell);
        return true;
    }

    private bool IsBlocked(Vector3Int cell)
    {
        return collisionTilemap != null && collisionTilemap.HasTile(cell);
    }

    private Transform FindPushableAtCell(Vector3Int cell)
    {
        Vector3 world = grid.GetCellCenterWorld(cell);
        Collider2D col = Physics2D.OverlapPoint(world, pushableMask);
        return col ? col.transform : null;
    }
}
