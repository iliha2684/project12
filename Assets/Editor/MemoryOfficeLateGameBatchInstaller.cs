#if UNITY_EDITOR
using System.Collections.Generic;
using ProjectED.Days;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Installs existing DAY13-18 logic presentation without replacing any authored scene,
/// GameManager, Canvas, or .meta file. Safe to run twice.
/// </summary>
public static class MemoryOfficeLateGameBatchInstaller
{
    private const string ScenePath = "Assets/Scenes/EndingScene.unity";
    private const string NoticeName = "MemoryOffice_LateDayNotice";
    private const string ResultCanvasName = "MemoryOffice_EndingResultCanvas";

    [MenuItem("기억관리국/UI/13. DAY 13~18 이벤트 및 엔딩 화면 일괄 연결")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드를 꺼 주세요", "Play 모드 종료 후 SampleScene에서 설치해 주세요.", "확인");
            return;
        }
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            EditorUtility.DisplayDialog("씬 확인", "SampleScene을 먼저 열어 주세요.", "확인");
            return;
        }

        var game = Object.FindFirstObjectByType<GameManager>();
        var campaign = Object.FindFirstObjectByType<CampaignManager>();
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (game == null || campaign == null || canvas == null || game.GetComponent<EndingManager>() == null)
        {
            EditorUtility.DisplayDialog("오브젝트 연결 확인",
                "SampleScene에서 GameManager + EndingManager, CampaignManager, Canvas를 찾지 못했습니다.", "확인");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("EndingScene 누락", ScenePath + " 경로에 기존 씬이 없습니다.", "확인");
            return;
        }

        if (game.GetComponent<MemoryOfficeEndingSceneBridge>() == null)
            Undo.AddComponent<MemoryOfficeEndingSceneBridge>(game.gameObject);
        CreateOrUpdateLateNotice(canvas);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        // Save the active scene before touching an additive scene.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[DAY13-18] 사용자가 저장을 취소하여 EndingScene 변경을 진행하지 않았습니다.");
            return;
        }

        bool installedEnding = CreateOrUpdateEndingScene();
        if (!installedEnding) return;
        EnsureBuildScene();
        EditorUtility.DisplayDialog("DAY 13~18 일괄 연결 완료",
            "DAY 14 점검 결과·DAY 17 국장 방문 예고 UI, 엔딩 판정 후 EndingScene 이동, 7종 결과 표시를 연결했습니다.\n" +
            "기존 DAY 13 백색서고 팝업과 DAY 14~16 판정 코드는 유지했습니다.\n\n" +
            "DAY 16 특수 시민의 이름·서류·대사 원본은 비어 있으므로 팀의 기획 데이터가 필요합니다.\n" +
            "DAY 17 단말기에서 '차장'을 검색할 수 있습니다.\n\n" +
            "Inspector 특별상 보상 기본값은 신뢰도/정신력 각 +10이며 임시 밸런스입니다.", "확인");
    }

    [MenuItem("기억관리국/검사/DAY 13~18 데이터 연결 점검")]
    public static void ValidateLateDays()
    {
        var findings = new List<string>();
        for (int day = 13; day <= 18; day++)
        {
            string path = "Assets/Data/DAY1~18/Day_" + day.ToString("00") + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<DayDefinition>(path);
            if (asset == null)
            {
                findings.Add("DAY " + day + ": DayDefinition 파일 누락");
                continue;
            }
            if (day < 18)
            {
                var issues = DayDefinitionValidator.Validate(asset);
                foreach (var issue in issues)
                    findings.Add("DAY " + day + ": " + issue.Message);
            }
        }
        var game = Object.FindFirstObjectByType<GameManager>();
        if (game != null)
        {
            var late = game.GetComponent<LateDayEventController>();
            if (late == null || late.PleadingCitizenCase == null)
                findings.Add("DAY 16 특수 시민이 LateDayEventController에 연결되지 않았습니다.");
            else
            {
                var inspector = new SerializedObject(late.PleadingCitizenCase);
                var citizen = inspector.FindProperty("citizen");
                if (citizen == null || citizen.objectReferenceValue == null)
                    findings.Add("DAY 16 특수 시민의 시민 프로필이 비어 있습니다. 기획 데이터 필요.");
            }
        }
        string message = findings.Count == 0
            ? "DAY 13~18 DayDefinition 정적 검사에서 오류/경고를 찾지 못했습니다. 실제 실행 검증은 별도로 필요합니다."
            : string.Join("\n", findings);
        Debug.Log("[DAY13-18 점검]\n" + message);
        EditorUtility.DisplayDialog("DAY 13~18 데이터 정적 검사", message, "확인");
    }

    private static void CreateOrUpdateLateNotice(Canvas canvas)
    {
        GameObject root = EnsureChild(canvas.transform, NoticeName, true);
        root.transform.SetAsLastSibling();
        SetFullScreen(root.GetComponent<RectTransform>());
        Image backdrop = Require<Image>(root);
        backdrop.color = new Color(0.04f, 0.045f, 0.05f, 0.65f);
        backdrop.raycastTarget = true;
        CanvasGroup group = Require<CanvasGroup>(root);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject card = EnsureChild(root.transform, "NoticeCard", true);
        Place(card.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(800f, 330f));
        Image cardImg = Require<Image>(card);
        cardImg.color = new Color(0.14f, 0.17f, 0.18f, 1f);
        cardImg.raycastTarget = true;
        TMP_Text title = MakeText(card.transform, "Heading", "차장 점검", new Vector2(0f, 105f),
            new Vector2(750f, 65f), 36f, new Color(0.89f, 0.95f, 0.86f), FontStyles.Bold);
        TMP_Text body = MakeText(card.transform, "Body", "진행 결과", new Vector2(0f, 13f),
            new Vector2(725f, 125f), 30f, Color.white, FontStyles.Normal);
        Button close = MakeButton(card.transform, "Btn_LateNoticeClose", "확인", new Vector2(0f, -115f));
        var component = Require<MemoryOfficeLateDayNoticeUI>(root);
        var so = new SerializedObject(component);
        so.FindProperty("panel").objectReferenceValue = group;
        so.FindProperty("heading").objectReferenceValue = title;
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.ApplyModifiedPropertiesWithoutUndo();
        close.onClick = new Button.ButtonClickedEvent();
        EditorUtility.SetDirty(root);
    }

    private static bool CreateOrUpdateEndingScene()
    {
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject root = null;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == ResultCanvasName) { root = obj; break; }
            }
            if (root == null)
            {
                root = new GameObject(ResultCanvasName, typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, "엔딩 화면 생성");
            }
            Canvas c = Require<Canvas>(root);
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = Require<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            SetFullScreen(root.GetComponent<RectTransform>());
            GameObject background = EnsureChild(root.transform, "ResultBackground", true);
            SetFullScreen(background.GetComponent<RectTransform>());
            Image bg = Require<Image>(background);
            bg.color = new Color(0.035f, 0.05f, 0.055f, 1f);
            bg.raycastTarget = false;
            TMP_Text title = MakeText(background.transform, "EndingTitle", "ENDING", new Vector2(0f, 125f),
                new Vector2(1720f, 160f), 70f, new Color(0.87f, 0.91f, 0.79f), FontStyles.Bold);
            TMP_Text detail = MakeText(background.transform, "EndingDetail", "결과를 준비 중입니다.", new Vector2(0f, -40f),
                new Vector2(1640f, 215f), 36f, new Color(0.84f, 0.84f, 0.83f), FontStyles.Normal);
            var presenter = Require<MemoryOfficeEndingScenePresenter>(root);
            SerializedObject so = new SerializedObject(presenter);
            so.FindProperty("heading").objectReferenceValue = title;
            so.FindProperty("detail").objectReferenceValue = detail;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }
        catch (System.Exception error)
        {
            Debug.LogError("EndingScene 설정에 실패했습니다: " + error);
            return false;
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void EnsureBuildScene()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var scene in scenes)
        {
            if (scene.path == ScenePath)
            {
                scene.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject EnsureChild(Transform parent, string name, bool withImage)
    {
        Transform old = parent.Find(name);
        if (old != null)
        {
            old.gameObject.SetActive(true);
            return old.gameObject;
        }
        GameObject obj = withImage
            ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            : new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "엔딩/UI 요소 생성");
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static T Require<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static TMP_Text MakeText(Transform parent, string name, string content,
        Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        GameObject obj = EnsureChild(parent, name, false);
        Place(obj.GetComponent<RectTransform>(), position, size);
        var text = Require<TextMeshProUGUI>(obj);
        text.font = TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject obj = EnsureChild(parent, name, true);
        Place(obj.GetComponent<RectTransform>(), position, new Vector2(270f, 75f));
        Image image = Require<Image>(obj);
        image.color = new Color(0.29f, 0.41f, 0.35f, 1f);
        image.raycastTarget = true;
        Button button = Require<Button>(obj);
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        MakeText(obj.transform, "Label", label, Vector2.zero, new Vector2(250f, 60f),
            30f, Color.white, FontStyles.Bold);
        return button;
    }

    private static void SetFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
#endif
