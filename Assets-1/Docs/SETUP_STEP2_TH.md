# Midnight School — คู่มือ Step 2 (ภาษาไทย)

ระบบมินิเกมและ UI Interface — Lockpick, Struggle, Wire + Trigger ในโลกจริง

---

## สิ่งที่ได้ใน Step 2

| ระบบ | รายละเอียด |
|------|------------|
| **Lockpick** | กด ↑ ค้างดันกิ๊ฟผม — หลีกขอบซ้าย-ขวา, ถึงเป้าหมายด้านบน = สำเร็จ |
| **Struggle** | กด Q/E สลับกัน — รักษาเกจใน Sweet Spot ครบ 5 วิ = สำเร็จ |
| **Wire** | ลากเมาส์จากจุดซ้ายไปจุดขวาสีเดียวกัน — ครบ 4 เส้น = สำเร็จ |
| **MinigameManager** | เปิด/ปิด UI, ล็อกผู้เล่น, ปลดล็อกเมาส์ |
| **MinigameTrigger** | เดินชน Trigger → เปิดมินิเกม → สำเร็จแล้วเรียก Event เปิดประตู |

---

## ขั้นตอนเริ่มต้น (แนะนำ One-Click)

### 1. เปิดโปรเจกต์และ Scene

1. เปิด **Unity Hub** → โปรเจกต์ **`My project (3)`**
2. เปิด Scene **`Assets/Scenes/SampleScene`**
3. ถ้ายังไม่มี Player → รัน **Midnight School > Setup Step 1** ก่อน

### 2. สร้าง UI อัตโนมัติ

1. คลิกเมนู **Midnight School > Setup Step 2 (Minigame UI + Triggers)**
2. กด **Setup** ใน Dialog

สคริปต์จะสร้างให้:
- `MinigameCanvas` + EventSystem
- Panel ทั้ง 3 ชนิด พร้อม wire references
- `MinigameManager` เชื่อม Player
- Trigger ทดสอบ 3 ก้อน (Lockpick / Struggle / Wire)
- ตั้ง Tag **Player** ให้ Player อัตโนมัติ

### 3. ทดสอบ

1. กด **Play**
2. เดินเข้า **กล่องสีฟ้า** ในฉาก
3. มินิเกมจะเปิด — เมาส์ปลดล็อก, ตัวละครหยุดเดิน
4. ทำมินิเกมให้สำเร็จ → UI ปิด, เมาส์ล็อกกลับ

---

## โครงสร้าง Hierarchy หลัง Setup

```
Scene
├── Player                          ← Tag: Player, FirstPersonController
├── MinigameCanvas                  ← Canvas + GraphicRaycaster
│   ├── LockpickPanel               ← LockpickMinigame (ปิดอยู่ตอนเริ่ม)
│   │   ├── LockBackground
│   │   ├── LeftBoundary / RightBoundary
│   │   ├── TargetZone
│   │   ├── StartAnchor
│   │   └── HairClip
│   ├── StrugglePanel               ← StruggleMinigame
│   │   └── StruggleSlider
│   │       └── SweetSpot
│   └── WirePanel                   ← WireMinigame
│       ├── Source_Red / Blue / Green / Yellow
│       ├── Target_* (สลับตำแหน่งสี)
│       ├── DragLine
│       └── ConnectionRoot
├── MinigameManager                 ← MinigameManager
├── EventSystem
├── Trigger_Lockpick                ← MinigameTrigger
├── Trigger_Struggle
└── Trigger_Wire
```

---

## วิธีจัด UI ด้วยมือ (Step-by-Step มือใหม่)

ถ้าต้องการสร้าง/แก้ UI เอง แทนการใช้ One-Click Setup:

### A. Canvas หลัก

1. **Hierarchy** → คลิกขวา → **UI > Canvas**
2. ตั้งชื่อ **`MinigameCanvas`**
3. ใน Inspector:
   - **Canvas** → Render Mode: **Screen Space - Overlay**
   - **Canvas Scaler** → UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**
   - Match: **0.5**
4. ตรวจว่ามี **EventSystem** ใน Scene (Unity สร้างให้อัตโนมัติเมื่อสร้าง UI แรก)

---

### B. มินิเกม Lockpick (งัดกุญแจ)

#### B.1 สร้าง Panel

1. คลิกขวาที่ `MinigameCanvas` → **UI > Panel**
2. ตั้งชื่อ **`LockpickPanel`**
3. Rect Transform: Anchor **stretch-stretch** (เต็มจอ)
4. Image สีดำ Alpha ~**190** (พื้นหลังมืด)

#### B.2 องค์ประกอบภายใน

| Object | ประเภท | Anchored Position | Size (W×H) | หมายเหตุ |
|--------|--------|-------------------|------------|----------|
| LockBackground | Image | (0, 40) | 280 × 420 | ภาพแม่กุญแจ (ใส่ Sprite ทีหลังได้) |
| LeftBoundary | Empty/Image | (-70, 40) | 10 × 400 | ขอบซ้าย — Image Alpha 0 ได้ |
| RightBoundary | Empty/Image | (70, 40) | 10 × 400 | ขอบขวา |
| TargetZone | Image | (0, 220) | 80 × 40 | โซนเป้าหมายด้านบน — สีเขียวโปร่ง |
| StartAnchor | Empty | (0, -160) | 20 × 20 | จุดเริ่มกิ๊ฟผม |
| HairClip | Image | (0, -160) | 18 × 140 | ภาพกิ๊ฟผม — Pivot (0.5, 0) แนะนำ |

#### B.3 แนบสคริปต์

1. เลือก **LockpickPanel**
2. **Add Component** → **Lockpick Minigame**
3. ลาก reference ตามตารางใน Inspector:

| Field | ลากจาก |
|-------|--------|
| Panel Root | LockpickPanel |
| Hair Clip Rect | HairClip |
| Start Anchor | StartAnchor |
| Left Boundary | LeftBoundary |
| Right Boundary | RightBoundary |
| Target Zone | TargetZone |

4. **ปิด** LockpickPanel (uncheck ที่ Inspector บนสุด) — เปิดเมื่อเล่นมินิเกมเท่านั้น

#### B.4 ปุ่มและพฤติกรรม

| ปุ่ม | การทำงาน |
|------|----------|
| **↑ (Arrow Up)** ค้าง | ดันกิ๊ฟผมขึ้น |
| ชนขอบซ้าย/ขวา | กิ๊ฟเด้งกลับจุดเริ่ม |
| ถึง TargetZone | สำเร็จ → Event เปิดประตู |

---

### C. มินิเกม Struggle (ดิ้นรน Q/E)

#### C.1 สร้าง Panel

1. **UI > Panel** ใต้ Canvas → ชื่อ **`StrugglePanel`**
2. ตั้งค่าเหมือน LockpickPanel (เต็มจอ, พื้นมืด)

#### C.2 Slider + Sweet Spot

1. คลิกขวา StrugglePanel → **UI > Slider**
2. ตั้งชื่อ **`StruggleSlider`**
3. Rect Transform: Pos **(0, 0)**, Size **600 × 40**
4. Slider component:
   - Min Value: **0**
   - Max Value: **1**
   - Value: **0.5**
   - **Interactable: ปิด** (สคริปต์ควบคุมเอง)

5. สร้าง **SweetSpot** เป็น child ของ Slider:
   - **UI > Image** ใต้ StruggleSlider
   - Size **80 × 50**, Pos **(0, 0)** ตรงกลาง Track
   - สีเขียวโปร่งใส — **Raycast Target: ปิด**

#### C.3 แนบสคริปต์

1. Add **Struggle Minigame** ที่ StrugglePanel
2. Wire references:

| Field | ลากจาก |
|-------|--------|
| Panel Root | StrugglePanel |
| Gauge Slider | StruggleSlider |
| Sweet Spot Rect | SweetSpot |
| Slider Track Rect | Background (child ของ Slider) |

3. ตั้งค่าใน Inspector:
   - Decay Rate: **0.15**
   - Mash Boost: **0.12**
   - Required Hold Time: **5** วินาที

#### C.4 ปุ่ม

| ปุ่ม | การทำงาน |
|------|----------|
| **Q** แล้ว **E** สลับ | เพิ่มค่าเกจ |
| เกจอยู่ใน Sweet Spot ครบ 5 วิ | สำเร็จ |
| เกจ = 0 | ล้มเหลว (ถ้า Fail On Empty เปิด) |

---

### D. มินิเกม Wire (ต่อสายไฟ)

#### D.1 สร้าง Panel

1. **UI > Panel** → **`WirePanel`**

#### D.2 จุดเชื่อมสาย (WireNode)

สร้าง **UI > Image** วงกลม 4 จุดฝั่งซ้าย:

| ชื่อ | Position | Size | Wire Color | Side |
|------|----------|------|------------|------|
| Source_Red | (-320, 120) | 48×48 | Red | Source |
| Source_Blue | (-320, 40) | 48×48 | Blue | Source |
| Source_Green | (-320, -40) | 48×48 | Green | Source |
| Source_Yellow | (-320, -120) | 48×48 | Yellow | Source |

สร้าง 4 จุดฝั่งขวา (**สลับตำแหน่งสี** เพื่อให้ยาก):

| ชื่อ | Position | Wire Color | Side |
|------|----------|------------|------|
| Target_Yellow | (320, 100) | Yellow | Target |
| Target_Red | (320, 20) | Red | Target |
| Target_Blue | (320, -60) | Blue | Target |
| Target_Green | (320, -140) | Green | Target |

แต่ละจุด:
1. **Add Component → Wire Node**
2. ตั้ง **Side** = Source หรือ Target
3. ตั้ง **Wire Color** ให้ตรงสี
4. **Raycast Target: เปิด** (ต้องรับคลิกเมาส์)

#### D.3 เส้นลากและ Connection

1. สร้าง Image **`DragLine`** — Size (100, 6), Pivot **(0, 0.5)**, สีขาว, **Raycast Target: ปิด**, **ปิด GameObject**
2. สร้าง Empty **`ConnectionRoot`** — stretch เต็ม Panel

#### D.4 แนบ WireMinigame

| Field | ลากจาก |
|-------|--------|
| Panel Root | WirePanel |
| Source Nodes | ลาก 4 Source (Size = 4) |
| Target Nodes | ลาก 4 Target (Size = 4) |
| Drag Line Rect | DragLine |
| Connection Root | ConnectionRoot |

#### D.5 การเล่น

1. **คลิกค้าง** ที่ Source สีใดสีหนึ่ง
2. **ลาก** เมาส์ไปที่ Target **สีเดียวกัน**
3. **ปล่อย** — ถ้าถูกต้อง เส้นจะล็อก
4. ทำครบ 4 สี = สำเร็จ

> **Event Trigger ไม่จำเป็น** — WireNode ใช้ `IPointerDown/Drag/Up` ผ่าน EventSystem โดยตรง

---

### E. MinigameManager

1. สร้าง Empty GameObject ชื่อ **`MinigameManager`**
2. **Add Component → Minigame Manager**
3. ลาก references:

| Field | ค่า |
|-------|-----|
| Lockpick Minigame | LockpickPanel (component) |
| Struggle Minigame | StrugglePanel (component) |
| Wire Minigame | WirePanel (component) |
| Player Controller | Player |
| Minigame Canvas | MinigameCanvas |

---

### F. MinigameTrigger (ในโลกจริง)

ใช้กับประตู, ตู้ไฟ, กล่องลับ:

1. สร้าง GameObject หรือเลือกประตูที่มีอยู่
2. **Add Component → Box Collider**
   - **Is Trigger: เปิด**
   - ปรับ Size ให้ผู้เล่นเดินชนได้
3. **Add Component → Minigame Trigger**
4. ตั้ง **Minigame Type** = Lockpick / Struggle / Wire
5. ใน **On Minigame Success** → กด **+** เพิ่ม Event:
   - ตัวอย่าง: ลากประตูมา → `GameObject.SetActive(true)`
   - หรือ Animator → `SetTrigger("Open")`

#### ตัวอย่างเชื่อมประตู

```
On Minigame Success:
  └─ Door (GameObject) → SetActive(true)
  หรือ
  └─ DoorAnimator → SetTrigger("Open")
```

---

## ไฟล์สคริปต์ทั้งหมด

| ไฟล์ | หน้าที่ |
|------|---------|
| `Assets/Scripts/Minigames/MinigameType.cs` | Enum ประเภทมินิเกม |
| `Assets/Scripts/Minigames/MinigameBase.cs` | คลาสฐาน + Event สำเร็จ/ล้มเหลว |
| `Assets/Scripts/Minigames/MinigameManager.cs` | ตัวกลางเปิด/ปิด + ล็อกผู้เล่น |
| `Assets/Scripts/Minigames/LockpickMinigame.cs` | มินิเกมงัดกุญแจ |
| `Assets/Scripts/Minigames/StruggleMinigame.cs` | มินิเกมดิ้นรน |
| `Assets/Scripts/Minigames/WireMinigame.cs` | มินิเกมต่อสาย |
| `Assets/Scripts/Minigames/WireNode.cs` | จุดเชื่อมสาย + Drag |
| `Assets/Scripts/World/MinigameTrigger.cs` | Trigger ในโลกจริง |
| `Assets/Scripts/Editor/MidnightSchoolMinigameSetup.cs` | One-Click Setup |

---

## การเชื่อมกับ FirstPersonController

เมื่อมินิเกมเปิด `MinigameManager` จะเรียก:

```csharp
playerController.SetInputEnabled(false);  // หยุด WASD + เมาส์มอง
playerController.SetCursorLocked(false);  // แสดงเมาส์ + ปลดล็อก
```

เมื่อมินิเกมปิด → คืนค่ากลับทั้งสองอย่าง

---

## Troubleshooting

| อาการ | วิธีแก้ |
|-------|---------|
| เดินชน Trigger แล้วไม่มีอะไรเกิด | ตรวจ Tag Player, มี MinigameManager ใน Scene, Collider เป็น Trigger |
| กล้องยังหมุน / ยังเดินได้ตอนมินิเกม | ตรวจ Player Reference ใน MinigameManager |
| Wire ลากไม่ได้ | ต้องมี EventSystem, WireNode Raycast Target เปิด, GraphicRaycaster บน Canvas |
| Lockpick ชนขอบตลอด | ขยายระยะ Left/Right Boundary หรือลด Wobble Strength |
| Struggle เกจไม่เข้า Sweet Spot | ปรับตำแหน่ง SweetSpot ให้อยู่กลาง Track, ตรวจ Slider Track Rect |
| มินิเกมเปิดซ้อนกัน | MinigameManager ป้องกันอยู่แล้ว — รอปิดก่อนเข้า Trigger ใหม่ |

---

## ขั้นตอนถัดไป (Step 3+)

- เชื่อม `onMinigameSuccess` กับ Animator ประตูจริง
- ใส่ Sprite/Art แทน Image สี placeholder
- เพิ่มเสียง SFX ตอน mash / lockpick / wire connect
- ใช้ `currentSoundLevel` จาก FirstPersonController กับ AI ยาม

---

## Checklist ก่อน Play

- [ ] รัน Setup Step 1 (มี Player)
- [ ] รัน Setup Step 2 (มี Canvas + Manager)
- [ ] Player Tag = **Player**
- [ ] LockpickPanel / StrugglePanel / WirePanel **ปิด** ตอนไม่เล่น
- [ ] MinigameManager references ครบ
- [ ] Trigger มี Collider + Is Trigger
