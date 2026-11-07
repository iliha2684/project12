#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>기존 Canvas와 모든 .meta를 보존하며 NPC 수락/거절 팝업만 설치.</summary>
public static class MemoryOfficeNpcOfferInstaller
{
    private const string RootName = "MemoryOffice_NpcOfferPrompt";

    [MenuItem("기억관리국/UI/7. NPC 제안 수락·거절 팝업 연결")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play를 끄고 SampleScene에서 설치해 주세요.", "확인");
            return;
        }

        Canvas canvas = null;
        ExaminationController examination = null;
        foreach (var candidate in Object.FindObjectsByType<ExaminationController>(FindObjectsSortMode.None))
        {
            Canvas c = candidate.GetComponent<Canvas>();
            if (c == null) c = candidate.GetComponentInParent<Canvas>();
            if (c == null || c.transform.Find("MemoryOffice_Design_Layers") == null) continue;
            canvas = c;
            examination = candidate;
            break;
        }
        if (canvas == null || examination == null)
        {
            EditorUtility.DisplayDialog("게임 Canvas를 찾을 수 없음",
                "SampleScene에 Canvas와 MemoryOffice_Design_Layers가 있는지 확인해 주세요.", "확인");
            return;
        }

        GameObject root;
        Transform old = canvas.transform.Find(RootName);
        if (old != null)
        {
            root = old.gameObject;
            if (!EditorUtility.DisplayDialog("NPC 팝업이 이미 있음",
                "기존 NPC 팝업을 그대로 두고 버튼 및 연결을 다시 설정할까요?", "다시 연결", "취소"))
                return;
        }
        else
        {
            root = new GameObject(RootName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(root, "NPC 제안 팝업 생성");
            root.transform.SetParent(canvas.transform, false);
        }
        root.SetActive(true);
        root.transform.SetAsLastSibling();

        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        var backdrop = Require<Image>(root);
        backdrop.color = new Color(0.025f, 0.035f, 0.04f, 0.71f);
        backdrop.raycastTarget = true;  // 팝업 뒤의 심사 버튼을 막는다.
        var group = Require<CanvasGroup>(root);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject card = EnsurePanel(root.transform, "OfferCard", 820f, 370f,
            new Color(0.13f, 0.12f, 0.12f, 1f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchoredPosition = new Vector2(0f, -35f);

        TMP_FontAsset font = FindSceneFont(canvas);
        TMP_Text title = EnsureText(card.transform, "OfferTitle", font,
            new Vector2(0f, 123f), new Vector2(760f, 66f), 37f,
            new Color(0.98f, 0.92f, 0.75f, 1f), "NPC 제안", FontStyles.Bold);
        TMP_Text description = EnsureText(card.transform, "OfferDescription", font,
            new Vector2(0f, 19f), new Vector2(738f, 145f), 27f,
            new Color(0.95f, 0.96f, 0.94f, 1f), "선택을 기다리고 있습니다.", FontStyles.Normal);

        TMP_Text acceptLabel;
        Button accept = EnsureButton(card.transform, "Btn_NpcOfferAccept", font,
            new Vector2(-190f, -130f), "수락", new Color(0.19f, 0.41f, 0.29f, 1f), out acceptLabel);
        TMP_Text rejectLabel;
        Button reject = EnsureButton(card.transform, "Btn_NpcOfferReject", font,
            new Vector2(190f, -130f), "거절", new Color(0.58f, 0.22f, 0.19f, 1f), out rejectLabel);

        var runtime = Require<MemoryOfficeNpcOfferPrompt>(root);
        SerializedObject so = new SerializedObject(runtime);
        so.FindProperty("popupGroup").objectReferenceValue = group;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("acceptText").objectReferenceValue = acceptLabel;
        so.FindProperty("rejectText").objectReferenceValue = rejectLabel;
        so.FindProperty("acceptButton").objectReferenceValue = accept;
        so.FindProperty("rejectButton").objectReferenceValue = reject;
        so.FindProperty("examinationController").objectReferenceValue = examination;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 이전 설치로 OnClick 항목이 남아 있다면 중복 콜백을 막는다.
        Undo.RecordObject(accept, "NPC 수락 버튼 이벤트 재설정");
        accept.onClick = new Button.ButtonClickedEvent();
        Undo.RecordObject(reject, "NPC 거절 버튼 이벤트 재설정");
        reject.onClick = new Button.ButtonClickedEvent();

        // 이벤트가 발생하기 전엔 게임 화면을 가리지 않는다.
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root;
        Debug.Log("[기억관리국 UI] 상인/백색서고 최종 제안 수락·거절 팝업 설치 완료");
        EditorUtility.DisplayDialog("NPC 제안 팝업 설치 완료",
            "상인 그림 구매 제안과 DAY 13 백색서고 최종 제안이 나타나면 Play 화면에 수락/거절 팝업이 뜹니다.\n" +
            "선택 전에는 팝업 뒤의 판정 버튼을 누를 수 없습니다.\n\n" +
            "Ctrl + S로 씬을 저장해 주세요.", "확인");
    }

    private static T Require<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static GameObject EnsurePanel(Transform parent, string name,
        float width, float height, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "NPC 제안 카드 생성");
            go.transform.SetParent(parent, false);
        }
        go.SetActive(true);
        RectTransform r = Require<RectTransform>(go);
        Place(r, Vector2.zero, new Vector2(width, height));
        Image image = Require<Image>(go);
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font,
        Vector2 position, Vector2 size, float fontSize, Color color, string value, FontStyles style)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "NPC 제안 텍스트 생성");
            go.transform.SetParent(parent, false);
        }
        go.SetActive(true);
        Place(Require<RectTransform>(go), position, size);
        TextMeshProUGUI text = Require<TextMeshProUGUI>(go);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (font != null) text.font = font;
        return text;
    }

    private static Button EnsureButton(Transform parent, string name, TMP_FontAsset font,
        Vector2 position, string caption, Color color, out TMP_Text label)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "NPC 제안 버튼 생성");
            go.transform.SetParent(parent, false);
        }
        go.SetActive(true);
        Place(Require<RectTransform>(go), position, new Vector2(295f, 85f));
        Image image = Require<Image>(go);
        image.color = color;
        image.raycastTarget = true;
        Button button = Require<Button>(go);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.interactable = true;
        label = EnsureText(go.transform, "Label", font, Vector2.zero,
            new Vector2(275f, 72f), 31f, Color.white, caption, FontStyles.Bold);
        return button;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
    }

    private static void Place(RectTransform r, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
    }

    private static TMP_FontAsset FindSceneFont(Canvas canvas)
    {
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == "StatusText" && text.font != null) return text.font;
        return TMP_Settings.defaultFontAsset;
    }
}
#endif
