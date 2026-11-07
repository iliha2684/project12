using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Positions existing shared NPC / player dialogue bubbles within the main desk window.
/// Changes RectTransform only; never recreates bubbles or changes component references.
/// </summary>
public static class MemoryOfficeSpeechBubblePositionInstaller
{
    private const string MenuPath = "기억관리국/UI/11. NPC 말풍선 중앙 창구로 이동";

    [MenuItem(MenuPath)]
    private static void MoveBubbles()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("말풍선 위치 변경", "Play 모드를 종료한 뒤 실행해 주세요.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("말풍선 위치 변경", "SampleScene을 먼저 열어 주세요.", "확인");
            return;
        }

        Canvas canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.gameObject.name == "Canvas")
                {
                    canvas = candidate;
                    break;
                }
            }
            if (canvas != null) break;
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("말풍선 위치 변경", "현재 씬에서 Canvas를 찾지 못했습니다.", "확인");
            return;
        }

        Transform citizen = canvas.transform.Find("CitizenSpeechBubble");
        Transform player = canvas.transform.Find("PlayerSpeechBubble");
        if (!(citizen is RectTransform) || !(player is RectTransform))
        {
            EditorUtility.DisplayDialog("말풍선 위치 변경", "Canvas 바로 아래의 CitizenSpeechBubble 또는 PlayerSpeechBubble을 찾지 못했습니다. 기존 오브젝트 이름을 확인해 주세요.", "확인");
            return;
        }

        // Design is based on a 1920 x 1080 reference Canvas.
        // The top-right black monitor is reserved for the remaining-citizen counter.
        // Use the central reception window instead, away from that counter.
        RectTransform citizenRect = (RectTransform)citizen;
        RectTransform playerRect = (RectTransform)player;

        Undo.RecordObject(citizenRect, "Move NPC speech bubble to reception window");
        Undo.RecordObject(playerRect, "Move player speech bubble to reception window");

        Place(citizenRect);
        Place(playerRect);

        EditorUtility.SetDirty(citizenRect);
        EditorUtility.SetDirty(playerRect);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "말풍선 위치 변경 완료",
            "시민(성직자 포함) 및 플레이어 말풍선을 새 UI 중앙 창구로 옮겼습니다.\n\n" +
            "기존 말풍선/대사 스크립트/클릭 이벤트는 그대로 유지됩니다.\n" +
            "Ctrl + S로 씬을 저장해 주세요.",
            "확인");
    }

    private static void Place(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-110f, 205f);
        // Do not change size, parent, active state, raycast target or serialized references.
    }
}
