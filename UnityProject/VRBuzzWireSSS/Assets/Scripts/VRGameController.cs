using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// VR 電流急急棒遊戲控制器（Unity 物理引擎版本）
/// 使用 Unity Collider 進行碰撞偵測，Python 只負責提供軌道資料
/// </summary>
public class VRGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PythonClient pythonClient;
    [SerializeField] private TrackRenderer trackRenderer;
    [SerializeField] private RingCollisionDetector ringCollisionDetector;
    
    [Header("VR Controller")]
    [Tooltip("右手控制器物件")]
    [SerializeField] private Transform rightController;
    
    [Tooltip("鐵環物件")]
    [SerializeField] private Transform ringTransform;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference triggerAction;
    
    [Header("Game Settings")]
    [SerializeField] private float vibrationIntensity = 0.5f;
    [SerializeField] private float vibrationDuration = 0.1f;
    [SerializeField] private float collisionCooldown = 0.5f;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI collisionText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject gameCompletePanel;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color collisionColor = Color.red;
    [SerializeField] private Renderer ringRenderer;

    [Header("Haptic Feedback")]
    [SerializeField] private XRBaseController xrController;

    // 遊戲狀態
    private bool isGameStarted = false;
    private bool isGameCompleted = false;
    private bool isTimingStarted = false;
    
    // 本地計時和碰撞計數
    private float localElapsedTime = 0f;
    private int localCollisionCount = 0;
    private bool wasColliding = false;
    private float lastCollisionTime = 0f;
    
    // 起點終點區域
    private Vector3 startZone;
    private Vector3 endZone;
    private float zoneRadius = 0.1f;

    void Start()
    {
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
        
        UpdateStatusText("Connecting to server...");
        
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Enable();
        }
        
        // 訂閱碰撞事件
        if (ringCollisionDetector != null)
        {
            ringCollisionDetector.OnCollisionStart.AddListener(OnRingCollisionStart);
            ringCollisionDetector.OnCollisionEnd.AddListener(OnRingCollisionEnd);
        }
        
        StartCoroutine(ConnectToServer());
    }

    void OnDestroy()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Disable();
        }
        
        if (ringCollisionDetector != null)
        {
            ringCollisionDetector.OnCollisionStart.RemoveListener(OnRingCollisionStart);
            ringCollisionDetector.OnCollisionEnd.RemoveListener(OnRingCollisionEnd);
        }
    }

    IEnumerator ConnectToServer()
    {
        bool serverOk = false;
        yield return pythonClient.CheckHealth(ok => serverOk = ok);
        
        if (!serverOk)
        {
            UpdateStatusText("Cannot connect to server!\nMake sure Python server is running.");
            yield break;
        }
        
        UpdateStatusText("Server connected!\nPress Trigger to start.");
        
        while (!isGameStarted)
        {
            if (GetTriggerPressed())
            {
                yield return StartGame();
            }
            yield return null;
        }
    }

    IEnumerator StartGame()
    {
        UpdateStatusText("Starting game...");
        
        yield return pythonClient.StartGame(
            0.05f, // collision threshold（在物理引擎版本中不使用）
            onSuccess: state =>
            {
                if (state.track_points != null && state.track_points.Count > 0)
                {
                    trackRenderer.RenderTrack(
                        state.track_points,
                        state.start_zone,
                        state.end_zone,
                        state.zone_radius
                    );
                    
                    // 儲存起點終點位置
                    if (state.start_zone != null && state.start_zone.Count >= 3)
                    {
                        startZone = new Vector3(state.start_zone[0], state.start_zone[1], state.start_zone[2]);
                    }
                    if (state.end_zone != null && state.end_zone.Count >= 3)
                    {
                        endZone = new Vector3(state.end_zone[0], state.end_zone[1], state.end_zone[2]);
                    }
                    zoneRadius = state.zone_radius;
                    
                    Debug.Log($"[Game] Track rendered with {state.track_points.Count} points");
                    Debug.Log($"[Game] Start zone: {startZone}, End zone: {endZone}, Radius: {zoneRadius}");
                }
                else
                {
                    Debug.LogError("Track points invalid or empty!");
                }
                
                // 重置本地狀態
                isGameStarted = true;
                isGameCompleted = false;
                isTimingStarted = false;
                localElapsedTime = 0f;
                localCollisionCount = 0;
                wasColliding = false;
                
                UpdateStatusText("Move ring to START zone!");
            },
            onError: error =>
            {
                UpdateStatusText("Error: " + error);
                Debug.LogError(error);
            }
        );
    }

    void Update()
    {
        if (!isGameStarted || isGameCompleted) return;
        
        // 取得 Ring 位置
        Vector3 ringPosition = ringTransform != null ? ringTransform.position : 
                              (rightController != null ? rightController.position : Vector3.zero);
        
        // 檢查是否進入起點區域（開始計時）
        if (!isTimingStarted)
        {
            if (IsInZone(ringPosition, startZone, zoneRadius))
            {
                isTimingStarted = true;
                localElapsedTime = 0f;
                UpdateStatusText("GO! Follow the track!");
                Debug.Log("[Game] Timer started!");
            }
        }
        else
        {
            // 計時
            localElapsedTime += Time.deltaTime;
            
            // 檢查碰撞（使用 RingCollisionDetector 的狀態）
            bool isCurrentlyColliding = ringCollisionDetector != null && ringCollisionDetector.IsColliding;
            
            // 碰撞計數（有冷卻時間）
            if (isCurrentlyColliding && !wasColliding)
            {
                if (Time.time - lastCollisionTime > collisionCooldown)
                {
                    localCollisionCount++;
                    lastCollisionTime = Time.time;
                    Debug.Log($"[Game] Collision! Count: {localCollisionCount}");
                    TriggerHapticFeedback();
                }
            }
            wasColliding = isCurrentlyColliding;
            
            // 更新視覺效果
            if (isCurrentlyColliding)
            {
                OnCollision();
            }
            else
            {
                OnNoCollision();
            }
            
            // 檢查是否到達終點
            if (IsInZone(ringPosition, endZone, zoneRadius))
            {
                OnGameComplete();
            }
        }
        
        // 更新 UI
        UpdateUI();
        
        // 遊戲完成後按 Trigger 重新開始
        if (isGameCompleted && GetTriggerPressed())
        {
            StartCoroutine(ResetGame());
        }
    }

    bool IsInZone(Vector3 position, Vector3 zoneCenter, float radius)
    {
        return Vector3.Distance(position, zoneCenter) < radius;
    }

    void UpdateUI()
    {
        if (timeText != null)
        {
            timeText.text = $"Time: {localElapsedTime:F1}s";
        }
        
        if (collisionText != null)
        {
            collisionText.text = $"Collisions: {localCollisionCount}";
        }
    }

    void OnRingCollisionStart(Collider other)
    {
        Debug.Log("[Game] Ring collision started with: " + other.name);
    }

    void OnRingCollisionEnd(Collider other)
    {
        Debug.Log("[Game] Ring collision ended with: " + other.name);
    }

    void OnCollision()
    {
        if (ringRenderer != null)
        {
            ringRenderer.material.color = collisionColor;
        }
        if (trackRenderer != null)
        {
            trackRenderer.SetTrackColor(collisionColor);
        }
    }

    void OnNoCollision()
    {
        if (ringRenderer != null)
        {
            ringRenderer.material.color = normalColor;
        }
        if (trackRenderer != null)
        {
            trackRenderer.SetTrackColor(normalColor);
        }
    }

    void OnGameComplete()
    {
        isGameCompleted = true;
        
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
        }
        
        UpdateStatusText($"Complete!\nTime: {localElapsedTime:F1}s\nCollisions: {localCollisionCount}\n\nPress Trigger to restart");
        
        Debug.Log($"[Game] Completed! Time: {localElapsedTime:F2}s, Collisions: {localCollisionCount}");
    }

    void TriggerHapticFeedback()
    {
        if (xrController != null)
        {
            xrController.SendHapticImpulse(vibrationIntensity, vibrationDuration);
        }
    }

    bool GetTriggerPressed()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            return triggerAction.action.WasPressedThisFrame();
        }
        return UnityEngine.Input.GetKeyDown(KeyCode.Space);
    }

    IEnumerator ResetGame()
    {
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
        
        // 重置本地狀態
        isGameCompleted = false;
        isTimingStarted = false;
        localElapsedTime = 0f;
        localCollisionCount = 0;
        wasColliding = false;
        
        UpdateStatusText("Move ring to START zone!");
        
        yield return null;
    }

    void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
        Debug.Log("[Game] " + text);
    }
}
