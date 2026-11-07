using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ProjectED.Examination;

/// <summary>
/// Formats the existing shared speech bubbles for the 1920x1080 Canvas.
/// Preserves the original sprites, dialogue controller, event references and scene GUIDs.
/// </summary>
public static class MemoryOfficeSpeechBubbleReadabilityInstaller
{
    private const string Menu = "기억관리국/UI/12. NPC 말풍선 글자 잘림 수정";

    [MenuItem(Menu)]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("말풍선 수정", "Play 모드를 종료한 뒤 실행해 주세요.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("말풍선 수정", "SampleScene을 먼저 열어 주세요.", "확인");
            return;
        }

        Transform canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas candidate in root.GetComponentsInChildren<Canvas>(true))
            {
                if (candidate.gameObject.name == "Canvas")
                {
                    canvas = candidate.transform;
                    break;
                }
            }
            if (canvas != null) break;
        }
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("말풍선 수정", "Canvas를 찾지 못했습니다.", "확인");
            return;
        }

        Transform citizen = canvas.Find("CitizenSpeechBubble");
        Transform player = canvas.Find("PlayerSpeechBubble");
        if (citizen == null || player == null)
        {
            EditorUtility.DisplayDialog("말풍선 수정", "기존 CitizenSpeechBubble / PlayerSpeechBubble을 찾지 못했습니다.", "확인");
            return;
        }

        if (!FormatBubble(citizen) || !FormatBubble(player))
        {
            EditorUtility.DisplayDialog("말풍선 수정", "말풍선의 Main/Text 또는 SpeechBubbleView 구성 요소를 찾지 못했습니다. 기존 구조를 확인해 주세요.", "확인");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("말풍선 수정 완료",
            "기존 NPC/플레이어 말풍선의 글자 여백과 크기를 수정했습니다.\n" +
            "말풍선 배경 이미지, 클릭 이벤트, NPC 데이터는 변경하지 않았습니다.\n\n" +
            "Ctrl + S를 눌러 씬을 저장해 주세요.", "확인");
    }

    private static bool FormatBubble(Transform root)
    {
        SpeechBubbleView view = root.GetComponent<SpeechBubbleView>();
        RectTransform panel = root.Find("Main") as RectTransform;
        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        if (view == null || panel == null || text == null) return false;

        // The runtime Show() method already resizes the panel to fit lines of dialogue.
        // Disable the editor-created fitters so they do not fight those explicit sizes.
        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            Undo.RecordObject(fitter, "Stop automatic bubble size conflict");
            fitter.enabled = false;
            EditorUtility.SetDirty(fitter);
        }
        VerticalLayoutGroup group = panel.GetComponent<VerticalLayoutGroup>();
        if (group != null)
        {
            Undo.RecordObject(group, "Stop bubble text layout conflict");
            group.enabled = false;
            EditorUtility.SetDirty(group);
        }

        // Update the serialized settings without replacing or recreating this script.
        Undo.RecordObject(view, "Increase speech bubble minimum height");
        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty minHeight = serialized.FindProperty("minHeight");
        SerializedProperty padding = serialized.FindProperty("verticalPadding");
        if (minHeight != null) minHeight.floatValue = 175f;
        if (padding != null) padding.floatValue = 36f;
        serialized.ApplyModifiedProperties();

        Undo.RecordObject(text, "Add speech bubble text padding");
        text.enableAutoSizing = false;
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(20f, 20f, 18f, 16f);
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);

        // Keep the existing position and width. Only avoid stretching text across the edge.
        RectTransform textRect = text.rectTransform;
        Undo.RecordObject(textRect, "Align speech text within panel");
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        EditorUtility.SetDirty(textRect);
        EditorUtility.SetDirty(view);
        return true;
    }
}
