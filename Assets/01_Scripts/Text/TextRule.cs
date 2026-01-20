using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextRule : MonoBehaviour
{
    private TextBlock[] all;
    [SerializeField] private Grid grid;

    // 좌표 -> TextBlock (겹침 없음)
    private readonly Dictionary<Vector2Int, TextBlock> map = new();

    private static readonly Vector2Int[] dirs =
    {
        Vector2Int.right,
        Vector2Int.down,
    };

    private void Awake()
    {
        if (!grid) grid = FindFirstObjectByType<Grid>();
    }

    private void Start()
    {
        all = GetComponentsInChildren<TextBlock>(true);
        SyncAllTextPos(grid);
    }




    /// <summary>
    /// 가로/세로로 연속 3개가 있는지 검사하고
    /// OVO / OVP만 디버그 출력
    /// </summary>
    public void SentenceScan()
    {
        RebuildMap();

        foreach (var a in all)
        {
            if(a.type != TextBlockType.Object) continue;
            foreach (var dir in dirs)
            {
                var bPos = a.pos + dir;
                var cPos = a.pos + dir * 2;
                if (!map.TryGetValue(bPos, out var b)) continue;
                if (!map.TryGetValue(cPos, out var c)) continue;
                // 이제 a-b-c는 "연속 3칸" 성립
                if (IsOVO(a, b, c))
                {
                    Debug.Log($"OVO : {a.entityType} {b.verbType} {c.entityType}");
                }
                else if (IsOVP(a, b, c))
                {
                    Debug.Log($"OVP : {a.entityType} {b.verbType} {c.entityState}");
                }
            }
        }
    }

    private bool IsOVO(TextBlock a, TextBlock b, TextBlock c)
    {
        return a.type == TextBlockType.Object
            && b.type == TextBlockType.Verb
            && b.verbType == VerbType.Is
            && c.type == TextBlockType.Object;
    }

    private bool IsOVP(TextBlock a, TextBlock b, TextBlock c)
    {
        return a.type == TextBlockType.Object
            && b.type == TextBlockType.Verb
            && b.verbType == VerbType.Is
            && c.type == TextBlockType.Property
            && c.entityState != EntityState.None;
    }

    private void RebuildMap()
    {
        map.Clear();
        var all = GetComponentsInChildren<TextBlock>(true);
        foreach (var t in all)
        {
            map[t.pos] = t; // 겹침 없으니 바로 대입
        }
    }

    public void SyncAllTextPos(Grid grid)
    {
        foreach (var t in all)
            t.SyncPosFromWorld(grid);
    }
}
