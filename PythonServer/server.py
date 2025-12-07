"""
VR Buzz Wire Game Server - Complete Version
VR 電流急急棒遊戲伺服器 - 完整版

這個伺服器負責：
1. 接收 Unity VR 端的手把位置
2. 進行碰撞偵測
3. 回傳遊戲狀態（是否碰撞、時間、碰撞次數）
"""

from flask import Flask, request, jsonify
from flask_cors import CORS

# 使用完整版進行測試
from buzz_wire_game import BuzzWireGame
# from buzz_wire_game_lab import BuzzWireGame  # 學生版

app = Flask(__name__)
CORS(app)

# 遊戲實例
game = None


@app.route('/start', methods=['POST'])
def start_game():
    """
    開始新遊戲
    
    Returns:
        JSON: 初始遊戲狀態 + 軌道資料
    """
    global game
    
    try:
        # 取得可選的參數
        data = request.get_json() or {}
        threshold = data.get('collision_threshold', 0.05)
        
        # 創建新遊戲
        game = BuzzWireGame(collision_threshold=threshold)
        game.start_game()
        
        # 回傳初始狀態和軌道資料
        state = game.get_state()
        track = game.get_track()
        
        response = {
            **state,
            **track
        }
        
        print(f"[Server] Game started! Threshold: {threshold}")
        print(f"[Server] Track points count: {len(track.get('track_points', []))}")
        
        return jsonify(response)
    
    except Exception as e:
        print(f"[Server] Error in start_game: {e}")
        import traceback
        traceback.print_exc()
        return jsonify({"error": str(e)}), 500


@app.route('/update', methods=['POST'])
def update_game():
    """
    更新遊戲狀態
    
    接收手把位置，回傳碰撞偵測結果和遊戲狀態
    
    支援兩種格式：
    1. 單點：{"position": [x, y, z]}
    2. 多點：{"positions": [[x,y,z], [x,y,z], ...]}
    
    Returns:
        JSON: 遊戲狀態（碰撞、時間、次數等）
    """
    global game
    
    try:
        if game is None:
            return jsonify({"error": "Game not started"}), 400
        
        # 取得手把位置
        data = request.get_json()
        if data is None:
            return jsonify({"error": "No JSON data received"}), 400
        
        # 支援兩種格式：position（單點）或 positions（多點）
        if 'positions' in data:
            # 多點格式
            positions = data.get('positions', [[0, 0, 0]])
        else:
            # 單點格式，轉換成多點
            position = data.get('position', [0, 0, 0])
            if not isinstance(position, list) or len(position) != 3:
                position = [0, 0, 0]
            positions = [position]
        
        # 更新遊戲狀態
        state = game.update(positions)
        
        # 如果發生碰撞，輸出提示
        if state and state.get('is_colliding'):
            print(f"[Server] COLLISION! Count: {state['collision_count']}, Distance: {state['distance_to_track']}")
        
        return jsonify(state)
    
    except Exception as e:
        print(f"[Server] Error in update_game: {e}")
        import traceback
        traceback.print_exc()
        return jsonify({"error": str(e)}), 500


@app.route('/track', methods=['GET'])
def get_track():
    """
    取得軌道資料
    
    Unity 可以用這個 API 來獲取軌道點，進行渲染
    
    Returns:
        JSON: 軌道點列表和相關參數
    """
    global game
    
    if game is None:
        # 如果遊戲還沒開始，創建一個臨時的來取得軌道
        temp_game = BuzzWireGame()
        return jsonify(temp_game.get_track())
    
    return jsonify(game.get_track())


@app.route('/state', methods=['GET'])
def get_state():
    """
    取得當前遊戲狀態
    
    Returns:
        JSON: 當前遊戲狀態
    """
    global game
    
    if game is None:
        return jsonify({"error": "Game not started"}), 400
    
    return jsonify(game.get_state())


@app.route('/reset', methods=['POST'])
def reset_game():
    """
    重置遊戲（不需要重新取得軌道）
    
    Returns:
        JSON: 重置後的遊戲狀態
    """
    global game
    
    if game is None:
        return jsonify({"error": "Game not started"}), 400
    
    game.start_game()
    return jsonify(game.get_state())


@app.route('/health', methods=['GET'])
def health_check():
    """健康檢查"""
    return jsonify({
        "status": "ok",
        "message": "VR Buzz Wire Game Server is running!"
    })


if __name__ == '__main__':
    print("=" * 55)
    print("  VR Buzz Wire Game Server")
    print("  VR 電流急急棒遊戲伺服器")
    print("  http://localhost:5000")
    print("=" * 55)
    print("\nEndpoints:")
    print("  POST /start     - 開始新遊戲，回傳軌道資料")
    print("  POST /update    - 發送手把位置，回傳碰撞結果")
    print("  GET  /track     - 取得軌道資料")
    print("  GET  /state     - 取得當前遊戲狀態")
    print("  POST /reset     - 重置遊戲")
    print("  GET  /health    - 健康檢查")
    print("\n等待 Unity VR 客戶端連接...\n")
    
    app.run(host='0.0.0.0', port=5000, debug=True)
