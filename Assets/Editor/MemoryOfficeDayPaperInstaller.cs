#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Add the date/day label on the upper-left paper in the existing Canvas.
/// Preserves all 15 image layers and prior input/button components.
/// </summary>
public static class MemoryOfficeDayPaperInstaller
{
    private const string LabelName = "MemoryOffice_DayPaperDate";

    [MenuItem("기억관리국/UI/5. 왼쪽 종이에 날짜와 DAY 표시")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play를 끈 다음 실행해 주세요.", "확인");
            return;
        }

        GameObject canvasObject = GameObject.Find("Canvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas == null || canvas.transform.Find("MemoryOffice_Design_Layers") == null)
        {
            EditorUtility.DisplayDialog("새 UI가 없습니다",
                "SampleScene을 열고 Canvas 아래 MemoryOffice_Design_Layers가 있는지 확인해 주세요.", "확인");
            return;
        }

        Transform existing = canvas.transform.Find(LabelName);
        GameObject labelObject;
        if (existing != null)
        {
            labelObject = existing.gameObject;
            Undo.RecordObject(labelObject, "업무 기록 표시 갱신");
        }
        else
        {
            labelObject = new GameObject(LabelName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "날짜 표시 추가");
            labelObject.transform.SetParent(canvas.transform, false);
        }
        labelObject.SetActive(true);
        labelObject.transform.SetAsLastSibling();

        // Coordinates relative to original 1920x1080 reference artwork:
        // inside the paper at x~35-225, y~38-300.
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "날짜 글자 위치");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(132f - 960f, 540f - 163f);
        rect.sizeDelta = new Vector2(174f, 220f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(labelObject);
        Undo.RecordObject(text, "날짜 글자 스타일");
        text.text = "<size=23><b>기억관리국</b></size>\n<size=19>업무 기록</size>\n\n" +
            "<size=40><b>DAY 01</b></size>\n<size=21>심사 1일차</size>";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Normal;
        text.color = new Color32(53, 46, 39, 255);
        text.enableWordWrapping = false;
        text.richText = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        // Follow the font used by the existing examination UI (Korean glyph support).
        TMP_Text existingStatus = null;
        foreach (TMP_Text item in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (item != text && item.name == "StatusText")
            {
                existingStatus = item;
                break;
            }
        }
        if (existingStatus != null && existingStatus.font != null)
            text.font = existingStatus.font;
        else if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        MemoryOfficeDayPaperDisplay display = labelObject.GetComponent<MemoryOfficeDayPaperDisplay>();
        if (display == null) display = Undo.AddComponent<MemoryOfficeDayPaperDisplay>(labelObject);

        EditorUtility.SetDirty(labelObject);
        EditorUtility.SetDirty(text);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = labelObject;

        Debug.Log("[기억관리국 UI] 왼쪽 종이에 DAY/날짜 표시가 연결되었습니다.");
        EditorUtility.DisplayDialog("날짜 표시 준비 완료",
            "왼쪽 위 종이에 DAY 번호와 심사 일차가 표시됩니다.\n" +
            "현재 DAY가 바뀌면 자동으로 갱신됩니다.\n\n" +
            "실제 연월일을 사용하려면 MemoryOfficeDayPaperDisplay의 Day One Calendar Date에 yyyy-MM-dd를 입력해 주세요.\n" +
            "(예: 팀에서 정한 DAY 1 날짜)\n\n" +
            "Ctrl + S로 SampleScene을 저장해 주세요.", "확인");
    }
}
#endif
