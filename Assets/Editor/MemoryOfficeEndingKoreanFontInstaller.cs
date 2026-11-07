#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Assign an existing Korean TMP font to the already-created EndingScene UI.
/// Does not recreate the Canvas or change any GameManager/ending logic.
/// </summary>
public static class MemoryOfficeEndingKoreanFontInstaller
{
    private const string FontPath = "Assets/Fonts/TMP/MulmaruMono SDF.asset";
    private const string RootName = "MemoryOffice_EndingResultCanvas";

    [MenuItem("기억관리국/UI/15. 엔딩 화면 한글 폰트 적용")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 끈 후 실행해 주세요.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "EndingScene")
        {
            EditorUtility.DisplayDialog("씬 확인", "Assets/Scenes/EndingScene.unity를 먼저 열어 주세요.", "확인");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("폰트 파일 확인", 
                "한글 TMP 폰트가 없습니다.\n" + FontPath +
                "\nFonts.zip의 Fonts 폴더를 Assets 안에 병합했는지 확인해 주세요.", "확인");
            return;
        }

        GameObject root = null;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (obj.name == RootName)
            {
                root = obj;
                break;
            }
        }
        if (root == null)
        {
            EditorUtility.DisplayDialog("엔딩 UI 확인", 
                RootName + "가 없습니다. 먼저 13번 설치 메뉴를 실행해 주세요.", "확인");
            return;
        }

        // Keep the current scene structure; only update its font references.
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            Undo.RecordObject(text, "엔딩 한글 폰트 적용");
            text.font = font;
            EditorUtility.SetDirty(text);
        }

        MemoryOfficeEndingScenePresenter presenter = root.GetComponent<MemoryOfficeEndingScenePresenter>();
        if (presenter != null)
        {
            Undo.RecordObject(presenter, "엔딩 한글 폰트 연결");
            SerializedObject so = new SerializedObject(presenter);
            SerializedProperty korean = so.FindProperty("koreanFont");
            if (korean != null) korean.objectReferenceValue = font;

            Transform bg = root.transform.Find("ResultBackground");
            if (bg != null)
            {
                Transform title = bg.Find("EndingTitle");
                Transform detail = bg.Find("EndingDetail");
                SerializedProperty headingProp = so.FindProperty("heading");
                SerializedProperty detailProp = so.FindProperty("detail");
                if (headingProp != null && title != null)
                    headingProp.objectReferenceValue = title.GetComponent<TextMeshProUGUI>();
                if (detailProp != null && detail != null)
                    detailProp.objectReferenceValue = detail.GetComponent<TextMeshProUGUI>();
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(presenter);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[기억관리국] EndingScene에 MulmaruMono SDF 폰트 적용 완료: " + texts.Length + "개 텍스트");
        EditorUtility.DisplayDialog("엔딩 한글 폰트 적용 완료", 
            "엔딩 화면 텍스트 " + texts.Length + "개에 기존 한글 TMP 폰트를 적용했습니다.\n" +
            "실행 중 폰트 덮어쓰기 문제도 수정했습니다.", "확인");
    }
}
#endif
