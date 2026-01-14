using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [System.Serializable]
    public class StageState
    {
        public Vector3 playerPos;
        public List<Transform> objects = new();
        public List<Vector3> objectPositions = new();
    }

    private readonly Stack<StageState> history = new();

    private StageState initialState; // Restart용

    // 스테이지 시작 상태를 한 번 저장(필요하면 PlayerController에서 호출)
    public void CaptureInitialState(Vector3 playerPos, List<Transform> pushablesInStage)
    {
        initialState = new StageState { playerPos = playerPos };
        foreach (var t in pushablesInStage)
        {
            if (t == null) continue;
            initialState.objects.Add(t);
            initialState.objectPositions.Add(t.position);
        }
        history.Clear();
    }

    // 스텝 실행 직전 상태 저장(Undo 1칸)
    public void SaveBeforeStep(Vector3 playerPos, List<Transform> movingObjects)
    {
        StageState state = new StageState { playerPos = playerPos };

        // movingObjects만 저장해도 Undo는 동작하지만,
        // 나중에 더 안전하게 가려면 "스테이지의 모든 pushable"을 저장하는 방식으로 확장 가능.
        foreach (var t in movingObjects)
        {
            if (t == null) continue;
            state.objects.Add(t);
            state.objectPositions.Add(t.position);
        }

        history.Push(state);
    }

    public bool TryPop(out StageState state)
    {
        if (history.Count == 0)
        {
            state = null;
            return false;
        }
        state = history.Pop();
        return true;
    }

    public void ClearHistory() => history.Clear();

    public bool HasInitialState => initialState != null;

    public StageState GetInitialState() => initialState;
}
