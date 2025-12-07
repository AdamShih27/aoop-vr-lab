using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 軌道渲染器
/// 負責根據 Python Server 傳來的軌道點繪製 3D 軌道
/// 並為每個線段創建 Collider 供物理碰撞偵測
/// </summary>
public class TrackRenderer : MonoBehaviour
{
    [Header("Track Appearance")]
    [SerializeField] private float wireRadius = 0.008f;  // 金屬線半徑
    [SerializeField] private Material wireMaterial;       // 金屬線材質
    [SerializeField] private Color wireColor = new Color(0.7f, 0.7f, 0.7f);  // 銀色
    
    [Header("Collision")]
    [Tooltip("是否為軌道創建 Collider（用於 Unity 物理碰撞偵測）")]
    [SerializeField] private bool createColliders = true;
    [Tooltip("Collider 半徑倍數（相對於 wireRadius）")]
    [SerializeField] private float colliderRadiusMultiplier = 1.5f;
    
    [Header("Zone Markers")]
    [SerializeField] private GameObject startMarkerPrefab;  // 起點標記
    [SerializeField] private GameObject endMarkerPrefab;    // 終點標記
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool showDebugSpheres = false;
    
    // 私有變數
    private List<GameObject> trackSegments = new List<GameObject>();
    private List<GameObject> trackColliders = new List<GameObject>();
    private GameObject startMarker;
    private GameObject endMarker;
    private LineRenderer lineRenderer;

    void Awake()
    {
        // 確保 "Track" Tag 存在
        // 注意：需要在 Unity Editor 中手動創建這個 Tag
    }

    /// <summary>
    /// 根據軌道點列表繪製軌道
    /// </summary>
    public void RenderTrack(List<List<float>> trackPoints, List<float> startZone, 
                            List<float> endZone, float zoneRadius)
    {
        // 清除舊的軌道
        ClearTrack();

        if (trackPoints == null || trackPoints.Count < 2)
        {
            Debug.LogError("Track points invalid!");
            return;
        }

        // 使用 LineRenderer 繪製視覺效果
        CreateLineRenderer(trackPoints);
        
        // 創建 Collider（用於物理碰撞偵測）
        if (createColliders)
        {
            CreateTrackColliders(trackPoints);
        }

        // 創建起點和終點標記
        if (startZone != null && startZone.Count >= 3)
        {
            CreateZoneMarker(startZone, zoneRadius, startColor, true);
        }
        
        if (endZone != null && endZone.Count >= 3)
        {
            CreateZoneMarker(endZone, zoneRadius, endColor, false);
        }

        // Debug: 顯示每個軌道點
        if (showDebugSpheres)
        {
            CreateDebugSpheres(trackPoints);
        }

        Debug.Log($"Track rendered with {trackPoints.Count} points, Colliders: {createColliders}");
    }

    /// <summary>
    /// 為軌道創建 Collider
    /// 每個線段使用一個 Capsule Collider
    /// </summary>
    private void CreateTrackColliders(List<List<float>> trackPoints)
    {
        GameObject collidersParent = new GameObject("TrackColliders");
        collidersParent.transform.SetParent(transform);
        
        float colliderRadius = wireRadius * colliderRadiusMultiplier;

        for (int i = 0; i < trackPoints.Count - 1; i++)
        {
            Vector3 start = new Vector3(
                trackPoints[i][0],
                trackPoints[i][1],
                trackPoints[i][2]
            );
            
            Vector3 end = new Vector3(
                trackPoints[i + 1][0],
                trackPoints[i + 1][1],
                trackPoints[i + 1][2]
            );

            // 創建 Capsule Collider 線段
            GameObject segment = CreateCapsuleCollider(start, end, colliderRadius, i);
            segment.transform.SetParent(collidersParent.transform);
            trackColliders.Add(segment);
        }
        
        trackSegments.Add(collidersParent);
        Debug.Log($"[TrackRenderer] Created {trackColliders.Count} track colliders");
    }

    /// <summary>
    /// 在兩點之間創建 Capsule Collider
    /// </summary>
    private GameObject CreateCapsuleCollider(Vector3 start, Vector3 end, float radius, int index)
    {
        GameObject capsuleObj = new GameObject($"TrackSegment_{index}");
        
        // 設定 Tag（需要先在 Unity 中創建 "Track" Tag）
        try
        {
            capsuleObj.tag = "Track";
        }
        catch
        {
            Debug.LogWarning("Tag 'Track' not found! Please create it in Unity: Edit > Project Settings > Tags and Layers");
        }
        
        // 計算位置和方向
        Vector3 direction = end - start;
        float length = direction.magnitude;
        Vector3 midPoint = (start + end) / 2f;

        capsuleObj.transform.position = midPoint;
        
        // Capsule 預設是 Y 軸方向，需要旋轉到正確方向
        if (direction != Vector3.zero)
        {
            capsuleObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        // 添加 Capsule Collider
        CapsuleCollider capsule = capsuleObj.AddComponent<CapsuleCollider>();
        capsule.radius = radius;
        capsule.height = length + radius * 2; // 加上兩端的半球
        capsule.direction = 1; // Y 軸方向
        capsule.isTrigger = true; // 設為 Trigger，不會產生物理推力

        return capsuleObj;
    }

    /// <summary>
    /// 使用 LineRenderer 繪製軌道
    /// </summary>
    private void CreateLineRenderer(List<List<float>> trackPoints)
    {
        GameObject lineObj = new GameObject("TrackLine");
        lineObj.transform.SetParent(transform);
        
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.positionCount = trackPoints.Count;
        
        // 設定外觀
        lineRenderer.startWidth = wireRadius * 2;
        lineRenderer.endWidth = wireRadius * 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

        // 設定材質
        if (wireMaterial != null)
        {
            lineRenderer.material = wireMaterial;
        }
        else
        {
            // 使用預設材質
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = wireColor;
            lineRenderer.endColor = wireColor;
        }

        // 設定位置
        for (int i = 0; i < trackPoints.Count; i++)
        {
            Vector3 pos = new Vector3(
                trackPoints[i][0],
                trackPoints[i][1],
                trackPoints[i][2]
            );
            lineRenderer.SetPosition(i, pos);
        }

        trackSegments.Add(lineObj);
    }

    /// <summary>
    /// 創建起點/終點區域標記
    /// </summary>
    private void CreateZoneMarker(List<float> position, float radius, Color color, bool isStart)
    {
        Vector3 pos = new Vector3(position[0], position[1], position[2]);
        
        GameObject marker;
        if (isStart && startMarkerPrefab != null)
        {
            marker = Instantiate(startMarkerPrefab, pos, Quaternion.identity);
        }
        else if (!isStart && endMarkerPrefab != null)
        {
            marker = Instantiate(endMarkerPrefab, pos, Quaternion.identity);
        }
        else
        {
            // 預設：創建透明球體
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(marker.GetComponent<Collider>());
            marker.transform.position = pos;
            marker.transform.localScale = Vector3.one * radius * 0.5f;  // 縮小標記
            
            Renderer renderer = marker.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(color.r, color.g, color.b, 0.5f);
            
            // 設定透明度
            mat.SetFloat("_Mode", 3);  // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            renderer.material = mat;
        }

        marker.name = isStart ? "StartZone" : "EndZone";
        marker.transform.SetParent(transform);

        if (isStart)
            startMarker = marker;
        else
            endMarker = marker;
    }

    /// <summary>
    /// 創建除錯用的球體標記
    /// </summary>
    private void CreateDebugSpheres(List<List<float>> trackPoints)
    {
        for (int i = 0; i < trackPoints.Count; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            
            sphere.transform.position = new Vector3(
                trackPoints[i][0],
                trackPoints[i][1],
                trackPoints[i][2]
            );
            sphere.transform.localScale = Vector3.one * 0.02f;
            sphere.transform.SetParent(transform);
            sphere.name = $"TrackPoint_{i}";
            
            // 顏色：起點綠、終點紅、中間白
            Color c = Color.white;
            if (i == 0) c = Color.green;
            else if (i == trackPoints.Count - 1) c = Color.red;
            sphere.GetComponent<Renderer>().material.color = c;
            
            trackSegments.Add(sphere);
        }
    }

    /// <summary>
    /// 清除所有軌道物件
    /// </summary>
    public void ClearTrack()
    {
        foreach (var obj in trackSegments)
        {
            if (obj != null)
                Destroy(obj);
        }
        trackSegments.Clear();
        trackColliders.Clear();

        if (startMarker != null)
        {
            Destroy(startMarker);
            startMarker = null;
        }

        if (endMarker != null)
        {
            Destroy(endMarker);
            endMarker = null;
        }
        
        lineRenderer = null;
    }

    /// <summary>
    /// 更新軌道顏色（例如碰撞時變紅）
    /// </summary>
    public void SetTrackColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
