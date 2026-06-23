# Midnight School — คู่มือ Step 1 (ภาษาไทย)

เกมแนว Horror/Survival มุมมองบุคคลที่หนึ่ง — ระบบเดิน, ไฟฉาย, และ Sound Meter

---

## สิ่งที่ได้ใน Step 1

| ระบบ | รายละเอียด |
|------|------------|
| First-Person Movement | เดิน (WASD), วิ่ง (Shift), หมอบคลาน (Ctrl ค้าง) |
| Mouse Look | หมุนกล้องแนวนอน/แนวตั้ง พร้อมจำกัดมุมก้ม-เงย |
| Crouch | ลดความสูงตัวและกล้องอย่าง Smooth + ช้าลง |
| Flashlight | กด F เปิด/ปิด Spotlight |
| Sound Meter | `currentSoundLevel` — 0 หยุด, 0.4 เดิน, 0.8 วิ่ง, 0 หมอบ |

---

## ขั้นตอนการใช้งาน (มือใหม่)

### 1. เปิดโปรเจกต์

1. เปิด **Unity Hub**
2. เปิดโปรเจกต์ **`My project (3)`**
3. รอ Unity Compile สคริปต์จนเสร็จ (ดูมุมล่างขวา ไม่มี loading)

### 2. เปิด Scene

1. ไปที่ **Project** → `Assets/Scenes/SampleScene`
2. ดับเบิลคลิกเพื่อเปิด Scene

### 3. ตั้งค่าอัตโนมัติ (One-Click)

1. คลิกเมนูบนสุด **Midnight School**
2. เลือก **Setup Step 1 (Player + Scene)**
3. กด **Setup** ใน Dialog ที่ขึ้นมา

สคริปต์จะสร้างให้อัตโนมัติ:
- Player + CharacterController + FirstPersonController
- ย้าย Main Camera เป็นลูกของ Player
- Flashlight (Spot Light + URP) + FlashlightController
- พื้นและกำแพงทดสอบ
- ลดแสงฉากให้มืด (Horror mood)

### 4. ทดสอบใน Play Mode

1. กดปุ่ม **Play** ▶
2. คลิกใน Game view เพื่อล็อกเมาส์
3. ทดสอบปุ่ม:

| ปุ่ม | ผลลัพธ์ |
|------|---------|
| W/A/S/D | เดิน |
| Left Shift + WASD | วิ่ง |
| Left Ctrl (ค้าง) + WASD | หมอบคลาน |
| เมาส์ | มองรอบ |
| F | เปิด/ปิดไฟฉาย |

### 5. ดูค่า currentSoundLevel

1. ขณะ Play อยู่ เลือก **Player** ใน Hierarchy
2. ดูใน Inspector ที่ component **First Person Controller**
3. ค่า **Current Sound Level** จะเปลี่ยนตามการเคลื่อนไหว

---

## โครงสร้าง Hierarchy หลัง Setup

```
Scene
├── Player                          ← FirstPersonController + CharacterController
│   └── Main Camera                 ← Audio Listener + URP Camera
│       └── Flashlight              ← Spot Light + FlashlightController
├── TestFloor                       ← พื้นทดสอบ
├── TestWalls                       ← กำแพงทดสอบ
├── Directional Light               ← แสงน้อยมาก
└── Global Volume
```

---

## ไฟล์สคริปต์

| ไฟล์ | ใส่ที่ |
|------|--------|
| `Assets/Scripts/Player/FirstPersonController.cs` | Player |
| `Assets/Scripts/Player/FlashlightController.cs` | Flashlight |
| `Assets/Scripts/Editor/MidnightSchoolPlayerSetup.cs` | (Editor เท่านั้น — ไม่ต้องแนบ) |

---

## ปัญหาที่พบบ่อย

### กด WASD / เมาส์ไม่ทำงาน

- โปรเจกต์ตั้ง Input เป็น **Both** แล้ว (Legacy + Input System)
- ถ้ายังไม่ทำงาน: **Edit → Project Settings → Player → Active Input Handling** → เลือก **Both** → Restart Unity

### กล้องไม่หมุน

- ตรวจว่า **Camera Transform** ใน FirstPersonController มี Main Camera ใส่แล้ว
- คลิกใน Game view ก่อน (เพื่อล็อกเมาส์)

### ไฟฉายไม่ส่อง (URP)

- ตรวจว่า Flashlight มี **Light** type = Spot
- ตรวจ Intensity ≈ 150 (URP ใช้ค่าสูงกว่า Built-in)
- กด F เพื่อเปิดไฟ

### ตัวจมพื้นหรือลอย

- ตั้ง Player Position Y = **1**
- Character Controller Height = **2**, Center Y = **1**

### เมนู Midnight School ไม่ขึ้น

- รอ Unity Compile สคริปต์เสร็จ
- ตรวจ Console ว่าไม่มี error สีแดง

---

## ขั้นตอนถัดไป (Step 2)

- เสียงฝีเท้าจริง (AudioSource + clip)
- UI แสดง Sound Meter
- AI ตรวจจับเสียงจาก `currentSoundLevel`
- ระบบแบตเตอรี่ไฟฉาย
