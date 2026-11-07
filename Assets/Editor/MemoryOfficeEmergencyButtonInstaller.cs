#if UNITY_EDITOR
using ProjectED.Examination;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 Canvas / PNG 15장 / 승인·거절 기능을 유지하면서
/// 오른쪽 긴급 버튼(2026.10.01_7.png) 위치에 투명 클릭 영역을 설치한다.
/// </summary>
public static class MemoryOfficeEmergencyButtonInstaller
{
    private const string LayerRoot = "MemoryOffice_Design_Layers";
    private const string ActionRoot = "MemoryOffice_ActionButtons";
    private const string ButtonName = "Btn_Emergency_Detention_OneClick";

    [MenuItem("기억관리국/UI/6. 오른쪽 긴급 구금 버튼 연결")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play를 끄고 SampleScene을 열어 주세요.", "확인");
            return;
        }

        ExaminationController controller = null;
        Canvas canvas = null;
        foreach (var candidate in Object.FindObjectsByType<ExaminationController>(FindObjectsSortMode.None))
        {
            var candidateCanvas = candidate.GetComponent<Canvas>();
            if (candidateCanvas == null)
                candidateCanvas = candidate.GetComponentInParent<Canvas>();
            if (candidateCanvas == null || candidateCanvas.transform.Find(LayerRoot) == null)
                continue;
            controller = candidate;
            canvas = candidateCanvas;
            break;
        }

        if (canvas == null || controller == null)
        {
            EditorUtility.DisplayDialog("게임 Canvas가 없음",
                "SampleScene을 열고 Canvas 안에 MemoryOffice_Design_Layers가 있는지 확인해 주세요.", "확인");
            return;
        }

        Transform art = canvas.transform.Find(LayerRoot + "/11_긴급버튼");
        if (art == null || art.GetComponent<Image>() == null)
        {
            EditorUtility.DisplayDialog("긴급 버튼 이미지가 없음",
                "Canvas > MemoryOffice_Design_Layers > 11_긴급버튼 이미지가 필요합니다.", "확인");
            return;
        }

        Transform holder = canvas.transform.Find(ActionRoot);
        if (holder == null)
        {
            EditorUtility.DisplayDialog("원클릭 버튼 그룹이 없음",
                "먼저 '기억관리국 > UI > 3. 새 승인·거절 버튼 원클릭 판정 연결'을 실행해 주세요.", "확인");
            return;
        }

        Transform found = holder.Find(ButtonName);
        GameObject go;
        if (found != null)
        {
            go = found.gameObject;
        }
        else
        {
            go = new GameObject(ButtonName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "긴급 구금 클릭 버튼 생성");
            go.transform.SetParent(holder, false);
        }

        // 원본 시안: 긴급 버튼 이미지 알파 범위 x1491..1642, y210..361.
        // 검정/노랑 패널과 빨간 원을 포함하는 클릭 영역.
        var rect = go.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "긴급 버튼 클릭 영역 위치");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(145f, 145f);
        rect.anchoredPosition = new Vector2(1566f - 960f, 540f - 285f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(go);
        Undo.RecordObject(image, "긴급 버튼 투명 영역");
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        var button = go.GetComponent<Button>();
        if (button == null) button = Undo.AddComponent<Button>(go);
        Undo.RecordObject(button, "긴급 버튼 입력 상태");
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.interactable = false; // Play 중 DAY 7이 시작되면 활성화.

        var handler = go.GetComponent<MemoryOfficeEmergencyDetentionButton>();
        if (handler == null)
            handler = Undo.AddComponent<MemoryOfficeEmergencyDetentionButton>(go);
        var properties = new SerializedObject(handler);
        properties.FindProperty("examinationController").objectReferenceValue = controller;
        properties.FindProperty("emergencyButton").objectReferenceValue = button;
        properties.ApplyModifiedProperties();

        // 긴급 버튼 PNG는 그대로 사용하며, 독립 버튼이 클릭만 처리한다.
        var artImage = art.GetComponent<Image>();
        Undo.RecordObject(artImage, "긴급 버튼 이미지 클릭 차단 해제");
        artImage.raycastTarget = false;

        // 기존 구금 UI는 참조를 보존한 채 비활성화해 이중 입력을 방지한다.
        foreach (var old in canvas.GetComponentsInChildren<Button>(true))
        {
            if (old.name != "Btn_Detain") continue;
            Undo.RecordObject(old.gameObject, "기존 구금 UI 비활성화");
            old.gameObject.SetActive(false);
            break;
        }

        go.SetActive(true);
        holder.SetAsLastSibling();
        go.transform.SetAsLastSibling();
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = go;

        Debug.Log("[기억관리국 UI] 긴급 버튼 연결 완료: DAY 7부터 한 번 클릭하면 피검자 구금 후 다음 시민으로 진행합니다.");
        EditorUtility.DisplayDialog("긴급 구금 버튼 연결 완료",
            "DAY 1~6: 긴급 버튼 비활성화\n" +
            "DAY 7 이후: 긴급 버튼 클릭 시 즉시 구금 판정 및 다음 시민 진행\n" +
            "게임 오버/엔딩 시에는 진행하지 않습니다.\n\n" +
            "기존 구금 버튼은 삭제하지 않고 숨겼습니다.\n" +
            "Ctrl + S로 씬을 저장해 주세요.", "확인");
    }
}
#endif
