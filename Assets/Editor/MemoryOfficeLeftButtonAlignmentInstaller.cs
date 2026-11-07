#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 Canvas의 좌측 승인/반려 버튼 받침을 복구하고,
/// 빨강/초록 버튼 그림(기본·눌림)과 실제 클릭 영역을 함께 정렬합니다.
/// 심사 함수, 버튼 OnClick, 기타 DAY UI는 수정하지 않습니다.
/// </summary>
public static class MemoryOfficeLeftButtonAlignmentInstaller
{
    private const string LayerRoot = "MemoryOffice_Design_Layers";
    private const string ButtonRoot = "MemoryOffice_ActionButtons";
    private const string FrameName = "11A_왼쪽_승인거절_버튼받침";
    private const string SpritePath = "Assets/MemoryOfficeCanvas/Sprites/LeftButtons_Frame_1920x1080.png";
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [MenuItem("기억관리국/UI/16. 왼쪽 승인·거절 버튼 받침 및 위치 맞추기")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 끄고 실행해 주세요.", "확인");
            return;
        }

        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음", "SampleScene을 열고 기존 Canvas를 확인해 주세요.", "확인");
            return;
        }

        Transform art = canvas.transform.Find(LayerRoot);
        Transform buttons = canvas.transform.Find(ButtonRoot);
        if (art == null || buttons == null)
        {
            EditorUtility.DisplayDialog("기존 UI 연결 필요",
                "Canvas에 MemoryOffice_Design_Layers와 MemoryOffice_ActionButtons가 필요합니다.\n" +
                "먼저 UI 1번과 3번 설치 메뉴가 적용되었는지 확인해 주세요.", "확인");
            return;
        }

        Transform redArt = art.Find("12_거절버튼_기본");
        Transform greenArt = art.Find("13_승인버튼_기본");
        Transform redPressed = art.Find("14_거절버튼_눌림_대체");
        Transform greenPressed = art.Find("15_승인버튼_눌림_대체");
        Transform redHit = buttons.Find("Btn_Red_Reject_OneClick");
        Transform greenHit = buttons.Find("Btn_Green_Approve_OneClick");
        if (redArt == null || greenArt == null || redPressed == null || greenPressed == null ||
            redHit == null || greenHit == null)
        {
            EditorUtility.DisplayDialog("버튼을 찾지 못함",
                "기존 빨강·초록 기본/눌림 이미지 4개와 원클릭 버튼 2개를 확인해 주세요.\n" +
                "기존 기능의 연결을 임의로 재생성하지 않고 작업을 중단했습니다.", "확인");
            return;
        }

        Sprite frameSprite = ImportSprite();
        if (frameSprite == null)
        {
            EditorUtility.DisplayDialog("이미지 파일 없음",
                "버튼 받침 PNG를 찾지 못했습니다:\n" + SpritePath +
                "\nZIP의 Assets 폴더를 기존 프로젝트에 병합했는지 확인해 주세요.", "확인");
            return;
        }

        // 별도의 이미지 오브젝트를 만들되 중복 설치해도 하나만 유지합니다.
        Transform existingFrame = art.Find(FrameName);
        GameObject frame;
        if (existingFrame != null) frame = existingFrame.gameObject;
        else
        {
            frame = new GameObject(FrameName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(frame, "왼쪽 버튼 받침 추가");
            frame.transform.SetParent(art, false);
        }
        var frameRect = frame.GetComponent<RectTransform>();
        if (frameRect == null) frameRect = Undo.AddComponent<RectTransform>(frame);
        SetFullCanvas(frameRect);
        var frameImage = frame.GetComponent<Image>();
        if (frameImage == null) frameImage = Undo.AddComponent<Image>(frame);
        Undo.RecordObject(frameImage, "버튼 받침 그림 설정");
        frameImage.sprite = frameSprite;
        frameImage.type = Image.Type.Simple;
        frameImage.preserveAspect = false;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;
        frame.SetActive(true);
        // 버튼 그림의 바로 뒤에 넣어 빨강/초록 본체가 프레임 위에 그려지게 합니다.
        int redIndex = redArt.GetSiblingIndex();
        if (frame.transform.GetSiblingIndex() < redIndex) redIndex--;
        frame.transform.SetSiblingIndex(redIndex);

        // 원본 1920x1080 시안의 절대 좌표를 그대로 사용합니다.
        // 이전 16번 도구가 적용한 Y=-24px 이동을 되돌립니다.
        RestoreOriginalFullSizeImage(redArt);
        RestoreOriginalFullSizeImage(greenArt);
        RestoreOriginalFullSizeImage(redPressed);
        RestoreOriginalFullSizeImage(greenPressed);

        // 실제 투명 클릭 영역도 원본 버튼 도형 위치와 정확히 일치시킵니다.
        // 기존 Button / 이벤트 리스너 / Bridge는 유지합니다.
        SetHitbox(redHit, 39f, 723f, 204f, 881f);
        SetHitbox(greenHit, 237f, 723f, 402f, 881f);

        EditorUtility.SetDirty(frame);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = frame;
        Debug.Log("[기억관리국 UI] 왼쪽 버튼 받침 추가 및 빨강·초록 버튼 그림/클릭 영역 정렬 완료.");
        EditorUtility.DisplayDialog("왼쪽 버튼 정렬 완료",
            "왼쪽 버튼 받침 이미지를 별도 Image로 추가했습니다.\n" +
            "버튼 그림과 클릭 영역을 원본 시안의 좌표로 정확히 복원했습니다.\n" +
            "기존 심사 버튼 이벤트는 그대로 유지했습니다.\n\nCtrl + S로 SampleScene을 저장해 주세요.", "확인");
    }

    private static void SetFullCanvas(RectTransform rt)
    {
        Undo.RecordObject(rt, "받침 위치 설정");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(rt);
    }

    private static void RestoreOriginalFullSizeImage(Transform target)
    {
        RectTransform rt = target.GetComponent<RectTransform>();
        if (rt == null) return;
        Undo.RecordObject(rt, "버튼 원본 시안 좌표 복원");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(rt);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "버튼 이미지 클릭 간섭 방지");
            image.raycastTarget = false;
        }
    }

    private static void SetHitbox(Transform target, float x1, float y1, float x2, float y2)
    {
        RectTransform rt = target.GetComponent<RectTransform>();
        if (rt == null) return;
        Undo.RecordObject(rt, "버튼 클릭 영역 정렬");
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(x2 - x1, y2 - y1);
        rt.anchoredPosition = new Vector2((x1 + x2) * .5f - ReferenceWidth * .5f,
                                          ReferenceHeight * .5f - (y1 + y2) * .5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(rt);
        // GameObject/Button/onClick/Bridge는 절대 교체하지 않음.
    }

    private static Canvas FindCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
            if (canvas.transform.Find(LayerRoot) != null)
                return canvas;
        return null;
    }

    private static Sprite ImportSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
#endif
