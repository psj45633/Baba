using System.Collections.Generic;
using UnityEngine;

public class TextRule : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Grid grid;

    private TextBlock[] all;

    // 좌표 -> TextBlock (겹침 없음)
    private readonly Dictionary<Vector2Int, TextBlock> map = new();

    // 앞에서부터만 읽기(중복/역방향 방지)
    private static readonly Vector2Int[] dirs =
    {
        Vector2Int.right,
        Vector2Int.down,
    };

    // 이전 문장 집합(비교용)
    private readonly HashSet<SentenceKey> prevSentences = new();

    private void Awake()
    {
        if (!grid) grid = FindFirstObjectByType<Grid>();
    }

    private void Start()
    {
        RefreshAll();
        SyncAllTextPos();         // 시작 시 한 번 pos 세팅
        ScanSentence();  // 시작 시 현재 문장 저장
    }

    /// <summary>
    /// (선택) 텍스트 블록이 추가/삭제/비활성 등으로 바뀔 수 있으면,
    /// 스캔 전에 한 번 호출
    /// </summary>
    public void RefreshAll()
    {
        all = GetComponentsInChildren<TextBlock>(true);
    }

    /// <summary>
    /// 텍스트들의 pos를 월드좌표->그리드좌표로 동기화
    /// </summary>
    public void SyncAllTextPos()
    {
        if (all == null) RefreshAll();
        foreach (var t in all)
            t.SyncPosFromWorld(grid);
    }

    /// <summary>
    /// 현재 문장을 스캔하고,
    /// [완성] / [해제] 디버그를 자동으로 출력
    /// </summary>
    public HashSet<SentenceKey> ScanSentence()
    {
        if (all == null) RefreshAll();
        RebuildMap();

        var current = new HashSet<SentenceKey>();

        foreach (var a in all)
        {
            if (a.type != TextBlockType.Object) continue;

            foreach (var dir in dirs)
            {
                var bPos = a.pos + dir;
                var cPos = a.pos + dir * 2;

                if (!map.TryGetValue(bPos, out var b)) continue;
                if (!map.TryGetValue(cPos, out var c)) continue;

                if (IsOVO(a, b, c))
                {
                    current.Add(SentenceKey.MakeOVO(a.entityType, b.verbType, c.entityType));
                }
                else if (IsOVP(a, b, c))
                {
                    current.Add(SentenceKey.MakeOVP(a.entityType, b.verbType, c.entityState));
                }
            }
        }

        // ✅ 완성된 문장
        foreach (var s in current)
        {
            if (!prevSentences.Contains(s))
                Debug.Log($"[완성] {s}");
        }

        // ✅ 해제된 문장
        foreach (var s in prevSentences)
        {
            if (!current.Contains(s))
                Debug.Log($"[해제] {s}");
        }

        // prev 갱신
        prevSentences.Clear();
        foreach (var s in current)
            prevSentences.Add(s);

        return current;
    }

    // ---------------- 내부 ----------------

    private void RebuildMap()
    {
        map.Clear();
        foreach (var t in all)
            map[t.pos] = t;
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
}

/// <summary>
/// HashSet 비교용 문장 키(중복 제거 + 생성/해제 비교에 사용)
/// </summary>
public readonly struct SentenceKey
{
    public readonly bool isOVO;
    public readonly EntityType subject;
    public readonly VerbType verb;
    public readonly EntityType obj;       // OVO
    public readonly EntityState prop;     // OVP

    private SentenceKey(bool isOVO, EntityType subject, VerbType verb, EntityType obj, EntityState prop)
    {
        this.isOVO = isOVO;
        this.subject = subject;
        this.verb = verb;
        this.obj = obj;
        this.prop = prop;
    }

    public static SentenceKey MakeOVO(EntityType s, VerbType v, EntityType o)
        => new SentenceKey(true, s, v, o, EntityState.None);

    public static SentenceKey MakeOVP(EntityType s, VerbType v, EntityState p)
        => new SentenceKey(false, s, v, default, p);

    public override string ToString()
        => isOVO ? $"OVO : {subject} {verb} {obj}"
                 : $"OVP : {subject} {verb} {prop}";

    public bool Equals(SentenceKey other)
        => isOVO == other.isOVO
        && subject == other.subject
        && verb == other.verb
        && obj == other.obj
        && prop == other.prop;

    public override bool Equals(object obj)
        => obj is SentenceKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + isOVO.GetHashCode();
            h = h * 31 + subject.GetHashCode();
            h = h * 31 + verb.GetHashCode();
            h = h * 31 + this.obj.GetHashCode();
            h = h * 31 + prop.GetHashCode();
            return h;
        }
    }
}