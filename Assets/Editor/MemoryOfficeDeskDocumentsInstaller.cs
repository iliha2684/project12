#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds five independent desk thumbnails and wires each to the original
/// IDcard / Paper. Leaves existing save, verdict, Canvas art, and .meta intact.
/// Re-running the menu updates/reuses children rather than duplicating them.
/// </summary>
public static class MemoryOfficeDeskDocumentsInstaller
{
    private const string RootName = "MemoryOffice_DeskDocuments";
    private static readonly string[] Names =
    {
        "Desk_IDCard", "Desk_Application", "Desk_Prescription",
        "Desk_MemoryReport", "Desk_Consent"
    };
    private static readonly string[] Captions =
    {
        "시민증", "신청서", "처방전", "검사표", "동의서"
    };

    [MenuItem("기억관리국/UI/17. 시민 서류 책상 배치·클릭 상세보기")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 종료한 뒤 실행해 주세요.", "확인");
            return;
        }

        ExaminationController examination = null;
        Canvas canvas = null;
        foreach (var candidate in Object.FindObjectsByType<ExaminationController>(FindObjectsSortMode.None))
        {
            var c = candidate.GetComponent<Canvas>() ?? candidate.GetComponentInParent<Canvas>();
            if (c == null || c.transform.Find("MemoryOffice_Design_Layers") == null)
                continue;
            examination = candidate;
            canvas = c;
            break;
        }
        if (canvas == null || examination == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음", "SampleScene을 열고 새 UI Canvas가 있는지 확인해 주세요.", "확인");
            return;
        }

        GameObject idCard = FindChild(canvas.transform, "IDcard")?.gameObject;
        GameObject paper = FindChild(canvas.transform, "Paper")?.gameObject;
        TMP_Text paperSource = FindChild(paper != null ? paper.transform : null, "PaperText")?.GetComponent<TMP_Text>();
        if (idCard == null || paper == null || paperSource == null)
        {
            EditorUtility.DisplayDialog("기존 서류 UI 없음",
                "Canvas 안의 IDcard, Paper, PaperText가 필요합니다.\n기존 서류 UI를 삭제하지 않았는지 확인해 주세요.", "확인");
            return;
        }

        Transform root = GetOrCreate(canvas.transform, RootName);
        var rootRect = (RectTransform)root;
        Stretch(rootRect);
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();

        var idSprite = idCard.GetComponent<Image>()?.sprite;
        var paperSprite = paper.GetComponent<Image>()?.sprite;
        TMP_FontAsset font = FindTextFont(canvas);

        var controller = root.GetComponent<MemoryOfficeDeskDocuments>();
        if (controller == null)
            controller = Undo.AddComponent<MemoryOfficeDeskDocuments>(root.gameObject);

        var buttons = new Button[5];
        var cards = new RectTransform[5];
        // 1920 x 1080 reference. These land on the visible desk, clear of the
        // left decision buttons, the upper counter and the right emergency button.
        const float startX = 610f;
        const float spacing = 183f;
        for (int i = 0; i < Names.Length; i++)
        {
            var go = GetOrCreate(root, Names[i]);
            var rect = (RectTransform)go;
            float x = startX + i * spacing;
            SetRect(rect, new Vector2(x - 960f, 540f - 880f),
                i == 0 ? new Vector2(152f, 110f) : new Vector2(142f, 174f));
            cards[i] = rect;

            var image = GetOrAdd<Image>(go.gameObject);
            Undo.RecordObject(image, "서류 아이콘 이미지");
            image.sprite = i == 0 ? idSprite : paperSprite;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;

            buttons[i] = GetOrAdd<Button>(go.gameObject);
            Undo.RecordObject(buttons[i], "서류 아이콘 클릭 설정");
            buttons[i].targetGraphic = image;
            buttons[i].transition = Selectable.Transition.ColorTint;
            buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };

            var label = GetOrCreate(go, "Label");
            var labelRect = (RectTransform)label;
            SetRect(labelRect, new Vector2(0f, -(i == 0 ? 74f : 104f)), new Vector2(160f, 34f));
            var txt = GetOrAdd<TextMeshProUGUI>(label.gameObject);
            Undo.RecordObject(txt, "서류 아이콘 이름");
            txt.text = Captions[i];
            txt.fontSize = 27f;
            if (font != null) txt.font = font;
            txt.color = new Color32(245, 228, 194, 255);
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;
            txt.raycastTarget = false;
            go.gameObject.SetActive(false); // Visible only after a citizen finishes talking.
        }

        Button closeID = MakeCloseButton(idCard.transform, font, "MemoryOffice_CloseIDCard");
        Button closePaper = MakeCloseButton(paper.transform, font, "MemoryOffice_ClosePaper");

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("examination").objectReferenceValue = examination;
        serialized.FindProperty("idCardPanel").objectReferenceValue = idCard;
        serialized.FindProperty("paperPanel").objectReferenceValue = paper;
        serialized.FindProperty("paperSourceText").objectReferenceValue = paperSource;
        serialized.FindProperty("closeIdCardButton").objectReferenceValue = closeID;
        serialized.FindProperty("closePaperButton").objectReferenceValue = closePaper;
        var btnArray = serialized.FindProperty("deskButtons");
        var rectArray = serialized.FindProperty("deskCards");
        btnArray.arraySize = 5;
        rectArray.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            btnArray.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            rectArray.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }
        serialized.ApplyModifiedProperties();

        // Show the newly installed flow from the first frame of Play mode;
        // existing controller still owns the details and fills their text.
        Undo.RecordObject(idCard, "서류 상세보기 시작 상태");
        Undo.RecordObject(paper, "서류 상세보기 시작 상태");
        idCard.SetActive(false);
        paper.SetActive(false);
        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        EditorUtility.DisplayDialog("책상 위 서류 배치 완료",
            "시민 대화가 끝나면 서류 아이콘이 창구에서 책상으로 이동합니다.\n" +
            "서류를 클릭하면 기존 시민증/2페이지 서류 화면이 열립니다.\n" +
            "닫기를 누르면 다시 책상으로 돌아갑니다.\n\n" +
            "기존 DAY/판정/저장/검색 코드는 변경하지 않았습니다.\n" +
            "Ctrl+S로 SampleScene을 저장해 주세요.", "확인");
    }

    private static Button MakeCloseButton(Transform panel, TMP_FontAsset font, string name)
    {
        var root = GetOrCreate(panel, name);
        var rect = (RectTransform)root;
        Undo.RecordObject(rect, "상세 화면 닫기 위치");
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -12f);
        rect.sizeDelta = new Vector2(122f, 49f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        var image = GetOrAdd<Image>(root.gameObject);
        image.color = new Color32(78, 58, 49, 245);
        image.raycastTarget = true;
        var button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = GetOrCreate(root, "Label");
        SetRect((RectTransform)label, Vector2.zero, new Vector2(115f, 45f));
        var txt = GetOrAdd<TextMeshProUGUI>(label.gameObject);
        if (font != null) txt.font = font;
        txt.text = "닫기 X";
        txt.fontSize = 27f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        return button;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static TMP_FontAsset FindTextFont(Canvas canvas)
    {
        foreach (var txt in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (txt.font != null && txt.name == "IDCardText") return txt.font;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/MulmaruMono SDF.asset");
    }

    private static Transform GetOrCreate(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name + " 생성");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void SetRect(RectTransform rect, Vector2 pos, Vector2 size)
    {
        Undo.RecordObject(rect, "UI 서류 위치");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Stretch(RectTransform rect)
    {
        Undo.RecordObject(rect, "책상 서류 루트");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
#endif
