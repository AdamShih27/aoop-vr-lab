using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 圓環碰撞偵測器
/// 使用 Unity 物理引擎偵測圓環與軌道的碰撞
/// 
/// 設定方式：
/// 1. 將此腳本加到 Torus 物件上
/// 2. Torus 需要有 Mesh Collider，並勾選 "Is Trigger" 和 "Convex"
/// 3. Torus 需要有 Rigidbody，勾選 "Is Kinematic"
/// </summary>
public class RingCollisionDetector : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("碰撞開始時觸發")]
    public UnityEvent<Collider> OnCollisionStart;
    
    [Tooltip("碰撞結束時觸發")]
    public UnityEvent<Collider> OnCollisionEnd;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    
    // 當前是否在碰撞中
    public bool IsColliding { get; private set; }
    
    // 碰撞的物件
    private Collider currentCollider;

    void Start()
    {
        // 確保有必要的組件
        EnsureComponents();
    }

    void EnsureComponents()
    {
        // 檢查 Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // 嘗試加 Mesh Collider
            MeshCollider meshCol = gameObject.AddComponent<MeshCollider>();
            meshCol.convex = true;
            meshCol.isTrigger = true;
            Debug.Log("[RingCollisionDetector] Added Mesh Collider");
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log("[RingCollisionDetector] Set Collider to Trigger");
        }

        // 檢查 Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log("[RingCollisionDetector] Added Kinematic Rigidbody");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 只偵測軌道（Tag 為 "Track"）
        if (other.CompareTag("Track"))
        {
            IsColliding = true;
            currentCollider = other;
            
            if (showDebugLog)
            {
                Debug.Log("[Ring] Collision START with: " + other.name);
            }
            
            OnCollisionStart?.Invoke(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Track"))
        {
            IsColliding = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Track"))
        {
            IsColliding = false;
            currentCollider = null;
            
            if (showDebugLog)
            {
                Debug.Log("[Ring] Collision END with: " + other.name);
            }
            
            OnCollisionEnd?.Invoke(other);
        }
    }
}
