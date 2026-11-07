#if UNITY_EDITOR
using ProjectED.Examination;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이미 배치한 15개의 PNG 이미지와 기존 심사 코드를 보존하면서
/// 새 승인/거절 그림 위에 독립 Button 2개를 설치한다.
/// </summary>
public static class MemoryOfficeOneClickUIInstaller
{
    private const string RootName = "MemoryOffice_Design_Layers";
    private const string ButtonRootName = "MemoryOffice_ActionButtons";

    [MenuItem("기억관리국/UI/3. 새 승인·거절 버튼 원클릭 판정 연결")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 종료하고 설치해 주세요.", "확인");
            return;
        }

        // 다른 Canvas가 선택돼도 SampleScene의 기존 심사 Canvas를 우선한다.
        var controllers = Object.FindObjectsByType<ExaminationController>(FindObjectsSortMode.None);
        ExaminationController controller = null;
        Canvas canvas = null;
        foreach (var candidate in controllers)
        {
            var candidateCanvas = candidate.GetComponent<Canvas>();
            if (candidateCanvas != null && candidateCanvas.transform.Find(RootName) != null)
            {
                controller = candidate;
                canvas = candidateCanvas;
                break;
            }
        }

        if (controller == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas를 찾을 수 없음",
                "SampleScene을 열고 '기억관리국 > UI > 1. PNG 15개를 Canvas에 개별 배치'를 먼저 실행해 주세요.", "확인");
            return;
        }

        Transform layers = canvas.transform.Find(RootName);
        Image rejectBase = FindImage(layers, "12_거절버튼_기본");
        Image approveBase = FindImage(layers, "13_승인버튼_기본");
        Image rejectPressed = FindImage(layers, "14_거절버튼_눌림_대체");
        Image approvePressed = FindImage(layers, "15_승인버튼_눌림_대체");
        if (rejectBase == null || approveBase == null || rejectPressed == null || approvePressed == null)
        {
            EditorUtility.DisplayDialog("버튼 이미지 없음",
                "MemoryOffice_Design_Layers 안의 빨강/초록 기본·눌림 이미지 4개를 찾지 못했어요.\n15개 이미지 배치를 먼저 확인해 주세요.", "확인");
            return;
        }

        Transform existing = canvas.transform.Find(ButtonRootName);
        if (existing != null && !EditorUtility.DisplayDialog("이미 연결됨",
                "기존 'MemoryOffice_ActionButtons'를 삭제하지 않고 위치와 참조를 다시 설정할까요?", "다시 연결", "취소"))
            return;

        Transform holder = existing;
        if (holder == null)
        {
            var go = new GameObject(ButtonRootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "새 UI 버튼 루트 만들기");
            holder = go.transform;
            holder.SetParent(canvas.transform, false);
        }
        Stretch((RectTransform)holder);
        holder.SetAsLastSibling(); // 각 이미지와 기존 버튼보다 위쪽에서 클릭 처리.
        holder.gameObject.SetActive(true);

        Button rejection = BuildHitbox(holder, "Btn_Red_Reject_OneClick", 39f, 723f, 204f, 881f,
            rejectBase, rejectPressed);
        Button approval = BuildHitbox(holder, "Btn_Green_Approve_OneClick", 237f, 723f, 402f, 881f,
            approveBase, approvePressed);

        var bridge = holder.GetComponent<MemoryOfficeInstantDecisionBridge>();
        if (bridge == null) bridge = Undo.AddComponent<MemoryOfficeInstantDecisionBridge>(holder.gameObject);

        // OnClick 직렬화 대신 Bridge의 OnEnable 리스너 등록으로 중복 판정 방지.
        SerializedObject so = new SerializedObject(bridge);
        so.FindProperty("examinationController").objectReferenceValue = controller;
        so.FindProperty("approvalButton").objectReferenceValue = approval;
        so.FindProperty("rejectionButton").objectReferenceValue = rejection;
        so.ApplyModifiedProperties();

        // 원래 판정 선택 UI는 참조를 살려둔 채 그림과 클릭만 비활성화.
        // 구금/치료/다음 시민 버튼과 서류 UI는 그대로 둔다.
        HideOldButton(canvas, "Btn_Approve");
        HideOldButton(canvas, "Btn_Reject");
        Selection.activeGameObject = holder.gameObject;
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[기억관리국 UI] 새 빨강/초록 버튼 한 번 클릭 = 판정 선택 + 확정 연결 완료. 다음 시민은 기존 버튼을 사용합니다.");
        EditorUtility.DisplayDialog("원클릭 판정 연결 완료",
            "초록 버튼: 추출 승인 즉시 확정\n빨간 버튼: 서류 반려 즉시 확정\n\n기존 DAY/오류/NPC/엔딩 판정은 그대로 사용합니다.\n결과 확인 후 다음 시민은 기존 '다음 시민' 버튼으로 넘겨 주세요.\n\nCtrl + S로 씬을 저장해 주세요.", "확인");
    }

    private static Button BuildHitbox(Transform holder, string name,
        float x1, float y1, float x2, float y2, Image normal, Image pressed)
    {
        Transform found = holder.Find(name);
        GameObject go;
        if (found != null)
            go = found.gameObject;
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "UI 투명 클릭 버튼 추가");
            go.transform.SetParent(holder, false);
        }
        go.SetActive(true);
        var rect = go.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "원클릭 UI 버튼 좌표");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = new Vector2(x2 - x1, y2 - y1);
        rect.anchoredPosition = new Vector2((x1 + x2) * 0.5f - 960f, 540f - (y1 + y2) * 0.5f);
        var img = go.GetComponent<Image>();
        Undo.RecordObject(img, "원클릭 UI 클릭 영역");
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = true;
        var button = go.GetComponent<Button>();
        Undo.RecordObject(button, "원클릭 UI 버튼 상태");
        button.targetGraphic = img;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.interactable = false;

        var visual = go.GetComponent<MemoryOfficeButtonPressVisual>();
        if (visual == null) visual = Undo.AddComponent<MemoryOfficeButtonPressVisual>(go);
        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("normalImage").objectReferenceValue = normal;
        so.FindProperty("pressedImage").objectReferenceValue = pressed;
        so.ApplyModifiedProperties();
        normal.raycastTarget = false;
        pressed.raycastTarget = false;
        normal.gameObject.SetActive(true);
        pressed.gameObject.SetActive(false);
        go.transform.SetAsLastSibling();
        return button;
    }

    private static void HideOldButton(Canvas canvas, string name)
    {
        foreach (var b in canvas.GetComponentsInChildren<Button>(true))
        {
            if (b.name != name) continue;
            var img = b.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "원래 버튼 그림 숨김");
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = false;
            }
            foreach (var graphic in b.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == img) continue;
                Undo.RecordObject(graphic, "원래 버튼 글자 숨김");
                graphic.enabled = false;
            }
            Undo.RecordObject(b, "원래 버튼 키보드 탐색 끄기");
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            break;
        }
    }

    private static Image FindImage(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
#endif
