#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds the DAY 11 priest lookup to an already-installed DAY 10 terminal.
/// Preserves the existing scene objects and their runtime click listeners.
/// Safe to run again without creating duplicate buttons.
/// </summary>
public static class MemoryOfficePriestSearchInstaller
{
    [MenuItem("기억관리국/UI/10. DAY 11 성직자 기록 검색 연결")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료",
                "Play를 끄고 SampleScene에서 실행해 주세요.", "확인");
            return;
        }

        MemoryOfficeDay10TerminalUI terminal =
            Object.FindFirstObjectByType<MemoryOfficeDay10TerminalUI>();
        if (terminal == null)
        {
            EditorUtility.DisplayDialog("검색 단말기 없음",
                "먼저 기억관리국 > UI > 8번 메뉴로 기존 검색 단말기를 설치해 주세요.", "확인");
            return;
        }

        Transform root = terminal.transform;
        Transform screen = root.Find("TerminalPopup/ArchiveScreen");
        if (screen == null)
        {
            EditorUtility.DisplayDialog("검색 단말기 구조 확인",
                "TerminalPopup/ArchiveScreen을 찾을 수 없습니다. 현재 UI 구조를 확인해 주세요.", "확인");
            return;
        }

        Transform death = screen.Find("Btn_DeathRegisterSearch");
        Transform incident = screen.Find("Btn_IncidentArchiveSearch");
        if (death == null || incident == null ||
            death.GetComponent<Button>() == null || incident.GetComponent<Button>() == null)
        {
            EditorUtility.DisplayDialog("조회 버튼 확인",
                "기존 사망자 명부/과거 사건 버튼이 보이지 않습니다. 8번 단말기를 먼저 설치해 주세요.", "확인");
            return;
        }

        TMP_Text templateLabel = death.GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset font = templateLabel != null ? templateLabel.font : TMP_Settings.defaultFontAsset;

        // Three buttons fit the existing ArchiveScreen's 950px wide content area.
        PlaceButton(death, -320f, 296f);
        PlaceButton(incident, 0f, 296f);
        FitLabel(death, 296f);
        FitLabel(incident, 296f);

        Transform existing = screen.Find("Btn_PriestRecordSearch");
        GameObject priest;
        if (existing != null)
        {
            priest = existing.gameObject;
        }
        else
        {
            priest = new GameObject("Btn_PriestRecordSearch", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(priest, "성직자 검색 버튼 추가");
            priest.transform.SetParent(screen, false);
        }
        priest.SetActive(true);
        PlaceButton(priest.transform, 320f, 296f);

        Image image = Require<Image>(priest);
        Undo.RecordObject(image, "성직자 버튼 색상");
        image.color = new Color(0.39f, 0.32f, 0.48f, 1f);
        image.raycastTarget = true;
        Button button = Require<Button>(priest);
        Undo.RecordObject(button, "성직자 버튼 설정");
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        // Runtime component manages listener registration, no persistent OnClick.

        Transform labelChild = priest.transform.Find("Label");
        GameObject labelObject;
        if (labelChild != null)
            labelObject = labelChild.gameObject;
        else
        {
            labelObject = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelObject, "성직자 버튼 글자 추가");
            labelObject.transform.SetParent(priest.transform, false);
        }
        TextMeshProUGUI label = Require<TextMeshProUGUI>(labelObject);
        Undo.RecordObject(label, "성직자 버튼 글자 설정");
        label.text = "성직자 기록 조회";
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        if (font != null) label.font = font;
        RectTransform lr = Require<RectTransform>(labelObject);
        Undo.RecordObject(lr, "성직자 글자 위치");
        lr.anchorMin = lr.anchorMax = new Vector2(.5f, .5f);
        lr.pivot = new Vector2(.5f, .5f);
        lr.anchoredPosition = Vector2.zero;
        lr.sizeDelta = new Vector2(282f, 64f);
        lr.localRotation = Quaternion.identity;
        lr.localScale = Vector3.one;

        Transform input = screen.Find("CitizenNumberInput/TextArea/Placeholder");
        if (input != null)
        {
            TMP_Text placeholder = input.GetComponent<TMP_Text>();
            if (placeholder != null)
            {
                Undo.RecordObject(placeholder, "검색 입력 안내");
                placeholder.text = "시민번호 또는 성직자";
                EditorUtility.SetDirty(placeholder);
            }
        }

        SerializedObject so = new SerializedObject(terminal);
        SerializedProperty property = so.FindProperty("priestSearchButton");
        if (property == null)
        {
            EditorUtility.DisplayDialog("스크립트 확인 필요",
                "새 MemoryOfficeDay10TerminalUI.cs가 적용되지 않았습니다. 압축 파일을 다시 병합해 주세요.", "확인");
            return;
        }
        property.objectReferenceValue = button;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(terminal);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = priest;
        EditorUtility.DisplayDialog("성직자 기록 조회 연결 완료",
            "DAY 11~17에서 검색어 '성직자'를 입력한 뒤 성직자 기록 조회를 누르세요.\n" +
            "기존 DAY 10 검색 기능은 그대로 유지됩니다.\n\n" +
            "Ctrl+S로 SampleScene을 저장해 주세요.", "확인");
    }

    private static void PlaceButton(Transform transform, float x, float width)
    {
        RectTransform rect = transform.GetComponent<RectTransform>();
        if (rect == null) return;
        Undo.RecordObject(rect, "검색 버튼 배치");
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 72f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void FitLabel(Transform parent, float width)
    {
        TMP_Text text = parent.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        Undo.RecordObject(text, "검색 버튼 글자 크기");
        text.fontSize = 23f;
        text.enableWordWrapping = false;
        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, "검색 버튼 글자 폭");
            rect.sizeDelta = new Vector2(width - 12f, 62f);
            EditorUtility.SetDirty(rect);
        }
        EditorUtility.SetDirty(text);
    }

    private static T Require<T>(GameObject go) where T : Component
    {
        T found = go.GetComponent<T>();
        return found != null ? found : Undo.AddComponent<T>(go);
    }
}
#endif
