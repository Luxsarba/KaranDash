#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class BuildStoryAuthoringScene
{
    private const string RootName = "StoryScaffoldRoot";
    private const string RuntimeRootName = "_SceneRuntime";
    private const string QuestItemsFolder = "Assets/_Project/QuestItems/StoryScaffold";
    private const string PrefabsFolder = "Assets/_Project/Prefabs/StoryScaffold";
    private const string RegistryPath = "Assets/_Project/Resources/Inventory/InventoryItemRegistry.asset";
    private const string LayoutPath = "Assets/_Project/Inventory/DefaultInventoryLayout.asset";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
    private const string PianoPanelPrefabPath = "Assets/_Project/Scripts/MiniGames/Piano/PianoPanel.prefab";
    private const string MemoryPanelPrefabPath = "Assets/_Project/Scripts/MiniGames/Memory/MemoryPanel.prefab";
    private const string TerritoryPanelPrefabPath = "Assets/_Project/Scripts/MiniGames/Territory/TerritoryPaintPanel.prefab";
    private const string FifteenPanelPrefabPath = "Assets/_Project/Scripts/MiniGames/Fifteen/FifteenPuzzlePanel.prefab";
    private const string EraserPrefabPath = "Assets/_Project/Prefabs/Enemies/Eraser.prefab";
    private const string EraserBossPrefabPath = "Assets/_Project/Prefabs/Enemies/EraserBoss.prefab";
    private const string ActiveSceneTransitionName = "Game assets";

    private sealed class StoryAssets
    {
        public GameObject playerPrefab;
        public GameObject pianoPanelPrefab;
        public GameObject memoryPanelPrefab;
        public GameObject territoryPanelPrefab;
        public GameObject fifteenPanelPrefab;
        public GameObject eraserPrefab;
        public GameObject eraserBossPrefab;

        public QuestItemData houseKey;
        public QuestItemData ancientArtifact;
        public QuestItemData tapeMeasure;
        public InventoryItemData officeKillToken;

        public CollectionPieceData fragment1;
        public CollectionPieceData fragment2;
        public CollectionPieceData fragment3;
        public CollectionPieceData fragment4;
        public CollectionPieceData fragment5;
        public CollectionPieceData fragment6;
        public CollectionSetData paintingSet;

        public GameObject bossFragmentLootPrefab;
    }

    [MenuItem("Tools/Story/Build Story Authoring Scene")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[BuildStoryAuthoringScene] Active scene is not valid.");
            return;
        }

        EnsureFolder("Assets/_Project/QuestItems", "StoryScaffold");
        EnsureFolder("Assets/_Project/Prefabs", "StoryScaffold");

        StoryAssets assets = LoadAndCreateAssets();
        UpdateInventoryRegistry(assets);
        UpdateInventoryLayout(assets);

        DisableSceneCamera();

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
            UnityEngine.Object.DestroyImmediate(existingRoot);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Story Authoring Scene");

        CreateRuntimeRoot(root.transform, assets.playerPrefab);
        CreateGround(root.transform);

        CreateReadmeArea(root.transform);
        CreateOptionalSaveStation(root.transform);

        Transform house = CreateSectionRoot(root.transform, "01_House", new Vector3(0f, 0f, 0f), "1. Дом");
        Transform city = CreateSectionRoot(root.transform, "01_1_City", new Vector3(60f, 0f, 0f), "1.1 / 1.2 / 1.3 / 1.4 / 6. Город");
        Transform office = CreateSectionRoot(root.transform, "02_Office", new Vector3(120f, 0f, 0f), "2. Офис");
        Transform barn = CreateSectionRoot(root.transform, "03_Barn", new Vector3(180f, 0f, 0f), "3. Коровник");
        Transform warehouse = CreateSectionRoot(root.transform, "04_Warehouse", new Vector3(240f, 0f, 0f), "4. Склад");
        Transform village = CreateSectionRoot(root.transform, "05_Village", new Vector3(300f, 0f, 0f), "5. Деревня");
        Transform finalCity = CreateSectionRoot(root.transform, "06_FinalCity", new Vector3(360f, 0f, 0f), "6. Город / Финал");

        CreateSpawnPoint(house, "house_start", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), true);
        CreateSpawnPoint(city, "city_hub", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);
        CreateSpawnPoint(office, "office_entry", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);
        CreateSpawnPoint(barn, "barn_entry", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);
        CreateSpawnPoint(warehouse, "warehouse_entry", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);
        CreateSpawnPoint(village, "village_entry", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);
        CreateSpawnPoint(finalCity, "final_city_entry", new Vector3(-11f, 1.1f, -5f), Quaternion.Euler(0f, 25f, 0f), false);

        BuildHouseSection(house, assets);
        BuildCitySection(city, assets);
        BuildOfficeSection(office, assets);
        BuildBarnSection(barn, assets);
        BuildWarehouseSection(warehouse, assets);
        BuildVillageSection(village, assets);
        BuildFinalCitySection(finalCity, assets);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log("[BuildStoryAuthoringScene] Story scaffold scene built successfully.");
    }

    private static StoryAssets LoadAndCreateAssets()
    {
        StoryAssets assets = new StoryAssets();
        assets.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        assets.pianoPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PianoPanelPrefabPath);
        assets.memoryPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MemoryPanelPrefabPath);
        assets.territoryPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerritoryPanelPrefabPath);
        assets.fifteenPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FifteenPanelPrefabPath);
        assets.eraserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EraserPrefabPath);
        assets.eraserBossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EraserBossPrefabPath);

        assets.houseKey = EnsureQuestItem("HouseKey", "Ключ");
        assets.ancientArtifact = EnsureQuestItem("AncientArtifact", "Артефакт");
        assets.tapeMeasure = EnsureQuestItem("TapeMeasure", "Рулетка");
        assets.officeKillToken = EnsureGeneralItem("OfficeKill12Token", "Office Kill 12 Complete");

        assets.fragment1 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_0.asset");
        assets.fragment2 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_1.asset");
        assets.fragment3 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_2.asset");
        assets.fragment4 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_3.asset");
        assets.fragment5 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_4.asset");
        assets.fragment6 = AssetDatabase.LoadAssetAtPath<CollectionPieceData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestPiece_5.asset");
        assets.paintingSet = AssetDatabase.LoadAssetAtPath<CollectionSetData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestSet.asset");

        assets.bossFragmentLootPrefab = EnsureBossLootPrefab(assets.fragment6);
        return assets;
    }

    private static void UpdateInventoryRegistry(StoryAssets assets)
    {
        InventoryItemRegistry registry = AssetDatabase.LoadAssetAtPath<InventoryItemRegistry>(RegistryPath);
        if (registry == null)
            return;

        SerializedObject so = new SerializedObject(registry);
        SerializedProperty itemsProp = so.FindProperty("items");
        AppendUniqueAsset(itemsProp, assets.houseKey);
        AppendUniqueAsset(itemsProp, assets.ancientArtifact);
        AppendUniqueAsset(itemsProp, assets.tapeMeasure);
        AppendUniqueAsset(itemsProp, assets.officeKillToken);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
    }

    private static void UpdateInventoryLayout(StoryAssets assets)
    {
        InventoryLayoutData layout = AssetDatabase.LoadAssetAtPath<InventoryLayoutData>(LayoutPath);
        if (layout == null)
            return;

        SerializedObject so = new SerializedObject(layout);
        SerializedProperty storySlots = so.FindProperty("storyTabSlots");
        EnsureArraySize(storySlots, 4);

        SetSlotDefinition(storySlots.GetArrayElementAtIndex(0), AssetDatabase.LoadAssetAtPath<InventoryItemData>("Assets/_Project/QuestItems/InventoryTest/PaintingTestQuestPass.asset"), "Quest Pass");
        SetSlotDefinition(storySlots.GetArrayElementAtIndex(1), assets.houseKey, "Ключ");
        SetSlotDefinition(storySlots.GetArrayElementAtIndex(2), assets.ancientArtifact, "Артефакт");
        SetSlotDefinition(storySlots.GetArrayElementAtIndex(3), assets.tapeMeasure, "Рулетка");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(layout);
    }

    private static void BuildHouseSection(Transform parent, StoryAssets assets)
    {
        CreateSectionNote(parent, "TemplateNote_House", new Vector3(-12f, 0.6f, 8f), "Дом", "Готовый паттерн", "Здесь собран рабочий паттерн: три записки -> пианино -> ключ -> квест-дверь -> переход. Переход сейчас ведет в этот же authoring scene на spawn 'city_hub', чтобы можно было тестировать систему переходов и инвентарь без дополнительных сцен.");
        CreateNoteObject(parent, "Note_01", new Vector3(-8f, 0.8f, 4f), "№1", "Ноты", "G4 G4");
        CreateNoteObject(parent, "Note_02", new Vector3(-6f, 0.8f, 5.5f), "№2", "Ноты", "A4 G4");
        CreateNoteObject(parent, "Note_03", new Vector3(-4f, 0.8f, 7f), "№3", "Ноты", "C5 B4");

        GameObject pianoObject = InstantiatePrefab(parent, assets.pianoPanelPrefab, "Piano_Minigame", new Vector3(0f, 0f, 5f), Quaternion.identity);
        SetPrivateString(pianoObject.GetComponent<PianoPanel>(), "melodySequence", "G4 G4 A4 G4 C5 B4");
        CreateWorldLabel(pianoObject.transform, "Пианино -> Ключ", new Vector3(0f, 2.4f, 0f));

        GameObject pianoReward = CreateQuestActionHost(parent, "Reward_HouseKey_FromPiano", new Vector3(0f, 0.5f, 9f), new InventoryItemData[] { assets.houseKey }, null, null);
        AddPersistentUnityEventListener(pianoObject.GetComponent<PianoPanel>(), "onSuccess", pianoReward.GetComponent<QuestCompleteActions>().Run);

        GameObject unlockedTransition = CreateTransitionTrigger(parent, "UnlockedTransition_ToCity", new Vector3(11f, 1.1f, 0f), new Vector3(1.5f, 2f, 5f), ActiveSceneTransitionName, "city_hub");
        unlockedTransition.SetActive(false);

        GameObject door = CreateNpcBody(parent, "QuestDoor_KeyRequired", PrimitiveType.Cube, new Vector3(9f, 1.5f, 0f), new Vector3(2f, 3f, 0.4f), "Дверь: нужен ключ");
        EnsurePersistentId(door);
        QuestCompleteActions doorActions = door.AddComponent<QuestCompleteActions>();
        SetQuestActionHost(doorActions, null, new GameObject[] { unlockedTransition }, null);
        ConfigureFetchQuestNpc(door, assets.houseKey, CreateDialogue("Дверь", "door", "Игрок", "Я закрыта. Найди ключ и тогда открою путь."), CreateDialogue("Дверь", "door", "Игрок", "Ключа всё ещё нет."), CreateDialogue("Дверь", "door", "Игрок", "Ключ подошёл. Проход открыт."), CreateDialogue("Дверь", "door", "Игрок", "Путь уже открыт."), doorActions);
    }

    private static void BuildCitySection(Transform parent, StoryAssets assets)
    {
        CreateSectionNote(parent, "TemplateNote_City", new Vector3(-12f, 0.6f, 8f), "Город", "Паттерн NPC+награда", "Рамка и NPC, которые сразу отдают предмет, собраны через раздельные hotspot-объекты Talk/Reward. Это не прихоть сцены, а текущее ограничение interaction path: для надежного authoring нельзя вешать и диалог, и reward на один и тот же hit-объект.");
        CreateDialogueNpc(parent, "NPC_Elder1", new Vector3(-6f, 1f, 1f), "Старейшина 1", CreateDialogue("Старейшина 1", "elder1", "Игрок", "Ох, юный друг, представляю, какой путь тебе предстоит преодолеть, чтобы восстановить картину.", "Я буду тебе помогать. Ещё увидимся."));
        CreateDialogueRewardNpc(parent, "NPC_Frame_GivesFragment1", new Vector3(-1f, 1f, -1.5f), CreateDialogue("Рамка", "frame", "Игрок", "Вот один фрагмент, когда-то их было больше, но я хз."), new InventoryItemData[] { assets.fragment1 }, "Рамка -> Фрагмент #1");
        CreateDialogueNpc(parent, "Hint_ToOffice", new Vector3(3f, 1f, 3.5f), "Подсказка: Офис", CreateDialogue("Город", "hub", "Игрок", "Следующая зона — Офис. Там квест с ластиками."));
        CreateDialogueNpc(parent, "Hint_ToBarn", new Vector3(6f, 1f, 3.5f), "Подсказка: Коровник", CreateDialogue("Город", "hub", "Игрок", "Следующая зона — Коровник. Там мини-игра на захват поля."));
        CreateDialogueNpc(parent, "Hint_ToWarehouse", new Vector3(9f, 1f, 3.5f), "Подсказка: Склад", CreateDialogue("Город", "hub", "Игрок", "Следующая зона — Склад. Понадобятся рычаги и пятнашки."));
        CreateDialogueNpc(parent, "Hint_ToVillage", new Vector3(12f, 1f, 3.5f), "Подсказка: Деревня", CreateDialogue("Город", "hub", "Игрок", "Следующая зона — Деревня. Там квест на артефакт и Саня говорит."));
        CreateDialogueNpc(parent, "Hint_ToFinalCity", new Vector3(15f, 1f, 3.5f), "Подсказка: Финал", CreateDialogue("Город", "hub", "Игрок", "Финальная зона — босс ластиков и последний фрагмент."));
        CreateTransitionTrigger(parent, "ToOffice", new Vector3(10f, 1.1f, -4f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "office_entry");
        CreateTransitionTrigger(parent, "ToBarn", new Vector3(13f, 1.1f, -1f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "barn_entry");
        CreateTransitionTrigger(parent, "ToWarehouse", new Vector3(16f, 1.1f, 2f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "warehouse_entry");
        CreateTransitionTrigger(parent, "ToVillage", new Vector3(16f, 1.1f, 6f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "village_entry");
        CreateTransitionTrigger(parent, "ToFinalCity", new Vector3(10f, 1.1f, 9f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "final_city_entry");
    }

private static void BuildOfficeSection(Transform parent, StoryAssets assets)
    {
        CreateSectionNote(parent, "Office_CombatTemplate", new Vector3(-3f, 0.6f, 7f), "Офис", "Квест на 12 убийств", "Здесь уже собран рабочий kill-counter на 12 ластиков. Для нормального поведения AI в реальной локации всё ещё нужен baked NavMesh, но objective и reward-пайплайн уже готовы.");

        GameObject elder2 = CreateNpcBody(parent, "NPC_Elder2", PrimitiveType.Capsule, new Vector3(-6f, 1f, 1f), new Vector3(1f, 2f, 1f), "Старейшина 2");
        EnsurePersistentId(elder2);
        QuestCompleteActions elder2Actions = elder2.AddComponent<QuestCompleteActions>();
        SetQuestActionHost(elder2Actions, new InventoryItemData[] { assets.fragment2 }, null, null);
        ConfigureFetchQuestNpc(
            elder2,
            assets.officeKillToken,
            CreateDialogue("Старейшина 2", "elder2", "Игрок", "Чтобы достать фрагмент, нужно одолеть 12 ластиков.", "Возвращайся ко мне, когда закончишь."),
            CreateDialogue("Старейшина 2", "elder2", "Игрок", "Счёт ещё не закрыт. Уничтожь 12 ластиков и вернись."),
            CreateDialogue("Старейшина 2", "elder2", "Игрок", "Ты справился. Забирай второй фрагмент."),
            CreateDialogue("Старейшина 2", "elder2", "Игрок", "Ластики уже побеждены. Ищи следующий фрагмент."),
            elder2Actions);

        GameObject eraserPack = new GameObject("OfficeEraserPack12");
        eraserPack.transform.SetParent(parent, false);
        eraserPack.transform.localPosition = new Vector3(2f, 0f, 0f);
        CreateWorldLabel(eraserPack.transform, "12 ластиков -> вернуться к Старейшине 2", new Vector3(0f, 2.5f, -5f));
        EnsurePersistentId(eraserPack);
        KillCountObjectiveTracker tracker = eraserPack.AddComponent<KillCountObjectiveTracker>();
        ConfigureKillCountObjectiveTracker(tracker, assets.officeKillToken, 12, elder2.GetComponent<FetchQuestNPC>(), null);

        for (int i = 0; i < 12; i++)
        {
            int row = i / 4;
            int col = i % 4;
            Vector3 localPos = new Vector3(col * 2.5f, 0f, row * 2.5f);
            InstantiatePrefab(eraserPack.transform, assets.eraserPrefab, "Eraser_" + (i + 1), localPos, Quaternion.identity);
        }

        CreateTransitionTrigger(parent, "Exit_ToCity", new Vector3(12f, 1.1f, 0f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "city_hub");
    }

    private static void BuildBarnSection(Transform parent, StoryAssets assets)
    {
        CreateDialogueNpc(parent, "NPC_Elder3", new Vector3(-6f, 1f, 1f), "Старейшина 3", CreateDialogue("Старейшина 3", "elder3", "Игрок", "Захвати поле на том ковре, но будь осторожен, здесь бродят ластики."));
        GameObject territory = InstantiatePrefab(parent, assets.territoryPanelPrefab, "Territory_Minigame", new Vector3(2f, 0f, 2f), Quaternion.identity);
        CreateWorldLabel(territory.transform, "Захват поля -> Фрагмент #3", new Vector3(0f, 2.5f, -4f));
        GameObject reward = CreateQuestActionHost(parent, "Reward_Fragment3_FromTerritory", new Vector3(0f, 0.5f, 8f), new InventoryItemData[] { assets.fragment3 }, null, null);
        AddPersistentUnityEventListener(territory.GetComponent<TerritoryPaintPanel>(), "onSuccess", reward.GetComponent<QuestCompleteActions>().Run);
        CreateTransitionTrigger(parent, "Return_ToCity", new Vector3(12f, 1.1f, 0f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "city_hub");
    }

private static void BuildWarehouseSection(Transform parent, StoryAssets assets)
    {
        CreateSectionNote(parent, "TemplateNote_Warehouse", new Vector3(-12f, 0.6f, 8f), "Склад", "Паттерн рычагов", "Паттерн на три рычага собран как физическая multi-lock дверь: каждый lever отдельно отводит свою перекладину. Так это работает без нового агрегатора состояний.");
        GameObject exitFrame = CreatePrimitive(parent, "ExitDoorFrame", PrimitiveType.Cube, new Vector3(12f, 2f, 0f), new Vector3(0.5f, 4f, 4f), false);
        UnityEngine.Object.DestroyImmediate(exitFrame.GetComponent<Collider>());
        GameObject bar1 = CreatePrimitive(parent, "DoorBar_1", PrimitiveType.Cube, new Vector3(10.6f, 1f, -1.5f), new Vector3(3f, 0.3f, 0.3f), false);
        GameObject bar2 = CreatePrimitive(parent, "DoorBar_2", PrimitiveType.Cube, new Vector3(10.6f, 2f, 0f), new Vector3(3f, 0.3f, 0.3f), false);
        GameObject bar3 = CreatePrimitive(parent, "DoorBar_3", PrimitiveType.Cube, new Vector3(10.6f, 3f, 1.5f), new Vector3(3f, 0.3f, 0.3f), false);
        ConfigureLever(CreateLever(parent, "Lever_1", new Vector3(-7f, 0.9f, -3f), "Рычаг 1"), bar1.transform, LeverSwitch.RotationAxis.Z, 90f);
        ConfigureLever(CreateLever(parent, "Lever_2", new Vector3(-3.5f, 0.9f, -3f), "Рычаг 2"), bar2.transform, LeverSwitch.RotationAxis.Z, 90f);
        ConfigureLever(CreateLever(parent, "Lever_3", new Vector3(0f, 0.9f, -3f), "Рычаг 3"), bar3.transform, LeverSwitch.RotationAxis.Z, 90f);

        GameObject stairsRoot = new GameObject("PuzzleReward_BoxStairs");
        stairsRoot.transform.SetParent(parent, false);
        stairsRoot.transform.localPosition = new Vector3(3f, 0f, 6f);
        CreateWorldLabel(stairsRoot.transform, "Лестница из коробок", new Vector3(0f, 4.8f, 0f));
        for (int i = 0; i < 6; i++)
            CreatePrimitive(stairsRoot.transform, "Box_" + (i + 1), PrimitiveType.Cube, new Vector3(i * 1.2f, 0.5f + i * 0.5f, 0f), new Vector3(1f, 1f, 1f), true);
        stairsRoot.SetActive(false);

        GameObject fifteen = InstantiatePrefab(parent, assets.fifteenPanelPrefab, "Fifteen_Minigame", new Vector3(-4f, 0f, 4f), Quaternion.identity);
        CreateWorldLabel(fifteen.transform, "Пятнашки -> лестница", new Vector3(0f, 2.8f, 0f));
        GameObject fifteenReward = CreatePersistentActionHost(parent, "Reward_Boxes_FromFifteen", new Vector3(-1f, 0.5f, 8f), null, new GameObject[] { stairsRoot }, null);
        AddPersistentUnityEventListener(fifteen.GetComponent<FifteenPuzzlePanel>(), "onSuccess", fifteenReward.GetComponent<PersistentActionTrigger>().Complete);

        CreateDialogueRewardNpc(parent, "NPC_Elder4_GivesTapeMeasure", new Vector3(4f, 1f, -2f), CreateDialogue("Старейшина 4", "elder4", "Игрок", "Нужно выбраться отсюда, держи рулетку, с ее помощью, ты сможешь взобраться на те вершины.", "Она тебе ещё пригодится. Удачи!"), new InventoryItemData[] { assets.tapeMeasure }, "Старейшина 4 -> Рулетка");
        CreatePickup(parent, "Fragment4_AtExit", new Vector3(13.5f, 0.8f, 0f), assets.fragment4, true, "Фрагмент #4");
        CreateTransitionTrigger(parent, "Return_ToCity", new Vector3(16f, 1.1f, 0f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "city_hub");
    }

private static void BuildVillageSection(Transform parent, StoryAssets assets)
    {
        CreatePickup(parent, "ArtifactPickup_MOVE_ME", new Vector3(-8f, 0.8f, 6f), assets.ancientArtifact, true, "Артефакт");
        GameObject memory = InstantiatePrefab(parent, assets.memoryPanelPrefab, "Memory_Minigame", new Vector3(2f, 0f, 2f), Quaternion.identity);
        memory.SetActive(false);
        CreateWorldLabel(memory.transform, "Саня говорит -> кубики", new Vector3(0f, 2.2f, -3.5f));

        GameObject cubePath = new GameObject("MemoryReward_CubePath");
        cubePath.transform.SetParent(parent, false);
        cubePath.transform.localPosition = new Vector3(6f, 0f, 2f);
        CreateWorldLabel(cubePath.transform, "Путь к простым карандашам", new Vector3(2.5f, 3.5f, 0f));
        for (int i = 0; i < 8; i++)
            CreatePrimitive(cubePath.transform, "PathCube_" + (i + 1), PrimitiveType.Cube, new Vector3(i * 1.3f, 0.5f + (i % 2 == 0 ? 0f : 1.2f), 0f), new Vector3(1f, 1f, 1f), true);
        cubePath.SetActive(false);

        GameObject memoryReward = CreatePersistentActionHost(parent, "Reward_CubePath_FromMemory", new Vector3(0f, 0.5f, 8f), null, new GameObject[] { cubePath }, null);
        AddPersistentUnityEventListener(memory.GetComponent<MemoryPanel>(), "onSuccess", memoryReward.GetComponent<PersistentActionTrigger>().Complete);

        GameObject elder5 = CreateNpcBody(parent, "NPC_Elder5_ArtifactQuest", PrimitiveType.Capsule, new Vector3(-2f, 1f, -1.5f), new Vector3(1f, 2f, 1f), "Старейшина 5");
        EnsurePersistentId(elder5);
        QuestCompleteActions elder5Actions = elder5.AddComponent<QuestCompleteActions>();
        SetQuestActionHost(elder5Actions, null, new GameObject[] { memory }, null);
        ConfigureFetchQuestNpc(elder5, assets.ancientArtifact, CreateDialogue("Старейшина 5", "elder5", "Игрок", "Тебе нужно к племени простых карандашей, но путь закрыт.", "Найди мне тот артефакт."), CreateDialogue("Старейшина 5", "elder5", "Игрок", "Без артефакта я не открою путь к мини-игре."), CreateDialogue("Старейшина 5", "elder5", "Игрок", "Вот оно. Теперь мини-игра доступна, путь можно начать."), CreateDialogue("Старейшина 5", "elder5", "Игрок", "Мини-игра уже открыта."), elder5Actions);
        CreateDialogueRewardNpc(parent, "NPC_SimplePencil_GivesFragment5", new Vector3(18f, 1f, 2f), CreateDialogue("Простой карандаш", "pencil", "Игрок", "Здравствуй путник, благодарим за пройденный путь.", "Мы наслышаны о твоих подвигах."), new InventoryItemData[] { assets.fragment5 }, "Простой карандаш -> Фрагмент #5");
        CreateTransitionTrigger(parent, "Return_ToCity", new Vector3(12f, 1.1f, -3f), new Vector3(1.5f, 2f, 4f), ActiveSceneTransitionName, "city_hub");
    }

private static void BuildFinalCitySection(Transform parent, StoryAssets assets)
    {
        CreateDialogueNpc(parent, "NPC_Elder6", new Vector3(-6f, 1f, 1f), "Старейшина 6", CreateDialogue("Старейшина 6", "elder6", "Игрок", "Последний фрагмент забрал главарь ластиков.", "Одолей его, но будь осторожен."));
        GameObject boss = InstantiatePrefab(parent, assets.eraserBossPrefab, "Boss_Eraser", new Vector3(2f, 0f, 0f), Quaternion.identity);
        CreateWorldLabel(boss.transform, "БОСС: спавнит ластиков раз в 10 сек", new Vector3(0f, 3.5f, -5f));

        SerializedObject bossEnemySo = new SerializedObject(boss.GetComponent<Enemy>());
        bossEnemySo.FindProperty("hasLoot").boolValue = true;
        bossEnemySo.FindProperty("lootPrefab").objectReferenceValue = assets.bossFragmentLootPrefab;
        bossEnemySo.ApplyModifiedPropertiesWithoutUndo();

        EraserBossSpawner spawner = boss.GetComponent<EraserBossSpawner>();
        SerializedObject spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("spawnInterval").floatValue = 10f;
        spawnerSo.FindProperty("maxAliveSpawned").intValue = 4;
        spawnerSo.FindProperty("enemyPrefab").objectReferenceValue = assets.eraserPrefab;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        BoxCollider trigger = boss.transform.Find("BattleTrigger").GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(22f, 5f, 22f);
        trigger.center = new Vector3(0f, 1.5f, 0f);

        CreateSectionNote(parent, "Authoring_FinalVideo", new Vector3(10f, 0.6f, 7f), "Финал", "Видео placeholder", "Финальная рамка уже проверяет полный набор из 6 фрагментов и запускает cutscene overlay. Для полного прогона нужно только назначить VideoClip на объект FinalCutsceneOverlay.");
        VideoCutsceneOverlayPlayer cutsceneOverlay = CreateCutsceneOverlay(parent, "FinalCutsceneOverlay");

        GameObject frame = CreateNpcBody(parent, "FrameNPC_Final", PrimitiveType.Cube, new Vector3(10f, 1.2f, 2f), new Vector3(1.5f, 2.5f, 0.5f), "Рамка: собрать 6 фрагментов");
        EnsurePersistentId(frame);
        QuestCompleteActions frameActions = frame.AddComponent<QuestCompleteActions>();
        SetQuestActionHost(frameActions, null, null, null);
        AddPersistentUnityEventListener(frameActions, "onCompleted", cutsceneOverlay.Play);
        ConfigureCollectionSetQuestInteractable(
            frame,
            assets.paintingSet,
            CreateDialogue("Рамка", "frame", "Игрок", "Собери все шесть фрагментов и верни картину."),
            CreateDialogue("Рамка", "frame", "Игрок", "Пока ещё не хватает фрагментов."),
            CreateDialogue("Рамка", "frame", "Игрок", "Картина восстановлена. Спасибо за игру."),
            CreateDialogue("Рамка", "frame", "Игрок", "Картина уже восстановлена."),
            frameActions);
    }

private static void CreateReadmeArea(Transform parent)
    {
        Transform readme = CreateSectionRoot(parent, "00_Readme", new Vector3(-60f, 0f, 0f), "README / Ограничения");
        string text =
            "1. Все scene transitions сейчас настроены на этот же scene 'Game assets' и разные SceneSpawnPoint — это сделано специально для проверки одного authoring scene." + "\n" +
            "2. При переносе в реальные сцены у trigger'ов нужно заменить targetSceneName/targetSpawnPointId." + "\n" +
            "3. Dialogue + instant reward NPC сейчас собраны через раздельные Talk/Reward hotspot'ы — это текущее безопасное authoring-решение." + "\n" +
            "4. Kill quest на 12 ластиков, финальная рамка на 6 фрагментов и persistent world-state для мини-игр уже поддержаны кодом этой сцены." + "\n" +
            "5. Combat section templates всё ещё требуют baked NavMesh в реальных сценах для нормального поведения AI." + "\n" +
            "6. Для финального ролика в scaffold нужно только назначить VideoClip на объект FinalCutsceneOverlay.";
        CreateSectionNote(readme, "Readme_Note", new Vector3(-2f, 0.6f, 0f), "Story Scaffold", "Как использовать", text);
    }

    private static void CreateOptionalSaveStation(Transform parent)
    {
        GameObject stationRoot = new GameObject("Optional_SaveStation_Template");
        stationRoot.transform.SetParent(parent, false);
        stationRoot.transform.localPosition = new Vector3(-48f, 0f, -6f);
        CreateWorldLabel(stationRoot.transform, "Опциональная SaveStation", new Vector3(0f, 3.2f, 0f));
        GameObject baseVisual = CreatePrimitive(stationRoot.transform, "StationBody", PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 2f), true);
        SaveStation station = baseVisual.AddComponent<SaveStation>();
        SetPrivateString(station, "stationId", "story_scaffold_station");
        SetPrivateString(station, "saveLocationLabel", "Story Scaffold");
        Transform spawnPoint = CreateSpawnPoint(stationRoot.transform, "story_scaffold_station_spawn", new Vector3(0f, 1.1f, -4f), Quaternion.identity, false);
        SetPrivateObjectReference(station, "spawnPoint", spawnPoint.GetComponent<SceneSpawnPoint>());
    }

    private static void CreateRuntimeRoot(Transform parent, GameObject playerPrefab)
    {
        GameObject runtimeRoot = new GameObject(RuntimeRootName);
        runtimeRoot.transform.SetParent(parent, false);
        GameplaySceneBootstrap bootstrap = runtimeRoot.AddComponent<GameplaySceneBootstrap>();
        SetPrivateObjectReference(bootstrap, "playerPrefab", playerPrefab);
    }

    private static void CreateGround(Transform parent)
    {
        GameObject ground = CreatePrimitive(parent, "Ground", PrimitiveType.Plane, new Vector3(180f, 0f, 0f), new Vector3(48f, 1f, 12f), true);
        ground.isStatic = true;
    }

    private static Transform CreateSectionRoot(Transform parent, string name, Vector3 position, string labelText)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        section.transform.localPosition = position;
        CreatePrimitive(section.transform, "SectionFloor", PrimitiveType.Cube, new Vector3(0f, -0.25f, 0f), new Vector3(28f, 0.5f, 18f), true);
        CreateWorldLabel(section.transform, labelText, new Vector3(0f, 4.2f, -7f));
        return section.transform;
    }

    private static Transform CreateSpawnPoint(Transform parent, string spawnPointId, Vector3 localPosition, Quaternion localRotation, bool isDefaultSceneSpawn)
    {
        GameObject go = new GameObject("Spawn_" + spawnPointId);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        SceneSpawnPoint spawnPoint = go.AddComponent<SceneSpawnPoint>();
        SetPrivateString(spawnPoint, "spawnPointId", spawnPointId);
        SetPrivateBool(spawnPoint, "isDefaultSceneSpawn", isDefaultSceneSpawn);
        return go.transform;
    }

    private static GameObject CreateTransitionTrigger(Transform parent, string name, Vector3 localPosition, Vector3 size, string targetSceneName, string targetSpawnPointId)
    {
        GameObject trigger = CreatePrimitive(parent, name, PrimitiveType.Cube, localPosition, new Vector3(size.x, size.y, Mathf.Max(size.z, 0.25f)), true);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        SceneTransitionTrigger transition = trigger.AddComponent<SceneTransitionTrigger>();
        SetPrivateString(transition, "targetSceneName", targetSceneName);
        SetPrivateString(transition, "targetSpawnPointId", targetSpawnPointId);
        CreateWorldLabel(trigger.transform, "Transition -> " + targetSpawnPointId, new Vector3(0f, 1.8f, 0f));
        return trigger;
    }

    private static void CreateNoteObject(Transform parent, string name, Vector3 localPosition, string title, string subtitle, string text)
    {
        GameObject note = CreatePrimitive(parent, name, PrimitiveType.Cube, localPosition, new Vector3(0.7f, 0.1f, 1f), true);
        NoteTrigger trigger = note.AddComponent<NoteTrigger>();
        SetPrivateString(trigger, "noteTitle", title);
        SetPrivateString(trigger, "noteSubtitle", subtitle);
        SetPrivateString(trigger, "noteText", text);
        CreateWorldLabel(note.transform, title, new Vector3(0f, 0.6f, 0f));
    }

    private static void CreateSectionNote(Transform parent, string name, Vector3 localPosition, string title, string subtitle, string text)
    {
        CreateNoteObject(parent, name, localPosition, title, subtitle, text);
    }

    private static GameObject CreateDialogueNpc(Transform parent, string name, Vector3 localPosition, string label, Dialogue dialogue)
    {
        GameObject npc = CreateNpcBody(parent, name, PrimitiveType.Capsule, localPosition, new Vector3(1f, 2f, 1f), label);
        DialogueTrigger trigger = npc.AddComponent<DialogueTrigger>();
        SetDialogue(trigger, "dialogue", dialogue);
        return npc;
    }

    private static void CreateDialogueRewardNpc(Transform parent, string name, Vector3 localPosition, Dialogue dialogue, InventoryItemData[] rewardItems, string label)
    {
        GameObject root = CreateNpcBody(parent, name, PrimitiveType.Capsule, localPosition, new Vector3(1f, 2f, 1f), label);
        UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
        GameObject talk = CreatePrimitive(root.transform, "TalkHotspot", PrimitiveType.Cube, new Vector3(-1.4f, 1f, 1f), new Vector3(0.8f, 1f, 0.8f), true);
        DialogueTrigger dialogueTrigger = talk.AddComponent<DialogueTrigger>();
        SetDialogue(dialogueTrigger, "dialogue", dialogue);
        CreateWorldLabel(talk.transform, "Talk", new Vector3(0f, 0.9f, 0f));
        GameObject reward = CreatePrimitive(root.transform, "RewardHotspot", PrimitiveType.Cube, new Vector3(1.4f, 1f, 1f), new Vector3(0.8f, 1f, 0.8f), true);
        InventoryRewardInteractable rewardInteractable = reward.AddComponent<InventoryRewardInteractable>();
        SetRewardInteractable(rewardInteractable, null, true, rewardItems, true);
        CreateWorldLabel(reward.transform, "Reward", new Vector3(0f, 0.9f, 0f));
    }

    private static GameObject CreatePickup(Transform parent, string name, Vector3 localPosition, InventoryItemData item, bool persistent, string label)
    {
        GameObject pickup = CreatePrimitive(parent, name, PrimitiveType.Cube, localPosition, new Vector3(0.7f, 0.7f, 0.7f), true);
        InventoryItemPickup itemPickup = pickup.AddComponent<InventoryItemPickup>();
        SetPrivateObjectReference(itemPickup, "item", item);
        SetPrivateBool(itemPickup, "destroyOnPickup", true);
        if (persistent)
            EnsurePersistentId(pickup);
        CreateWorldLabel(pickup.transform, label, new Vector3(0f, 0.8f, 0f));
        return pickup;
    }

    private static GameObject CreateNpcBody(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, string label)
    {
        GameObject body = CreatePrimitive(parent, name, primitiveType, localPosition, localScale, true);
        CreateWorldLabel(body.transform, label, new Vector3(0f, 1.6f, 0f));
        return body;
    }

    private static GameObject CreateLever(Transform parent, string name, Vector3 localPosition, string label)
    {
        GameObject lever = CreatePrimitive(parent, name, PrimitiveType.Cylinder, localPosition, new Vector3(0.5f, 0.5f, 0.5f), true);
        lever.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreateWorldLabel(lever.transform, label, new Vector3(0f, 1.2f, 0f));
        lever.AddComponent<LeverSwitch>();
        return lever;
    }

    private static void ConfigureLever(GameObject leverObject, Transform target, LeverSwitch.RotationAxis axis, float angle)
    {
        LeverSwitch lever = leverObject.GetComponent<LeverSwitch>();
        lever.Configure(target, axis, true, angle, 0.6f, true, null);
        EditorUtility.SetDirty(lever);
    }

    private static GameObject CreateQuestActionHost(Transform parent, string name, Vector3 localPosition, InventoryItemData[] rewardItems, GameObject[] enableObjects, GameObject[] disableObjects)
    {
        GameObject host = new GameObject(name);
        host.transform.SetParent(parent, false);
        host.transform.localPosition = localPosition;
        QuestCompleteActions actions = host.AddComponent<QuestCompleteActions>();
        SetQuestActionHost(actions, rewardItems, enableObjects, disableObjects);
        return host;
    }

private static GameObject CreatePersistentActionHost(Transform parent, string name, Vector3 localPosition, InventoryItemData[] rewardItems, GameObject[] enableObjects, GameObject[] disableObjects)
    {
        GameObject host = CreateQuestActionHost(parent, name, localPosition, rewardItems, enableObjects, disableObjects);
        EnsurePersistentId(host);

        QuestCompleteActions actions = host.GetComponent<QuestCompleteActions>();
        PersistentActionTrigger trigger = host.AddComponent<PersistentActionTrigger>();
        SetPrivateObjectReference(trigger, "persistentStateId", host.GetComponent<PersistentWorldObjectId>());
        SetPrivateObjectReference(trigger, "actions", actions);
        return host;
    }


    private static void SetQuestActionHost(QuestCompleteActions actions, InventoryItemData[] rewardItems, GameObject[] enableObjects, GameObject[] disableObjects)
    {
        SerializedObject so = new SerializedObject(actions);
        SetObjectReferenceArray(so.FindProperty("rewardItems"), rewardItems);
        SetObjectReferenceArray(so.FindProperty("enableObjects"), enableObjects);
        SetObjectReferenceArray(so.FindProperty("disableObjects"), disableObjects);
        SetObjectReferenceArray(so.FindProperty("spawnPrefabs"), null);
        SetObjectReferenceArray(so.FindProperty("spawnPoints"), null);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(actions);
    }

    private static void ConfigureFetchQuestNpc(GameObject target, InventoryItemData requiredItem, Dialogue before, Dialogue waiting, Dialogue completed, Dialogue after, QuestCompleteActions actions)
    {
        FetchQuestNPC npc = target.AddComponent<FetchQuestNPC>();
        SetPrivateObjectReference(npc, "requiredItem", requiredItem);
        SetDialogue(npc, "beforeQuestDialogue", before);
        SetDialogue(npc, "waitingDialogue", waiting);
        SetDialogue(npc, "completedDialogue", completed);
        SetDialogue(npc, "afterCompletedDialogue", after);
        SetPrivateObjectReference(npc, "onCompleteActions", actions);
        SetPrivateObjectReference(npc, "persistentStateId", target.GetComponent<PersistentWorldObjectId>());
        EditorUtility.SetDirty(npc);
    }

private static void ConfigureCollectionSetQuestInteractable(GameObject target, CollectionSetData requiredSet, Dialogue before, Dialogue waiting, Dialogue completed, Dialogue after, QuestCompleteActions actions)
    {
        CollectionSetQuestInteractable interactable = target.AddComponent<CollectionSetQuestInteractable>();
        SetPrivateObjectReference(interactable, "requiredSet", requiredSet);
        SetDialogue(interactable, "beforeQuestDialogue", before);
        SetDialogue(interactable, "waitingDialogue", waiting);
        SetDialogue(interactable, "completedDialogue", completed);
        SetDialogue(interactable, "afterCompletedDialogue", after);
        SetPrivateObjectReference(interactable, "onCompleteActions", actions);
        SetPrivateObjectReference(interactable, "persistentStateId", target.GetComponent<PersistentWorldObjectId>());
        EditorUtility.SetDirty(interactable);
    }

private static void ConfigureKillCountObjectiveTracker(KillCountObjectiveTracker tracker, InventoryItemData completionTokenItem, int targetCount, FetchQuestNPC gatingQuestNpc, QuestCompleteActions onReachedTarget)
    {
        SetPrivateObjectReference(tracker, "persistentStateId", tracker.GetComponent<PersistentWorldObjectId>());
        SetPrivateObjectReference(tracker, "trackedEnemiesRoot", tracker.transform);
        SetPrivateObjectReference(tracker, "completionTokenItem", completionTokenItem);
        SetPrivateObjectReference(tracker, "onReachedTarget", onReachedTarget);
        SetPrivateObjectReference(tracker, "gatingQuestNpc", gatingQuestNpc);
        SetPrivateInt(tracker, "targetCount", targetCount);
        EditorUtility.SetDirty(tracker);
    }



    private static void SetRewardInteractable(InventoryRewardInteractable interactable, InventoryItemData requiredItem, bool consumeRequiredItem, InventoryItemData[] rewardItems, bool singleUse)
    {
        SetPrivateObjectReference(interactable, "requiredItem", requiredItem);
        SetPrivateBool(interactable, "consumeRequiredItem", consumeRequiredItem);
        SetPrivateBool(interactable, "singleUse", singleUse);
        SerializedObject so = new SerializedObject(interactable);
        SetObjectReferenceArray(so.FindProperty("rewardItems"), rewardItems);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(interactable);
    }

    private static Dialogue CreateDialogue(string name, string email, string recipient, params string[] sentences)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.name = name;
        dialogue.email = email;
        dialogue.recipientName = recipient;
        dialogue.sentences = sentences;
        return dialogue;
    }

    private static void SetDialogue(UnityEngine.Object target, string fieldName, Dialogue dialogue)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;
        prop.FindPropertyRelative("name").stringValue = dialogue != null ? dialogue.name ?? string.Empty : string.Empty;
        prop.FindPropertyRelative("email").stringValue = dialogue != null ? dialogue.email ?? string.Empty : string.Empty;
        prop.FindPropertyRelative("recipientName").stringValue = dialogue != null ? dialogue.recipientName ?? string.Empty : string.Empty;
        prop.FindPropertyRelative("portrait").objectReferenceValue = dialogue != null ? dialogue.portrait : null;
        SerializedProperty sentencesProp = prop.FindPropertyRelative("sentences");
        string[] sentences = dialogue != null && dialogue.sentences != null ? dialogue.sentences : Array.Empty<string>();
        sentencesProp.arraySize = sentences.Length;
        for (int i = 0; i < sentences.Length; i++)
            sentencesProp.GetArrayElementAtIndex(i).stringValue = sentences[i] ?? string.Empty;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static QuestItemData EnsureQuestItem(string itemId, string displayName)
    {
        string path = QuestItemsFolder + "/" + itemId + ".asset";
        QuestItemData item = AssetDatabase.LoadAssetAtPath<QuestItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<QuestItemData>();
            item.itemId = itemId;
            item.displayName = displayName;
            item.category = InventoryItemCategory.Quest;
            AssetDatabase.CreateAsset(item, path);
        }
        else
        {
            item.itemId = itemId;
            item.displayName = displayName;
            item.category = InventoryItemCategory.Quest;
            EditorUtility.SetDirty(item);
        }
        return item;
    }

private static InventoryItemData EnsureGeneralItem(string itemId, string displayName)
    {
        string path = QuestItemsFolder + "/" + itemId + ".asset";
        InventoryItemData item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<InventoryItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemId = itemId;
        item.displayName = displayName;
        item.category = InventoryItemCategory.General;
        EditorUtility.SetDirty(item);
        return item;
    }


    private static GameObject EnsureBossLootPrefab(CollectionPieceData fragment6)
    {
        string path = PrefabsFolder + "/BossFragment6Pickup.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            InventoryItemPickup existingPickup = prefab.GetComponent<InventoryItemPickup>();
            if (existingPickup != null)
            {
                SetPrivateObjectReference(existingPickup, "item", fragment6);
                SetPrivateBool(existingPickup, "destroyOnPickup", true);
                EditorUtility.SetDirty(existingPickup);
            }
            return prefab;
        }
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "BossFragment6Pickup";
        temp.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        InventoryItemPickup pickup = temp.AddComponent<InventoryItemPickup>();
        SetPrivateObjectReference(pickup, "item", fragment6);
        SetPrivateBool(pickup, "destroyOnPickup", true);
        prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void DisableSceneCamera()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            mainCamera.SetActive(false);
    }

    private static void AppendUniqueAsset(SerializedProperty arrayProperty, UnityEngine.Object asset)
    {
        if (arrayProperty == null || asset == null)
            return;
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            if (arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                return;
        }
        int index = arrayProperty.arraySize;
        arrayProperty.InsertArrayElementAtIndex(index);
        arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = asset;
    }

    private static void SetSlotDefinition(SerializedProperty slotProperty, InventoryItemData item, string placeholderTitle)
    {
        if (slotProperty == null)
            return;
        slotProperty.FindPropertyRelative("item").objectReferenceValue = item;
        slotProperty.FindPropertyRelative("placeholderTitle").stringValue = placeholderTitle;
        slotProperty.FindPropertyRelative("placeholderIcon").objectReferenceValue = item != null ? item.icon : null;
    }

    private static void EnsureArraySize(SerializedProperty property, int size)
    {
        if (property != null)
            property.arraySize = size;
    }

    private static void SetObjectReferenceArray(SerializedProperty arrayProperty, UnityEngine.Object[] values)
    {
        if (arrayProperty == null)
            return;
        int size = values != null ? values.Length : 0;
        arrayProperty.arraySize = size;
        for (int i = 0; i < size; i++)
            arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void AddPersistentUnityEventListener(Component target, string eventFieldName, UnityAction call)
    {
        if (target == null || call == null)
            return;
        FieldInfo field = target.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            return;
        UnityEvent unityEvent = field.GetValue(target) as UnityEvent;
        if (unityEvent == null)
        {
            unityEvent = new UnityEvent();
            field.SetValue(target, unityEvent);
        }
        UnityEventTools.AddPersistentListener(unityEvent, call);
        EditorUtility.SetDirty(target);
    }

    private static GameObject InstantiatePrefab(Transform parent, GameObject prefab, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("Failed to instantiate prefab: " + (prefab != null ? prefab.name : "null"));
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        return instance;
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
        }
        return go;
    }

    private static void CreateWorldLabel(Transform parent, string text, Vector3 localPosition)
    {
        GameObject label = new GameObject("Label_" + text.Replace(" ", "_"));
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.black;
    }

private static VideoCutsceneOverlayPlayer CreateCutsceneOverlay(Transform parent, string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        VideoCutsceneOverlayPlayer overlayPlayer = root.AddComponent<VideoCutsceneOverlayPlayer>();
        VideoPlayer videoPlayer = root.AddComponent<VideoPlayer>();

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        overlay.transform.SetParent(root.transform, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        StretchRectTransform(overlayRect);

        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image background = overlay.GetComponent<Image>();
        background.color = Color.black;

        GameObject videoImageObject = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage));
        videoImageObject.transform.SetParent(overlay.transform, false);
        RectTransform videoRect = videoImageObject.GetComponent<RectTransform>();
        StretchRectTransform(videoRect);
        RawImage videoImage = videoImageObject.GetComponent<RawImage>();
        videoImage.color = Color.white;

        GameObject skipButtonObject = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipButtonObject.transform.SetParent(overlay.transform, false);
        RectTransform skipRect = skipButtonObject.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.sizeDelta = new Vector2(180f, 52f);
        skipRect.anchoredPosition = new Vector2(-40f, -40f);

        Image skipImage = skipButtonObject.GetComponent<Image>();
        skipImage.color = new Color(0f, 0f, 0f, 0.65f);
        Button skipButton = skipButtonObject.GetComponent<Button>();
        skipButton.targetGraphic = skipImage;

        GameObject skipTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        skipTextObject.transform.SetParent(skipButtonObject.transform, false);
        RectTransform skipTextRect = skipTextObject.GetComponent<RectTransform>();
        StretchRectTransform(skipTextRect);
        Text skipText = skipTextObject.GetComponent<Text>();
        skipText.text = "Пропустить";
        skipText.alignment = TextAnchor.MiddleCenter;
        skipText.color = Color.white;
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        overlay.SetActive(false);

        SetPrivateObjectReference(overlayPlayer, "overlayRoot", overlay);
        SetPrivateObjectReference(overlayPlayer, "videoImage", videoImage);
        SetPrivateObjectReference(overlayPlayer, "videoPlayer", videoPlayer);
        SetPrivateObjectReference(overlayPlayer, "skipButton", skipButton);
        SetPrivateBool(overlayPlayer, "returnToMenuOnFinish", true);
        EditorUtility.SetDirty(overlayPlayer);
        return overlayPlayer;
    }

private static void StretchRectTransform(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }



    private static void EnsurePersistentId(GameObject go)
    {
        PersistentWorldObjectId id = go.GetComponent<PersistentWorldObjectId>();
        if (id == null)
            id = go.AddComponent<PersistentWorldObjectId>();
        SerializedObject so = new SerializedObject(id);
        SerializedProperty prop = so.FindProperty("persistentId");
        if (string.IsNullOrWhiteSpace(prop.stringValue))
            prop.stringValue = Guid.NewGuid().ToString("N");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(id);
    }

    private static void SetPrivateString(UnityEngine.Object target, string fieldName, string value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;
        prop.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetPrivateBool(UnityEngine.Object target, string fieldName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;
        prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

private static void SetPrivateInt(UnityEngine.Object target, string fieldName, int value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;
        prop.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }


    private static void SetPrivateObjectReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
