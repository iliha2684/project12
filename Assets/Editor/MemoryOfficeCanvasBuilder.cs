// 기억관리국 - PNG 15개를 Canvas의 개별 UI Image로 배치하는 에디터 도구.
// 기존 게임의 스크립트, .meta, 버튼 참조를 삭제하거나 교체하지 않습니다.
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MemoryOfficeCanvasBuilder
{
    private const string SpriteDir = "Assets/MemoryOfficeCanvas/Sprites/";
    private const string RootName = "MemoryOffice_Design_Layers";

    // 원본 PNG는 모두 1920x1080, 투명 배경. 각각 원본 캔버스 위치를 유지합니다.
    // 표시 순서: 뒤 → 앞. 비활성 3장은 대체 상태(파란 단말기/눌린 버튼)입니다.
    private struct Layer
    {
        public readonly int id;
        public readonly string name;
        public readonly bool active;
        public Layer(int id, string name, bool active = true)
        { this.id = id; this.name = name; this.active = active; }
    }

    private static readonly Layer[] Layers =
    {
        new Layer(1,  "01_벽_천장_조명"),
        new Layer(3,  "02_양쪽벽_창구"),
        new Layer(4,  "03_중앙심사창구"),
        new Layer(5,  "04_상단대화모니터"),
        new Layer(2,  "05_책상"),
        new Layer(10, "06_왼쪽공지판"),
        new Layer(8,  "07_검색단말기_검정"),
        new Layer(9,  "08_검색단말기_파랑_대체", false),
        new Layer(11, "09_오른쪽서류함"),
        new Layer(12, "10_십자가"),
        new Layer(7,  "11_긴급버튼"),
        new Layer(13, "12_거절버튼_기본"),
        new Layer(14, "13_승인버튼_기본"),
        new Layer(16, "14_거절버튼_눌림_대체", false),
        new Layer(15, "15_승인버튼_눌림_대체", false),
    };

    [MenuItem("기억관리국/UI/1. PNG 15개를 Canvas에 개별 배치")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 끈 다음 실행해 주세요.", "확인");
            return;
        }

        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음", "SampleScene을 열고 Hierarchy에 Canvas가 있는지 확인해 주세요.", "확인");
            return;
        }

        string[] missing = CheckSprites();
        if (missing.Length > 0)
        {
            EditorUtility.DisplayDialog("이미지 파일을 찾을 수 없음", string.Join("\n", missing), "확인");
            return;
        }

        Transform old = canvas.transform.Find(RootName);
        if (old != null)
        {
            bool replace = EditorUtility.DisplayDialog("이미 배치됨", "기존 디자인 레이어만 다시 만들까요? 기존 시민증/버튼/게임 스크립트는 변경하지 않습니다.", "레이어만 다시 생성", "취소");
            if (!replace) return;
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        GameObject holder = new GameObject(RootName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(holder, "기억관리국 UI 레이어 생성");
        holder.transform.SetParent(canvas.transform, false);
        holder.transform.SetSiblingIndex(0); // 원래 게임 UI를 가리지 않도록 맨 뒤에 둡니다.
        Stretch((RectTransform)holder.transform);

        foreach (Layer layer in Layers)
        {
            string path = SpriteDir + "2026.10.01_" + layer.id + ".png";
            Sprite sprite = EnsureSprite(path);
            if (sprite == null)
            {
                Debug.LogError("[Canvas UI] Sprite 로드 실패: " + path);
                continue;
            }

            var go = new GameObject(layer.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "개별 UI Image 생성");
            go.transform.SetParent(holder.transform, false);
            Stretch((RectTransform)go.transform);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false; // 디자인 이미지가 기존 버튼 클릭을 가로막지 않게 합니다.
            go.SetActive(layer.active);
        }

        // 기존 샘플 Scene의 회색 'Desk' 이미지만 감춰 그림과 중복되지 않게 합니다.
        // 오브젝트나 참조는 삭제하지 않으며 Inspector에서 다시 켤 수 있습니다.
        Transform oldDesk = canvas.transform.Find("Desk");
        if (oldDesk != null)
        {
            var oldImage = oldDesk.GetComponent<Image>();
            if (oldImage != null)
            {
                Undo.RecordObject(oldImage, "기존 샘플 책상 이미지 숨기기");
                oldImage.enabled = false;
                EditorUtility.SetDirty(oldImage);
            }
        }

        Selection.activeGameObject = holder;
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[Canvas UI] PNG 15장을 Canvas 안에 독립된 Image로 배치했습니다. 대체 레이어 3장은 비활성화되어 있습니다.");
        EditorUtility.DisplayDialog("Canvas 이미지 배치 완료", "Canvas > MemoryOffice_Design_Layers 아래에 이미지 15개가 만들어졌습니다.\n기존 게임 기능과 Button 참조는 유지됩니다.\n\nScene 저장: Ctrl+S", "확인");
    }

    // 선택 옵션: 기존 게임의 버튼 컴포넌트 자체를 움직여서 빨강/초록 아트 위에 배치.
    // 버튼을 새로 만들지 않으므로 ExaminationController가 참조하는 컴포넌트는 유지됩니다.
    // 추출 승인/서류 반려는 "선택"이고, 기존 "도장 찍기"로 최종 제출합니다.
    [MenuItem("기억관리국/UI/2. 기존 승인-거절 버튼을 이미지 위치에 맞추기 (선택)")]
    public static void PlaceExistingButtons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 끈 다음 실행해 주세요.", "확인");
            return;
        }
        Canvas canvas = FindCanvas();
        if (canvas == null || canvas.transform.Find(RootName) == null)
        {
            EditorUtility.DisplayDialog("디자인 없음", "먼저 메뉴 1번으로 디자인 이미지들을 배치하세요.", "확인");
            return;
        }
        Button approve = FindButton(canvas, "Btn_Approve");
        Button reject = FindButton(canvas, "Btn_Reject");
        if (approve == null || reject == null)
        {
            EditorUtility.DisplayDialog("기존 버튼 찾기 실패", "Btn_Approve와 Btn_Reject가 있는 기존 SampleScene에서 실행해 주세요.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("기존 버튼 위치 변경", "버튼 컴포넌트와 기존 판정 스크립트 참조는 그대로 두고, 클릭 영역만 이미지 위치로 옮깁니다.\n\n승인(초록)과 거절(빨강)은 처분 선택 버튼입니다.\n최종 제출은 기존 '도장 찍기' 버튼을 사용합니다.", "버튼 위치 변경", "취소")) return;

        // 원본 1920x1080 좌상단 기준의 실제 버튼 bounding box.
        MoveButton(approve, canvas.transform, 237f, 723f, 402f, 881f);
        MoveButton(reject, canvas.transform, 39f, 723f, 204f, 881f);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[Canvas UI] 기존 승인/거절 Button 참조를 보존한 채 디자인 위치로 이동했습니다.");
    }

    private static void MoveButton(Button button, Transform canvas, float x1, float y1, float x2, float y2)
    {
        Undo.SetTransformParent(button.transform, canvas, "기존 버튼 디자인 위치로 이동");
        RectTransform rect = button.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "기존 버튼 RectTransform 조정");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = new Vector2(x2 - x1, y2 - y1);
        rect.anchoredPosition = new Vector2((x1 + x2) * 0.5f - 960f, 540f - (y1 + y2) * 0.5f);
        rect.SetAsLastSibling();
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "버튼 이미지 투명 처리");
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;  // 투명해도 버튼 클릭은 가능합니다.
        }
        foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == image) continue;
            Undo.RecordObject(graphic, "기존 버튼 글자 숨기기");
            graphic.enabled = false;
        }
        EditorUtility.SetDirty(rect);
    }

    private static Button FindButton(Canvas canvas, string name)
    {
        foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
            if (btn.name == name) return btn;
        return null;
    }

    private static Canvas FindCanvas()
    {
        if (Selection.activeGameObject != null)
        {
            Canvas selected = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (selected != null) return selected;
        }
        var named = GameObject.Find("Canvas");
        if (named != null && named.GetComponent<Canvas>() != null) return named.GetComponent<Canvas>();
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        return canvases.Length > 0 ? canvases[0] : null;
    }

    private static string[] CheckSprites()
    {
        var missing = new System.Collections.Generic.List<string>();
        foreach (Layer l in Layers)
        {
            string path = SpriteDir + "2026.10.01_" + l.id + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) missing.Add(path);
        }
        return missing.ToArray();
    }

    private static Sprite EnsureSprite(string path)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = Vector2.zero;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
    }
}
#endif
