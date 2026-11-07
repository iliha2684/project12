#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저장/불러오기 버튼만 평소에 노출하고, 클릭 시 3개 슬롯을 보여주는 팝업을 설치한다.
/// 기존 저장 시스템 및 슬롯 데이터 형식은 변경하지 않는다.
/// </summary>
public static class MemoryOfficeSaveLoadInstaller
{
    [MenuItem("기억관리국/UI/14. 저장·불러오기 버튼 설치 (팝업 슬롯)")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 종료", "먼저 Play 모드를 종료해 주세요.", "확인");
            return;
        }

        Canvas canvas = null;
        foreach (Canvas item in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (item.GetComponentInChildren<ExaminationController>(true) != null ||
                item.transform.Find("MemoryOffice_Design_Layers") != null)
            {
                canvas = item;
                break;
            }
        }
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음", "SampleScene의 Canvas를 먼저 열어 주세요.", "확인");
            return;
        }

        // 이미 설치한 버튼과 MemoryOfficeSaveLoadUI 컴포넌트는 재사용한다.
        GameObject root = EnsureChild(canvas.transform, "MemoryOffice_SaveLoadControls", false);
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        RectTransform controlRect = root.GetComponent<RectTransform>();
        controlRect.anchorMin = controlRect.anchorMax = new Vector2(1f, 1f);
        controlRect.pivot = new Vector2(1f, 1f);
        controlRect.anchoredPosition = new Vector2(-14f, -14f);
        controlRect.sizeDelta = new Vector2(346f, 106f);
        controlRect.localScale = Vector3.one;

        TMP_FontAsset font = null;
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font != null) { font = text.font; break; }
        }

        Button save = ButtonAt(root.transform, "Btn_SaveGame", "저장", new Vector2(-84f, -31f),
            font, 155f, 45f);
        Button load = ButtonAt(root.transform, "Btn_LoadGame", "불러오기", new Vector2(84f, -31f),
            font, 155f, 45f);
        TMP_Text notice = TextAt(root.transform, "SaveFeedback", "", new Vector2(0f, -26f),
            new Vector2(336f, 30f), 15f, font, TextAlignmentOptions.Center);

        // 옛 UI의 슬롯 설명 라벨은 숨긴다. 슬롯 버튼은 아래 팝업으로 옮긴다.
        Transform oldInfo = root.transform.Find("SlotInformation");
        if (oldInfo != null) oldInfo.gameObject.SetActive(false);

        // 전체 Canvas를 덮는 투명 검은 배경. 창이 열린 동안 다른 UI 클릭 방지.
        GameObject overlay = EnsureChild(canvas.transform, "MemoryOffice_SaveLoadDialog", true);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(.5f, .5f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        Image blocker = overlay.GetComponent<Image>();
        if (blocker == null) blocker = Undo.AddComponent<Image>(overlay);
        blocker.color = new Color(0f, 0f, 0f, .67f);
        blocker.raycastTarget = true;

        GameObject panel = EnsureChild(overlay.transform, "SlotDialogPanel", true);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 408f);
        panelRect.localScale = Vector3.one;
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null) panelImage = Undo.AddComponent<Image>(panel);
        panelImage.color = new Color(.115f, .135f, .13f, .98f);
        panelImage.raycastTarget = true;

        TMP_Text title = TextAt(panel.transform, "DialogTitle", "저장할 슬롯 선택",
            new Vector2(0f, 162f), new Vector2(520f, 46f), 29f, font,
            TextAlignmentOptions.Center);
        TMP_Text hint = TextAt(panel.transform, "DialogHint",
            "슬롯을 누르면 해당 위치에 저장합니다.", new Vector2(0f, 116f),
            new Vector2(510f, 40f), 16f, font, TextAlignmentOptions.Center);

        // 기존 3슬롯 UI에서 생성한 버튼을 찾아 팝업 안으로 옮겨 재사용한다.
        Button slot1 = SlotButton(root.transform, panel.transform, "Btn_SaveSlot1", "슬롯 1",
            new Vector2(0f, 61f), font);
        Button slot2 = SlotButton(root.transform, panel.transform, "Btn_SaveSlot2", "슬롯 2",
            new Vector2(0f, -18f), font);
        Button slot3 = SlotButton(root.transform, panel.transform, "Btn_SaveSlot3", "슬롯 3",
            new Vector2(0f, -97f), font);
        Button close = ButtonAt(panel.transform, "Btn_CloseSaveDialog", "닫기",
            new Vector2(0f, -157f), font, 168f, 38f, false);

        MemoryOfficeSaveLoadUI ui = root.GetComponent<MemoryOfficeSaveLoadUI>();
        if (ui == null) ui = Undo.AddComponent<MemoryOfficeSaveLoadUI>(root);
        SerializedObject serialized = new SerializedObject(ui);
        serialized.FindProperty("saveButton").objectReferenceValue = save;
        serialized.FindProperty("loadButton").objectReferenceValue = load;
        serialized.FindProperty("feedbackText").objectReferenceValue = notice;
        serialized.FindProperty("slot1Button").objectReferenceValue = slot1;
        serialized.FindProperty("slot2Button").objectReferenceValue = slot2;
        serialized.FindProperty("slot3Button").objectReferenceValue = slot3;
        serialized.FindProperty("slotDialog").objectReferenceValue = overlay;
        serialized.FindProperty("slotDialogTitle").objectReferenceValue = title;
        serialized.FindProperty("slotDialogHint").objectReferenceValue = hint;
        serialized.FindProperty("closeDialogButton").objectReferenceValue = close;
        serialized.FindProperty("slotInfoText").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        ClearPersistent(save);
        ClearPersistent(load);
        ClearPersistent(slot1);
        ClearPersistent(slot2);
        ClearPersistent(slot3);
        ClearPersistent(close);
        // 바깥 버튼만 보이는 상태로 씬에 저장한다.
        overlay.transform.SetAsLastSibling();
        overlay.SetActive(false);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("저장·불러오기 팝업 설치 완료",
            "기존 저장/불러오기 버튼을 누르면 3개 슬롯 선택 창이 열립니다.\n" +
            "팝업에서 슬롯을 누르면 선택한 작업이 바로 실행됩니다.\n" +
            "기존 저장 파일과 GUID는 그대로 유지합니다.\n" +
            "Ctrl+S로 씬을 저장해 주세요.", "확인");
    }

    private static GameObject EnsureChild(Transform parent, string name, bool image)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null) go = existing.gameObject;
        else
        {
            go = image
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, name + " 생성");
            go.transform.SetParent(parent, false);
        }
        go.SetActive(true);
        return go;
    }

    private static Button SlotButton(Transform oldParent, Transform newParent, string name,
        string label, Vector2 position, TMP_FontAsset font)
    {
        Transform old = newParent.Find(name);
        if (old == null) old = oldParent.Find(name);
        if (old != null && old.parent != newParent)
            Undo.SetTransformParent(old, newParent, "슬롯 버튼을 팝업으로 이동");
        return ButtonAt(newParent, name, label, position, font, 490f, 61f, false);
    }

    private static void ClearPersistent(Button button)
    {
        if (button == null) return;
        Undo.RecordObject(button, "저장 버튼 이벤트 재설정");
        button.onClick = new Button.ButtonClickedEvent();
    }

    private static Button ButtonAt(Transform parent, string name, string label,
        Vector2 position, TMP_FontAsset font, float width, float height, bool fromTop = true)
    {
        Transform old = parent.Find(name);
        GameObject go;
        if (old == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "버튼 생성");
            go.transform.SetParent(parent, false);
        }
        else go = old.gameObject;
        go.SetActive(true);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = fromTop ? new Vector2(.5f, 1f) : new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = new Vector2(width, height);
        r.anchoredPosition = position;
        r.localScale = Vector3.one;
        Image img = go.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(go);
        img.color = new Color(.20f, .29f, .24f, .97f);
        img.raycastTarget = true;
        Button b = go.GetComponent<Button>();
        if (b == null) b = Undo.AddComponent<Button>(go);
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        TextAt(go.transform, "Label", label, Vector2.zero,
            new Vector2(width - 16f, height - 8f), width > 450f ? 19f : 20f, font,
            TextAlignmentOptions.Center);
        return b;
    }

    private static TMP_Text TextAt(Transform parent, string name, string content,
        Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font,
        TextAlignmentOptions alignment)
    {
        Transform old = parent.Find(name);
        GameObject go;
        if (old == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "텍스트 생성");
            go.transform.SetParent(parent, false);
        }
        else go = old.gameObject;
        go.SetActive(true);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        r.localScale = Vector3.one;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = new Color(.95f, .92f, .85f, 1f);
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (font != null) text.font = font;
        return text;
    }
}
#endif
