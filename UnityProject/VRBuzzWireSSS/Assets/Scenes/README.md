# Scene 設定指南

## 基本場景設定

### 1. XR Origin

1. **GameObject → XR → XR Origin (VR)**
   
   這會自動創建：
   ```
   XR Origin
   ├── Camera Offset
   │   ├── Main Camera
   │   ├── Left Controller
   │   └── Right Controller
   └── XR Origin (Script)
   ```

2. **確認 Tracking Origin Mode**
   - XR Origin 組件 → Tracking Origin Mode: `Floor`

### 2. GameManager

創建空物件，添加所有管理腳本：

```
GameManager (Empty GameObject)
├── PythonClient (Component)
├── TrackRenderer (Component)
└── VRGameController (Component)
```

### 3. Ring (鐵環)

1. 創建 Ring 物件（參考 Prefabs/README.md）
2. 設為 Right Controller 的子物件
3. Position: (0, 0, 0.1) ← 稍微在手把前方

### 4. UI Canvas

1. **創建 Canvas**
   - GameObject → UI → Canvas
   - Render Mode: `World Space`
   - 位置: (0, 1.5, 2) ← 玩家前方
   - 縮放: (0.002, 0.002, 0.002)
   - Width: 400, Height: 300

2. **創建文字元素**
   ```
   Canvas
   ├── TimeText (上方)
   ├── CollisionText (上方，TimeText 旁邊)
   ├── StatusText (中間)
   └── GameCompletePanel (中間，預設隱藏)
       └── ResultText
   ```

---

## TODO 6: 螢幕同步顯示設定

### 目標

讓 VR 頭盔中的畫面同時顯示在電腦螢幕上，方便觀眾觀看或錄影。

### 方法 A: Spectator Camera（推薦）

創建一個獨立的攝影機，從不同角度觀看場景。

**Step 1: 創建 Spectator Camera**
```
Hierarchy:
└── Spectator Camera (新的 Camera)
```

**Step 2: 設定 Camera**
- Clear Flags: `Skybox`
- Culling Mask: `Everything`
- Target Display: `Display 1`（主螢幕）
- 位置: (0, 2, -1) ← 玩家後上方

**Step 3: 設定 VR Camera**
- Main Camera → Target Display: `Display 2`

**Step 4: 設定 Game View**
- 開啟第二個 Game View（Window → General → Game）
- 一個設為 Display 1（Spectator）
- 一個設為 Display 2（VR）

### 方法 B: Mirror Display（最簡單）

使用 Oculus 軟體內建的鏡像功能。

**設定方式：**
1. 開啟 Oculus 應用程式
2. Devices → Quest → Graphics Preferences
3. 確保 Mirror to Desktop 已開啟
4. Unity 執行時，Oculus 視窗會自動顯示 VR 畫面

**優點：**
- 無需額外設定
- 效能影響最小

**缺點：**
- 只能顯示頭盔看到的畫面
- 無法自訂視角

### 方法 C: Secondary Camera View

在 UI 上顯示第三人稱視角的小視窗。

**Step 1: 創建 Render Texture**
- Project → Create → Render Texture
- 命名為 `SpectatorRT`
- 大小: 512 x 512

**Step 2: 創建 Spectator Camera**
- 設定 Target Texture: `SpectatorRT`

**Step 3: 在 UI 顯示**
- Canvas 中創建 Raw Image
- 設定 Texture: `SpectatorRT`

### 方法 D: 使用 OVR Mirror

Oculus Integration 套件的功能。

**設定方式：**
1. 安裝 Oculus Integration（Asset Store）
2. 添加 OVRMirror 腳本到場景
3. 設定輸出到指定 Camera

---

## 完整場景層級結構

```
BuzzWireScene
│
├── XR Origin
│   └── Camera Offset
│       ├── Main Camera (VR)
│       ├── Left Controller
│       └── Right Controller
│           └── Ring
│               └── RingModel
│
├── GameManager
│   ├── PythonClient
│   ├── TrackRenderer
│   └── VRGameController
│
├── Spectator Camera (用於螢幕顯示)
│
├── Canvas (World Space)
│   ├── TimeText
│   ├── CollisionText
│   ├── StatusText
│   └── GameCompletePanel
│
├── Directional Light
│
└── Track (由 TrackRenderer 動態生成)
    ├── TrackLine
    ├── StartZone
    └── EndZone
```

---

## VRGameController 欄位設定

| 欄位 | 拖曳對象 |
|------|----------|
| Python Client | GameManager 上的 PythonClient |
| Track Renderer | GameManager 上的 TrackRenderer |
| Controller Node | Right Hand |
| Ring Transform | Ring 物件 |
| Ring Renderer | Ring 的 Mesh Renderer |
| Time Text | Canvas/TimeText |
| Collision Text | Canvas/CollisionText |
| Status Text | Canvas/StatusText |
| Game Complete Panel | Canvas/GameCompletePanel |
