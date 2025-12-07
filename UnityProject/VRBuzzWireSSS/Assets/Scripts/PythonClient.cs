using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Python Server 通訊客戶端
/// 負責所有與 Python Server 的 HTTP 通訊
/// 
/// 教學重點：
/// 1. HTTP POST/GET 請求
/// 2. JSON 資料格式
/// 3. Unity Coroutine 非同步處理
/// 4. 回呼函式（Callback）模式
/// </summary>
public class PythonClient : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://localhost:5000";
    [SerializeField] private float timeout = 5f;

    /// <summary>
    /// 健康檢查 - 確認 Server 是否在線
    /// GET /health
    /// </summary>
    public IEnumerator CheckHealth(Action<bool> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/health"))
        {
            request.timeout = (int)timeout;
            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;
            onComplete?.Invoke(success);
        }
    }

    /// <summary>
    /// 開始新遊戲
    /// POST /start -> 回傳初始狀態 + 軌道資料
    /// </summary>
    public IEnumerator StartGame(float collisionThreshold, Action<GameState> onSuccess, Action<string> onError)
    {
        string jsonBody = "{\"collision_threshold\": " + collisionThreshold + "}";

        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/start", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 發送手把位置更新
    /// POST /update with {position: [x, y, z]} -> 回傳碰撞結果
    /// </summary>
    public IEnumerator SendUpdate(Vector3 position, Action<GameState> onSuccess, Action<string> onError)
    {
        // 將 Vector3 轉換成 JSON 格式
        string jsonBody = "{\"position\": [" + 
            position.x.ToString("F4") + ", " + 
            position.y.ToString("F4") + ", " + 
            position.z.ToString("F4") + "]}";

        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/update", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 發送多個偵測點位置更新（用於圓環邊緣多點碰撞偵測）
    /// POST /update with {positions: [[x,y,z], [x,y,z], ...]} -> 回傳碰撞結果
    /// </summary>
    public IEnumerator SendUpdateMultiplePoints(System.Collections.Generic.List<Vector3> positions, Action<GameState> onSuccess, Action<string> onError)
    {
        // 將多個 Vector3 轉換成 JSON 格式
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"positions\": [");
        
        for (int i = 0; i < positions.Count; i++)
        {
            sb.Append("[");
            sb.Append(positions[i].x.ToString("F4"));
            sb.Append(", ");
            sb.Append(positions[i].y.ToString("F4"));
            sb.Append(", ");
            sb.Append(positions[i].z.ToString("F4"));
            sb.Append("]");
            
            if (i < positions.Count - 1)
                sb.Append(", ");
        }
        
        sb.Append("]}");
        string jsonBody = sb.ToString();

        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/update", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 取得軌道資料
    /// GET /track -> 回傳軌道點列表
    /// </summary>
    public IEnumerator GetTrack(Action<GameState> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/track"))
        {
            request.timeout = (int)timeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 取得當前遊戲狀態
    /// GET /state
    /// </summary>
    public IEnumerator GetState(Action<GameState> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/state"))
        {
            request.timeout = (int)timeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 重置遊戲
    /// POST /reset
    /// </summary>
    public IEnumerator ResetGame(Action<GameState> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/reset", "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GameState state = GameState.FromJson(request.downloadHandler.text);
                    onSuccess?.Invoke(state);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON Parse Error: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke(GetErrorMessage(request));
            }
        }
    }

    /// <summary>
    /// 產生錯誤訊息
    /// </summary>
    private string GetErrorMessage(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            return "Connection Error: Cannot connect to " + serverUrl + 
                   "\nMake sure Python server is running!";
        }
        else if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            return "Server Error: " + request.responseCode + " - " + request.error;
        }
        else
        {
            return "Error: " + request.error;
        }
    }
}
