using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PushTextController pushTextController;
    [SerializeField] private StageManager stageManager;

    [Header("Move")]
    [SerializeField] private float moveCooldown = 0.12f;
    [SerializeField] private float moveSpeed = 12f;

    private PlayerInputs input;
    private PlayerInputs.PlayerActions actions;

    private Vector2Int queuedDir;
    private float nextMoveTime;

    private bool isMoving;
    private Vector3 targetWorldPos;

    // 이번 스텝에 움직일 오브젝트들과 목표들
    private readonly List<Transform> movingObjects = new();
    private readonly List<Vector3> movingTargets = new();

    private void Awake()
    {
        input = new PlayerInputs();
        actions = input.Player;

        if (!pushTextController) pushTextController = GetComponent<PushTextController>();
        if (!stageManager) stageManager = FindFirstObjectByType<StageManager>();

        // 시작 위치 스냅(그리드 중앙)
        var grid = pushTextController.Grid;
        var startCell = grid.WorldToCell(transform.position);
        targetWorldPos = grid.GetCellCenterWorld(startCell);
        transform.position = targetWorldPos;
    }

    private void OnEnable()
    {
        actions.Enable();
        actions.Movement.performed += OnMove;
        actions.Movement.canceled += OnMove;
        actions.Return.performed += OnReturn;
        actions.Restart.performed += OnRestart;
    }

    private void OnDisable()
    {
        actions.Movement.performed -= OnMove;
        actions.Movement.canceled -= OnMove;
        actions.Return.performed -= OnReturn;
        actions.Restart.performed -= OnRestart;
        actions.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();

        // 대각선 방지: 큰 축만
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            queuedDir = new Vector2Int(v.x > 0 ? 1 : (v.x < 0 ? -1 : 0), 0);
        else
            queuedDir = new Vector2Int(0, v.y > 0 ? 1 : (v.y < 0 ? -1 : 0));
    }

    private void Update()
    {
        if (isMoving)
        {
            TickMove();
            return;
        }

        if (moveCooldown > 0f && Time.time < nextMoveTime) return;
        if (queuedDir == Vector2Int.zero) return;

        TryStartStep(queuedDir);
        nextMoveTime = Time.time + moveCooldown;
    }

    private void TryStartStep(Vector2Int dir)
    {
        bool ok = pushTextController.TryPlanStep(
            playerWorldPos: transform.position,
            dir: dir,
            playerTargetWorldPos: out Vector3 playerTarget,
            movingObjects: movingObjects,
            movingTargets: movingTargets
        );

        if (!ok) return;

        // 이동 확정되었으니, Undo 저장
        stageManager.SaveBeforeStep(transform.position, movingObjects);

        // 목표 세팅 후 이동 시작
        targetWorldPos = playerTarget;
        isMoving = true;
    }

    private void TickMove()
    {
        // 플레이어 이동
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
        bool playerDone = Vector3.Distance(transform.position, targetWorldPos) < 0.001f;

        // 오브젝트 이동
        bool objectsDone = true;
        for (int i = 0; i < movingObjects.Count; i++)
        {
            Transform obj = movingObjects[i];
            Vector3 t = movingTargets[i];

            if (obj == null) continue;

            obj.position = Vector3.MoveTowards(obj.position, t, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(obj.position, t) >= 0.001f)
                objectsDone = false;
        }

        if (playerDone && objectsDone)
        {
            // 스냅 마무리
            transform.position = targetWorldPos;
            for (int i = 0; i < movingObjects.Count; i++)
                if (movingObjects[i] != null) movingObjects[i].position = movingTargets[i];

            movingObjects.Clear();
            movingTargets.Clear();

            isMoving = false;
        }
    }

    private void OnReturn(InputAction.CallbackContext ctx)
    {
        if (isMoving) return;

        if (!stageManager.TryPop(out var state)) return;

        // Return도 부드럽게: 목표만 세팅하고 TickMove로 보내기
        targetWorldPos = state.playerPos;

        movingObjects.Clear();
        movingTargets.Clear();

        for (int i = 0; i < state.objects.Count; i++)
        {
            if (state.objects[i] == null) continue;
            movingObjects.Add(state.objects[i]);
            movingTargets.Add(state.objectPositions[i]);
        }

        queuedDir = Vector2Int.zero;
        isMoving = true;
    }

    private void OnRestart(InputAction.CallbackContext ctx)
    {
        stageManager.ClearHistory();
        Debug.Log("Restart");

        // (선택) 초기 상태로 돌아가기까지 하고 싶으면,
        // StageManager에 CaptureInitialState를 쓰는 방식으로 확장하면 됨.
    }
}

