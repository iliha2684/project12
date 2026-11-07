#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 기존 15개 Image와 원클릭 승인/거절 버튼을 보존하고
/// 상단 검은 모니터 위치에 숫자 전용 TextMeshProUGUI를 추가한다.
/// </summary>
public static class MemoryOfficeRemainingCounterInstaller
{
    private const string CounterName = "MemoryOffice_RemainingCitizenCounter";

    [MenuItem("기억관리국/UI/4. 상단 검은 화면에 남은 사람 수 표시")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play를 끈 후 실행해 주세요.", "확인");
            return;
        }

        GameObject canvasObject = GameObject.Find("Canvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas == null || canvas.transform.Find("MemoryOffice_Design_Layers") == null)
        {
            EditorUtility.DisplayDialog("새 Canvas 디자인이 없음",
                "SampleScene을 열고 Canvas 안에 MemoryOffice_Design_Layers가 있는지 확인해 주세요.", "확인");
            return;
        }

        ExaminationController controller = canvas.GetComponent<ExaminationController>();
        if (controller == null) controller = canvas.GetComponentInChildren<ExaminationController>(true);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("심사 컨트롤러가 없음",
                "현재 Canvas에서 ExaminationController를 찾지 못했습니다. SampleScene을 확인해 주세요.", "확인");
            return;
        }

        Transform existing = canvas.transform.Find(CounterName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            Undo.RecordObject(go, "남은 인원 글자 재설정");
        }
        else
        {
            go = new GameObject(CounterName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "남은 인원 글자 생성");
            go.transform.SetParent(canvas.transform, false);
        }
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        RectTransform rect = go.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "상단 모니터 글자 위치 설정");
        // 원본 시안 1920 x 1080의 위쪽 검은 화면 내부(가로 약 1170~1545px).
        // CanvasScaler Reference Resolution 역시 1920 x 1080이다.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(1358f - 960f, 540f - 46f);
        rect.sizeDelta = new Vector2(350f, 68f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (label == null) label = Undo.AddComponent<TextMeshProUGUI>(go);
        Undo.RecordObject(label, "남은 사람 텍스트 표시 설정");
        label.text = "남은 사람 0명";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 32f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color32(231, 252, 234, 255);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        // 기존 한글 폰트 설정을 재사용한다. 플레이 시작 시 ExaminationController가
        // Canvas 하위 TMP_Text 전체에 한글 런타임 폰트를 적용하는 구조이다.
        TMP_Text status = null;
        foreach (TMP_Text item in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (item != label && item.name == "StatusText")
            {
                status = item;
                break;
            }
        }
        if (status != null && status.font != null)
            label.font = status.font;
        else if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        var display = go.GetComponent<MemoryOfficeRemainingCitizenDisplay>();
        if (display == null) display = Undo.AddComponent<MemoryOfficeRemainingCitizenDisplay>(go);
        var serialized = new SerializedObject(display);
        serialized.FindProperty("examinationController").objectReferenceValue = controller;
        serialized.FindProperty("counterText").objectReferenceValue = label;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = go;
        Debug.Log("[기억관리국 UI] 상단 검은 모니터에 남은 사람 수 표시 연결 완료");
        EditorUtility.DisplayDialog("남은 사람 수 표시 완료",
            "상단 검은 화면에 '남은 사람 N명'을 표시하도록 연결했습니다.\n" +
            "시민의 판정이 확정될 때마다 1명씩 감소하고, DAY가 바뀌면 갱신됩니다.\n\n" +
            "Ctrl + S로 SampleScene을 저장해 주세요.", "확인");
    }
}
#endif
