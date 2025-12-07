using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 遊戲狀態資料結構
/// 對應 Python Server 回傳的 JSON 格式
/// </summary>
[Serializable]
public class GameState
{
    // 遊戲狀態
    public bool game_started;
    public bool game_completed;
    public bool timing_started;  // 新增：是否已開始計時
    public float elapsed_time;
    public int collision_count;
    public bool is_colliding;
    public float distance_to_track;
    public float collision_threshold;

    // 軌道資料（從 /start 或 /track 取得）
    public List<List<float>> track_points;
    public List<float> start_zone;
    public List<float> end_zone;
    public float zone_radius;

    /// <summary>
    /// 從 JSON 字串解析 GameState
    /// </summary>
    public static GameState FromJson(string json)
    {
        GameState state = new GameState();

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Empty JSON received");
            return state;
        }

        try
        {
            // Debug: 輸出收到的 JSON（只顯示前500字）
            Debug.Log("[GameState] Parsing JSON: " + (json.Length > 500 ? json.Substring(0, 500) + "..." : json));

            // 解析基本欄位
            state.game_started = ParseBool(json, "game_started");
            state.game_completed = ParseBool(json, "game_completed");
            state.timing_started = ParseBool(json, "timing_started");
            state.elapsed_time = ParseFloat(json, "elapsed_time");
            state.collision_count = ParseInt(json, "collision_count");
            state.is_colliding = ParseBool(json, "is_colliding");
            state.distance_to_track = ParseFloat(json, "distance_to_track");
            state.collision_threshold = ParseFloat(json, "collision_threshold");
            state.zone_radius = ParseFloat(json, "zone_radius");

            // 解析軌道點
            state.track_points = ParseNestedFloatArray(json, "track_points");
            if (state.track_points != null)
            {
                Debug.Log($"[GameState] Parsed {state.track_points.Count} track points");
            }

            // 解析區域
            state.start_zone = ParseFloatArray(json, "start_zone");
            state.end_zone = ParseFloatArray(json, "end_zone");
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Parse Error: " + e.Message + "\n" + e.StackTrace);
        }

        return state;
    }

    /// <summary>
    /// 解析嵌套的浮點數陣列 [[x,y,z], [x,y,z], ...]
    /// </summary>
    private static List<List<float>> ParseNestedFloatArray(string json, string key)
    {
        List<List<float>> result = new List<List<float>>();

        try
        {
            // 找到 key
            string pattern = "\"" + key + "\"\\s*:\\s*\\[";
            Match match = Regex.Match(json, pattern);
            if (!match.Success)
            {
                Debug.LogWarning($"[GameState] Key '{key}' not found in JSON");
                return result;
            }

            int startIdx = match.Index + match.Length;
            
            // 找到對應的結束括號
            int depth = 1;
            int endIdx = startIdx;
            while (endIdx < json.Length && depth > 0)
            {
                if (json[endIdx] == '[') depth++;
                else if (json[endIdx] == ']') depth--;
                endIdx++;
            }

            // 提取陣列內容
            string arrayContent = json.Substring(startIdx, endIdx - startIdx - 1);
            
            // 使用正則表達式找所有的 [x, y, z] 格式
            MatchCollection pointMatches = Regex.Matches(arrayContent, @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]");
            
            foreach (Match pointMatch in pointMatches)
            {
                List<float> point = new List<float>();
                point.Add(float.Parse(pointMatch.Groups[1].Value));
                point.Add(float.Parse(pointMatch.Groups[2].Value));
                point.Add(float.Parse(pointMatch.Groups[3].Value));
                result.Add(point);
            }

            Debug.Log($"[GameState] ParseNestedFloatArray found {result.Count} points for '{key}'");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameState] Error parsing nested array '{key}': {e.Message}");
        }

        return result;
    }

    /// <summary>
    /// 解析簡單的浮點數陣列 [x, y, z]
    /// </summary>
    private static List<float> ParseFloatArray(string json, string key)
    {
        List<float> result = new List<float>();

        try
        {
            string pattern = "\"" + key + "\"\\s*:\\s*\\[\\s*(-?\\d+\\.?\\d*)\\s*,\\s*(-?\\d+\\.?\\d*)\\s*,\\s*(-?\\d+\\.?\\d*)\\s*\\]";
            Match match = Regex.Match(json, pattern);
            
            if (match.Success)
            {
                result.Add(float.Parse(match.Groups[1].Value));
                result.Add(float.Parse(match.Groups[2].Value));
                result.Add(float.Parse(match.Groups[3].Value));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameState] Error parsing float array '{key}': {e.Message}");
        }

        return result;
    }

    private static bool ParseBool(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*(true|false)";
        Match match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return match.Success && match.Groups[1].Value.ToLower() == "true";
    }

    private static int ParseInt(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*(-?\\d+)";
        Match match = Regex.Match(json, pattern);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
        {
            return result;
        }
        return 0;
    }

    private static float ParseFloat(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*(-?\\d+\\.?\\d*)";
        Match match = Regex.Match(json, pattern);
        if (match.Success && float.TryParse(match.Groups[1].Value, out float result))
        {
            return result;
        }
        return 0f;
    }
}
