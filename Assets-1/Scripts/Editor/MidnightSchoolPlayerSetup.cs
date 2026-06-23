using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// ตั้งค่า Player, Camera, Flashlight และฉากทดสอบอัตโนมัติ
/// เมนู: Midnight School > Setup Step 1 (Player + Scene)
/// </summary>
public static class MidnightSchoolPlayerSetup
{
    private const string PlayerName = "Player";
    private const string FlashlightName = "Flashlight";
    private const string TestFloorName = "TestFloor";
    private const string TestWallsName = "TestWalls";

    [MenuItem("Midnight School/Setup Step 1 (Player + Scene)")]
    public static void SetupStep1()
    {
        if (!EditorUtility.DisplayDialog(
                "Midnight School — Setup Step 1",
                "สคริปต์นี้จะสร้าง Player, ย้าย Main Camera, สร้าง Flashlight และฉากทดสอบใน Scene ปัจจุบัน\n\nดำเนินการต่อ?",
                "Setup",
                "Cancel"))
        {
            return;
        }

        GameObject existingPlayer = GameObject.Find(PlayerName);
        if (existingPlayer != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "พบ Player อยู่แล้ว",
                    "มี GameObject ชื่อ 'Player' อยู่ใน Scene แล้ว\nต้องการลบและสร้างใหม่?",
                    "สร้างใหม่",
                    "ยกเลิก"))
            {
                return;
            }

            Object.DestroyImmediate(existingPlayer);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("Error", "ไม่พบ Main Camera ใน Scene", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Midnight School Setup Step 1");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject player = CreatePlayer(mainCamera.transform);
        GameObject flashlight = CreateFlashlight(mainCamera.transform);
        WirePlayerReferences(player, mainCamera.transform);
        SetupTestEnvironment();
        DimSceneLighting();

        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log("[Midnight School] Setup Step 1 เสร็จสมบูรณ์ — กด Play เพื่อทดสอบ (WASD, Shift, Ctrl, Mouse, F)");
    }

    private static GameObject CreatePlayer(Transform cameraTransform)
    {
        GameObject player = new GameObject(PlayerName);
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.transform.position = new Vector3(0f, 1f, 0f);

        CharacterController controller = Undo.AddComponent<CharacterController>(player);
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0.001f;

        Undo.AddComponent<FirstPersonController>(player);

        Undo.SetTransformParent(cameraTransform, player.transform, "Parent Camera to Player");
        cameraTransform.localPosition = new Vector3(0f, 1.7f, 0f);
        cameraTransform.localRotation = Quaternion.identity;

        return player;
    }

    private static GameObject CreateFlashlight(Transform cameraTransform)
    {
        Transform existing = cameraTransform.Find(FlashlightName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject flashlight = new GameObject(FlashlightName);
        Undo.RegisterCreatedObjectUndo(flashlight, "Create Flashlight");
        Undo.SetTransformParent(flashlight.transform, cameraTransform, "Parent Flashlight to Camera");
        flashlight.transform.localPosition = Vector3.zero;
        flashlight.transform.localRotation = Quaternion.identity;

        Light light = Undo.AddComponent<Light>(flashlight);
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.96f, 0.78f);
        light.intensity = 150f;
        light.range = 15f;
        light.spotAngle = 45f;
        light.shadows = LightShadows.Soft;
        light.enabled = false;

        Undo.AddComponent<UniversalAdditionalLightData>(flashlight);
        Undo.AddComponent<FlashlightController>(flashlight);

        return flashlight;
    }

    private static void WirePlayerReferences(GameObject player, Transform cameraTransform)
    {
        FirstPersonController controller = player.GetComponent<FirstPersonController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupTestEnvironment()
    {
        GameObject existingFloor = GameObject.Find(TestFloorName);
        if (existingFloor != null)
            Object.DestroyImmediate(existingFloor);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(floor, "Create Test Floor");
        floor.name = TestFloorName;
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        GameObject existingWalls = GameObject.Find(TestWallsName);
        if (existingWalls != null)
            Object.DestroyImmediate(existingWalls);

        GameObject wallsRoot = new GameObject(TestWallsName);
        Undo.RegisterCreatedObjectUndo(wallsRoot, "Create Test Walls");

        CreateWall(wallsRoot.transform, new Vector3(0f, 1.5f, 24f), new Vector3(50f, 3f, 1f));
        CreateWall(wallsRoot.transform, new Vector3(0f, 1.5f, -24f), new Vector3(50f, 3f, 1f));
        CreateWall(wallsRoot.transform, new Vector3(24f, 1.5f, 0f), new Vector3(1f, 3f, 50f));
        CreateWall(wallsRoot.transform, new Vector3(-24f, 1.5f, 0f), new Vector3(1f, 3f, 50f));
    }

    private static void CreateWall(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(wall, "Create Wall");
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }

    private static void DimSceneLighting()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                Undo.RecordObject(light, "Dim Directional Light");
                light.intensity = 0.08f;
            }
        }

        RenderSettings.ambientIntensity = 0.4f;
    }
}
