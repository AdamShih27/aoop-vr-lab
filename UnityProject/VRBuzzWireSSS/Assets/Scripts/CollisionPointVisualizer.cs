using UnityEngine;

/// <summary>
/// 碰撞偵測點可視化工具
/// 在 Scene 視窗和 Game 視窗中顯示偵測點位置
/// </summary>
public class CollisionPointVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("球體大小")]
    [SerializeField] private float sphereSize = 0.015f;
    
    [Tooltip("正常狀態顏色")]
    [SerializeField] private Color normalColor = Color.yellow;
    
    [Tooltip("碰撞狀態顏色")]
    [SerializeField] private Color collisionColor = Color.red;
    
    [Tooltip("是否在 Game 視窗中顯示")]
    [SerializeField] private bool showInGame = true;
    
    [Header("References")]
    [Tooltip("碰撞偵測點列表")]
    [SerializeField] private Transform[] collisionPoints;
    
    // 當前是否碰撞（可從外部設定）
    private bool isColliding = false;
    
    // 用於 Game 視窗顯示的球體
    private GameObject[] debugSpheres;
    private Material debugMaterial;

    void Start()
    {
        if (showInGame && collisionPoints != null && collisionPoints.Length > 0)
        {
            CreateDebugSpheres();
        }
    }

    void CreateDebugSpheres()
    {
        debugSpheres = new GameObject[collisionPoints.Length];
        
        // 創建共用材質
        debugMaterial = new Material(Shader.Find("Standard"));
        debugMaterial.color = normalColor;
        
        for (int i = 0; i < collisionPoints.Length; i++)
        {
            if (collisionPoints[i] == null) continue;
            
            // 創建球體
            debugSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSpheres[i].name = "DebugSphere_" + i;
            debugSpheres[i].transform.SetParent(collisionPoints[i]);
            debugSpheres[i].transform.localPosition = Vector3.zero;
            debugSpheres[i].transform.localScale = Vector3.one * sphereSize;
            
            // 移除 Collider（避免影響物理）
            Destroy(debugSpheres[i].GetComponent<Collider>());
            
            // 設定材質
            debugSpheres[i].GetComponent<Renderer>().material = debugMaterial;
        }
    }

    void Update()
    {
        if (debugMaterial != null)
        {
            debugMaterial.color = isColliding ? collisionColor : normalColor;
        }
    }

    /// <summary>
    /// 設定碰撞狀態（用於變色）
    /// </summary>
    public void SetCollisionState(bool colliding)
    {
        isColliding = colliding;
    }

    /// <summary>
    /// 在 Scene 視窗中繪製 Gizmos
    /// </summary>
    void OnDrawGizmos()
    {
        if (collisionPoints == null) return;
        
        Gizmos.color = isColliding ? collisionColor : normalColor;
        
        foreach (var point in collisionPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, sphereSize);
            }
        }
        
        // 繪製連線（顯示圓環形狀）
        Gizmos.color = Color.cyan;
        for (int i = 0; i < collisionPoints.Length; i++)
        {
            if (collisionPoints[i] == null) continue;
            
            int nextIndex = (i + 1) % collisionPoints.Length;
            if (collisionPoints[nextIndex] != null)
            {
                Gizmos.DrawLine(collisionPoints[i].position, collisionPoints[nextIndex].position);
            }
        }
    }

    /// <summary>
    /// 選中時也繪製（更明顯）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        OnDrawGizmos();
    }
}
