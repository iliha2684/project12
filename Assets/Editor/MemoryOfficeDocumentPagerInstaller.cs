#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reuses Paper and its existing paging buttons, replacing only the paper
/// display with 2 sheets x 2 independently scrollable document panels.
/// Rerunning is safe: object names and component instances are reused.
/// </summary>
public static class MemoryOfficeDocumentPagerInstaller
{
    [MenuItem("기억관리국/UI/9. 서류 이전·다음 페이지 버튼 설치")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료",
                "Play를 끄고 SampleScene에서 설치해 주세요.", "확인");
            return;
        }

        ExaminationController examination = null;
        Transform paper = null;
        foreach (var candidate in Object.FindObjectsByType<ExaminationController>(FindObjectsSortMode.None))
        {
            var target = candidate.transform.Find("Paper");
            if (target == null || target.Find("PaperText") == null) continue;
            examination = candidate;
            paper = target;
            break;
        }
        if (paper == null)
        {
            EditorUtility.DisplayDialog("서류를 찾을 수 없습니다",
                "SampleScene의 Canvas에 Paper/PaperText가 있는지 확인해 주세요.", "확인");
            return;
        }

        var original = paper.Find("PaperText").GetComponent<TextMeshProUGUI>();
        if (original == null)
        {
            EditorUtility.DisplayDialog("PaperText 없음", "PaperText에 TextMeshProUGUI가 필요합니다.", "확인");
            return;
        }

        // ExaminationController still writes the full original document here.
        // Hide only its rendering; never deactivate or delete the component.
        Undo.RecordObject(original, "기존 서류 데이터 소스 보존");
        var hidden = original.color;
        hidden.a = 0f;
        original.color = hidden;
        original.raycastTarget = false;

        var top = MakeDocumentPanel(paper, "MemoryOffice_TopDocument",
            new Vector2(0f, .54f), new Vector2(1f, .95f), original.font);
        var bottom = MakeDocumentPanel(paper, "MemoryOffice_BottomDocument",
            new Vector2(0f, .16f), new Vector2(1f, .51f), original.font);

        var dividerGo = GetOrCreate(paper, "MemoryOffice_DocumentDivider");
        var dividerRect = dividerGo.GetComponent<RectTransform>();
        SetCentered(dividerRect, new Vector2(0f, 20f), new Vector2(384f, 2f));
        var divider = GetOrAdd<Image>(dividerGo);
        Undo.RecordObject(divider, "서류 구분선");
        divider.color = new Color32(138, 120, 101, 170);
        divider.raycastTarget = false;

        var nav = GetOrCreate(paper, "MemoryOffice_PaperPageControls");
        SetBottom(nav.GetComponent<RectTransform>(), 62f, 35f);
        var prev = MakeButton(nav.transform, "Btn_PreviousPaperPage", -128f,
            "◀ 이전", new Color32(76, 63, 53, 255), original.font);
        var next = MakeButton(nav.transform, "Btn_NextPaperPage", 128f,
            "다음 ▶", new Color32(76, 63, 53, 255), original.font);

        var pageLabelGo = GetOrCreate(nav.transform, "PaperPageCounter");
        SetCentered(pageLabelGo.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(90f, 51f));
        var pageLabel = GetOrAdd<TextMeshProUGUI>(pageLabelGo);
        Undo.RecordObject(pageLabel, "서류 페이지 번호");
        pageLabel.text = "1 / 2";
        pageLabel.font = original.font;
        pageLabel.fontSize = 23f;
        pageLabel.color = new Color32(67, 53, 44, 255);
        pageLabel.alignment = TextAlignmentOptions.Center;
        pageLabel.raycastTarget = false;
        pageLabel.enableWordWrapping = false;

        // The controls and panels must be above the original paper image.
        top.gameObject.transform.SetAsLastSibling();
        bottom.gameObject.transform.SetAsLastSibling();
        dividerGo.transform.SetAsLastSibling();
        nav.transform.SetAsLastSibling();

        var pager = GetOrAdd<MemoryOfficeDocumentPager>(paper.gameObject);
        var data = new SerializedObject(pager);
        data.FindProperty("examination").objectReferenceValue = examination;
        data.FindProperty("paperText").objectReferenceValue = original;
        data.FindProperty("navigationRoot").objectReferenceValue = nav;
        data.FindProperty("previousButton").objectReferenceValue = prev;
        data.FindProperty("nextButton").objectReferenceValue = next;
        data.FindProperty("pageNumberText").objectReferenceValue = pageLabel;
        data.FindProperty("upperDocumentText").objectReferenceValue = top.GetComponentInChildren<TextMeshProUGUI>();
        data.FindProperty("lowerDocumentText").objectReferenceValue = bottom.GetComponentInChildren<TextMeshProUGUI>();
        data.FindProperty("upperDocumentScroll").objectReferenceValue = top;
        data.FindProperty("lowerDocumentScroll").objectReferenceValue = bottom;
        data.ApplyModifiedProperties();

        EditorUtility.SetDirty(paper.gameObject);
        EditorSceneManager.MarkSceneDirty(paper.gameObject.scene);
        Selection.activeGameObject = paper.gameObject;
        EditorUtility.DisplayDialog("2페이지 서류 화면 설치 완료",
            "1페이지: 기억 제거 신청서 + 의사 처방전\n" +
            "2페이지: 기억 구조 검사표 + 관계인 동의서\n\n" +
            "내용이 긴 서류는 해당 영역에서 마우스 휠로 읽을 수 있습니다.\n" +
            "새 시민이 오면 1페이지로 돌아갑니다.\n\n" +
            "Ctrl+S로 SampleScene을 저장해 주세요.", "확인");
    }

    private static ScrollRect MakeDocumentPanel(Transform paper, string name,
        Vector2 minAnchor, Vector2 maxAnchor, TMP_FontAsset font)
    {
        var panel = GetOrCreate(paper, name);
        var rect = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "서류 영역 위치");
        rect.anchorMin = minAnchor;
        rect.anchorMax = maxAnchor;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(38f, 0f);
        rect.offsetMax = new Vector2(-38f, 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        var mask = GetOrAdd<RectMask2D>(panel);
        Undo.RecordObject(mask, "긴 서류의 영역 마스킹");

        var documentGo = GetOrCreate(panel.transform, "DocumentText");
        var documentRect = documentGo.GetComponent<RectTransform>();
        Undo.RecordObject(documentRect, "서류 텍스트 위치");
        documentRect.anchorMin = new Vector2(0f, 1f);
        documentRect.anchorMax = new Vector2(1f, 1f);
        documentRect.pivot = new Vector2(.5f, 1f);
        documentRect.anchoredPosition = Vector2.zero;
        documentRect.sizeDelta = new Vector2(0f, 300f);
        documentRect.localScale = Vector3.one;
        var text = GetOrAdd<TextMeshProUGUI>(documentGo);
        Undo.RecordObject(text, "서류 본문 글꼴");
        text.text = string.Empty;
        text.font = font;
        text.fontSize = 22f;
        text.color = new Color32(58, 41, 34, 255);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = true;

        var scroll = GetOrAdd<ScrollRect>(panel);
        Undo.RecordObject(scroll, "서류 스크롤");
        scroll.viewport = rect;
        scroll.content = documentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        scroll.inertia = false;
        return scroll;
    }

    private static GameObject GetOrCreate(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name + " 생성");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetBottom(RectTransform rect, float height, float bottom)
    {
        Undo.RecordObject(rect, "서류 버튼 영역");
        rect.anchorMin = new Vector2(.5f, 0f);
        rect.anchorMax = new Vector2(.5f, 0f);
        rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
        rect.sizeDelta = new Vector2(425f, height);
        rect.localScale = Vector3.one;
    }

    private static void SetCentered(RectTransform rect, Vector2 pos, Vector2 size)
    {
        Undo.RecordObject(rect, "서류 버튼 위치");
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static Button MakeButton(Transform parent, string name, float x,
        string caption, Color background, TMP_FontAsset font)
    {
        var go = GetOrCreate(parent, name);
        SetCentered(go.GetComponent<RectTransform>(), new Vector2(x, 0f),
            new Vector2(133f, 54f));
        var image = GetOrAdd<Image>(go);
        Undo.RecordObject(image, "서류 버튼 이미지");
        image.color = background;
        image.raycastTarget = true;
        var button = GetOrAdd<Button>(go);
        Undo.RecordObject(button, "서류 이전 다음 버튼");
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        // The runtime pager owns onClick; do not add persistent callbacks.
        var label = GetOrCreate(go.transform, "Label");
        SetCentered(label.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(125f, 50f));
        var text = GetOrAdd<TextMeshProUGUI>(label);
        Undo.RecordObject(text, "서류 버튼 텍스트");
        text.font = font;
        text.fontSize = 23f;
        text.text = caption;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return button;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        var existing = gameObject.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(gameObject);
    }
}
#endif
