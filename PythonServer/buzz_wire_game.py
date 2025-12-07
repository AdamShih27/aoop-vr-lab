"""
VR Buzz Wire Game Logic - Complete Version (Answer Key)
VR 電流急急棒遊戲邏輯 - 完整版（答案）

這個模組負責：
1. 定義金屬軌道的 3D 路徑
2. 計算手把位置與軌道的碰撞偵測
3. 追蹤遊戲狀態（計時、碰撞次數）
"""

import math
import time
from typing import List, Tuple, Dict, Optional


class BuzzWireGame:
    """電流急急棒遊戲邏輯類別"""
    
    def __init__(self, collision_threshold: float = 0.05):
        """
        初始化遊戲
        
        Args:
            collision_threshold: 碰撞判定閾值（單位：公尺）
                                手把與軌道距離小於此值視為碰撞
        """
        self.collision_threshold = collision_threshold
        
        # 軌道定義：一系列 3D 點 [x, y, z]
        # 軌道從起點到終點，玩家要控制鐵環沿著軌道移動
        self.track_points = self._define_track()
        
        # 起點和終點區域（用於判定遊戲開始/結束）
        self.start_zone = self.track_points[0]
        self.end_zone = self.track_points[-1]
        self.zone_radius = 0.1  # 起點/終點區域半徑
        
        # 遊戲狀態
        self.reset()
    
    def _define_track(self) -> List[List[float]]:
        """
        定義軌道路徑
        
        軌道是一系列 3D 點，相鄰點之間形成線段
        座標系統：Unity 座標（Y 軸向上）
        
        Returns:
            List[List[float]]: 軌道點列表 [[x1,y1,z1], [x2,y2,z2], ...]
        """
        # 設計一個有趣的 3D 軌道
        # 起點在左邊，終點在右邊，中間有起伏和轉彎
        track = [
            # 起點區域
            [-0.5, 1.0, 0.0],
            
            # 第一段：向右上延伸
            [-0.3, 1.1, 0.0],
            [-0.1, 1.2, 0.05],
            
            # 第二段：波浪形
            [0.0, 1.15, 0.1],
            [0.1, 1.25, 0.05],
            [0.2, 1.1, 0.0],
            
            # 第三段：向前彎曲
            [0.3, 1.15, -0.1],
            [0.4, 1.2, -0.15],
            
            # 第四段：螺旋上升
            [0.5, 1.3, -0.1],
            [0.55, 1.35, 0.0],
            [0.5, 1.4, 0.1],
            
            # 第五段：下降到終點
            [0.6, 1.3, 0.1],
            [0.7, 1.2, 0.05],
            
            # 終點區域
            [0.8, 1.1, 0.0],
        ]
        
        return track
    
    def reset(self):
        """重置遊戲狀態"""
        self.game_started = False
        self.game_completed = False
        self.timing_started = False  # 新增：是否開始計時
        self.start_time: Optional[float] = None
        self.elapsed_time: float = 0.0
        self.collision_count: int = 0
        self.is_colliding: bool = False
        self.last_collision_time: float = 0.0
        self.cooldown: float = 0.5  # 碰撞冷卻時間（秒），避免重複計數
    
    def start_game(self):
        """開始遊戲（進入準備狀態，等待玩家碰觸起點）"""
        self.reset()
        self.game_started = True
        # 注意：不再這裡開始計時，等碰到起點才計時
        self.timing_started = False
        self.start_time = None
    
    def update(self, positions) -> Dict:
        """
        更新遊戲狀態
        
        支援單點或多點偵測：
        - 單點：positions = [x, y, z]
        - 多點：positions = [[x,y,z], [x,y,z], ...]
        
        多點偵測時，任何一個點的碰撞都算碰撞
        
        遊戲流程：
        1. 按 Trigger 後 game_started=True，但 timing_started=False
        2. 碰到起點區域後 timing_started=True，開始計時
        3. 碰到終點區域後 game_completed=True，停止計時
        
        Args:
            positions: 手把位置，可以是單點 [x,y,z] 或多點 [[x,y,z], ...]
        
        Returns:
            Dict: 包含遊戲狀態的字典
        """
        if not self.game_started or self.game_completed:
            return self.get_state()
        
        # 標準化輸入：確保 positions 是點的列表
        if len(positions) > 0 and not isinstance(positions[0], list):
            # 單點格式 [x, y, z] -> [[x, y, z]]
            positions = [positions]
        
        # 用第一個點來判斷區域（起點/終點）
        first_point = positions[0] if positions else [0, 0, 0]
        
        # 檢查是否碰到起點區域（開始計時）
        if not self.timing_started:
            if self._is_in_zone(first_point, self.start_zone):
                self.timing_started = True
                self.start_time = time.time()
                print("[Game] Player touched start zone! Timer started.")
            # 還沒碰到起點，不計算碰撞，直接返回
            return self.get_state()
        
        # 更新經過時間
        if self.start_time:
            self.elapsed_time = time.time() - self.start_time
        
        # 多點碰撞偵測：計算所有點到軌道的最短距離，取最小值
        min_distance = float('inf')
        for point in positions:
            dist = self._calculate_min_distance_to_track(point)
            min_distance = min(min_distance, dist)
        
        # 碰撞偵測
        current_time = time.time()
        was_colliding = self.is_colliding
        self.is_colliding = min_distance < self.collision_threshold
        
        # 新的碰撞事件（有冷卻時間）
        if self.is_colliding and not was_colliding:
            if current_time - self.last_collision_time > self.cooldown:
                self.collision_count += 1
                self.last_collision_time = current_time
        
        # 檢查是否到達終點（用第一個點判斷）
        if self._is_in_zone(first_point, self.end_zone):
            self.game_completed = True
            print(f"[Game] Player reached end zone! Final time: {self.elapsed_time:.2f}s, Collisions: {self.collision_count}")
        
        return self.get_state(min_distance)
    
    def _calculate_min_distance_to_track(self, point: List[float]) -> float:
        """
        計算點到軌道的最短距離
        
        軌道由多條線段組成，計算點到每條線段的距離，取最小值
        
        Args:
            point: 3D 點座標 [x, y, z]
        
        Returns:
            float: 最短距離
        """
        min_distance = float('inf')
        
        # 遍歷所有線段
        for i in range(len(self.track_points) - 1):
            segment_start = self.track_points[i]
            segment_end = self.track_points[i + 1]
            
            distance = self._point_to_segment_distance(point, segment_start, segment_end)
            min_distance = min(min_distance, distance)
        
        return min_distance
    
    def _point_to_segment_distance(self, point: List[float], 
                                    seg_start: List[float], 
                                    seg_end: List[float]) -> float:
        """
        計算 3D 空間中點到線段的最短距離
        
        這是碰撞偵測的核心演算法：
        1. 將線段視為向量 v = seg_end - seg_start
        2. 將點到線段起點的向量 w = point - seg_start
        3. 計算投影參數 t = (w · v) / (v · v)
        4. t 限制在 [0, 1] 範圍內（確保在線段上）
        5. 計算最近點並求距離
        
        Args:
            point: 目標點 [x, y, z]
            seg_start: 線段起點 [x, y, z]
            seg_end: 線段終點 [x, y, z]
        
        Returns:
            float: 點到線段的最短距離
        """
        # 轉換為向量計算
        px, py, pz = point
        ax, ay, az = seg_start
        bx, by, bz = seg_end
        
        # 線段向量 v = B - A
        vx = bx - ax
        vy = by - ay
        vz = bz - az
        
        # 點到起點向量 w = P - A
        wx = px - ax
        wy = py - ay
        wz = pz - az
        
        # v · v (線段長度平方)
        v_dot_v = vx * vx + vy * vy + vz * vz
        
        # 處理線段長度為 0 的情況（起點終點重合）
        if v_dot_v < 1e-10:
            return math.sqrt(wx * wx + wy * wy + wz * wz)
        
        # w · v
        w_dot_v = wx * vx + wy * vy + wz * vz
        
        # 計算投影參數 t，並限制在 [0, 1]
        t = max(0.0, min(1.0, w_dot_v / v_dot_v))
        
        # 線段上最近點 = A + t * v
        closest_x = ax + t * vx
        closest_y = ay + t * vy
        closest_z = az + t * vz
        
        # 計算距離
        dx = px - closest_x
        dy = py - closest_y
        dz = pz - closest_z
        
        return math.sqrt(dx * dx + dy * dy + dz * dz)
    
    def _is_in_zone(self, position: List[float], zone_center: List[float]) -> bool:
        """檢查位置是否在指定區域內"""
        dx = position[0] - zone_center[0]
        dy = position[1] - zone_center[1]
        dz = position[2] - zone_center[2]
        distance = math.sqrt(dx * dx + dy * dy + dz * dz)
        return distance < self.zone_radius
    
    def get_track(self) -> Dict:
        """
        取得軌道資料（供 Unity 渲染用）
        
        Returns:
            Dict: 包含軌道點、起點、終點等資訊
        """
        return {
            "track_points": self.track_points,
            "start_zone": self.start_zone,
            "end_zone": self.end_zone,
            "zone_radius": self.zone_radius,
            "collision_threshold": self.collision_threshold
        }
    
    def get_state(self, distance: float = -1.0) -> Dict:
        """
        取得當前遊戲狀態
        
        Args:
            distance: 當前距離軌道的距離（-1 表示未計算）
        
        Returns:
            Dict: 遊戲狀態
        """
        return {
            "game_started": self.game_started,
            "game_completed": self.game_completed,
            "timing_started": self.timing_started,  # 新增
            "elapsed_time": round(self.elapsed_time, 2),
            "collision_count": self.collision_count,
            "is_colliding": self.is_colliding,
            "distance_to_track": round(distance, 4) if distance >= 0 else -1,
            "collision_threshold": self.collision_threshold
        }


# 測試用
if __name__ == "__main__":
    game = BuzzWireGame(collision_threshold=0.05)
    
    print("=== Track Points ===")
    for i, pt in enumerate(game.track_points):
        print(f"  {i}: {pt}")
    
    print("\n=== Testing Collision Detection ===")
    
    # 測試：點在軌道上
    test_point = [-0.4, 1.05, 0.0]
    game.start_game()
    state = game.update(test_point)
    print(f"Point {test_point}")
    print(f"  Distance: {state['distance_to_track']}")
    print(f"  Colliding: {state['is_colliding']}")
    
    # 測試：點遠離軌道
    test_point2 = [0.0, 0.5, 0.0]
    state2 = game.update(test_point2)
    print(f"Point {test_point2}")
    print(f"  Distance: {state2['distance_to_track']}")
    print(f"  Colliding: {state2['is_colliding']}")
