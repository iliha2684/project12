#if UNITY_EDITOR
using ProjectED.Examination;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates an independent DAY 10 terminal overlay under the existing Canvas.</summary>
public static class MemoryOfficeDay10TerminalInstaller
{
    private const string RootName = "MemoryOffice_Day10SearchTerminal";

    [MenuItem("기억관리국/UI/8. DAY 10 기록 검색 단말기 연결")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play 모드 종료", "Play 모드를 끄고 SampleScene에서 실행해 주세요.", "확인");
            return;
        }

        ExaminationController controller = Object.FindFirstObjectByType<ExaminationController>();
        Canvas canvas = controller != null ? controller.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.transform.Find("MemoryOffice_Design_Layers") == null)
        {
            EditorUtility.DisplayDialog("UI 확인 필요",
                "SampleScene의 Canvas에 MemoryOffice_Design_Layers가 있는지 확인해 주세요.", "확인");
            return;
        }

        Transform existing = canvas.transform.Find(RootName);
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
        }
        else
        {
            root = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, "검색 단말기 설치");
            root.transform.SetParent(canvas.transform, false);
        }
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Stretch(Require<RectTransform>(root));
        TMP_FontAsset font = FindFont(canvas);

        // Art occupies the lower-left monitor. The hotspot is transparent.
        Button hotspot = MakeButton(root.transform, "Btn_OpenTerminal_OnLeftMonitor",
            new Vector2(-717f, 15f), new Vector2(366f, 330f),
            "", new Color(1f, 1f, 1f, 0f), font);

        GameObject overlay = MakeRect(root.transform, "TerminalPopup");
        Stretch(Require<RectTransform>(overlay));
        Image blocker = Require<Image>(overlay);
        blocker.color = new Color(0.02f, 0.025f, 0.025f, .83f);
        blocker.raycastTarget = true;

        GameObject screen = MakePanel(overlay.transform, "ArchiveScreen", new Vector2(0,0),
            new Vector2(1100,660),new Color(.07f,.11f,.10f,1));

        TMP_Text title = MakeText(screen.transform, "TerminalTitle",new Vector2(0f,262f),
            new Vector2(950,70),34f,"기억관리국 · 기록 검색",new Color(.75f, .98f, .81f, 1),font);

        TMP_Text explain = MakeText(screen.transform, "HelpText",new Vector2(0f,192f),
            new Vector2(950,52),25f,"서류의 시민번호를 입력한 뒤 조회 버튼을 누르세요.",
            new Color(.80f, .86f, .81f,1),font);

        GameObject inputBack = MakePanel(screen.transform, "CitizenNumberInput",new Vector2(0f,106f),
            new Vector2(790,80),new Color(.88f,.89f,.82f,1));
        var input = Require<TMP_InputField>(inputBack);
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 40;
        input.contentType = TMP_InputField.ContentType.Standard;

        GameObject viewport = MakeRect(inputBack.transform,"TextArea");
        Stretch(Require<RectTransform>(viewport));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.offsetMin = new Vector2(20,8);
        viewportRect.offsetMax = new Vector2(-20,-8);
        RectMask2D mask = Require<RectMask2D>(viewport);
        // TMP_InputField expects textComponent, placeholder, and viewport references.
        TMP_Text typed = MakeText(viewport.transform, "Text",Vector2.zero,
            new Vector2(730,55),30f,"",Color.black,font);
        typed.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_Text placeholder = MakeText(viewport.transform,"Placeholder",Vector2.zero,
            new Vector2(730,55),27f,"시민번호 입력 (예: 010101-1234567)",
            new Color(.45f,.48f,.47f,1),font);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.textViewport = viewportRect;
        input.textComponent = (TextMeshProUGUI)typed;
        input.placeholder = placeholder;
        input.text = string.Empty;
        // Suppress UGUI keyboard navigation that changes button selection unexpectedly.
        input.navigation = new Navigation { mode = Navigation.Mode.None };

        Button death = MakeButton(screen.transform,"Btn_DeathRegisterSearch",
            new Vector2(-260f,0f),new Vector2(430f,72f),"사망자 명부 조회",
            new Color(.20f,.40f,.30f,1),font);
        Button incident = MakeButton(screen.transform,"Btn_IncidentArchiveSearch",
            new Vector2(260f,0f),new Vector2(430f,72f),"과거 사건 기록 조회",
            new Color(.19f,.33f,.45f,1),font);

        GameObject resultPanel = MakePanel(screen.transform,"SearchResultBackground",
            new Vector2(0,-147f),new Vector2(950,190),new Color(.03f,.055f,.04f,1));
        TMP_Text results = MakeText(resultPanel.transform,"SearchResultText",Vector2.zero,
            new Vector2(900,165),26f,"검색 결과가 이곳에 나타납니다.",
            new Color(.78f,.99f,.85f,1),font);
        results.alignment = TextAlignmentOptions.TopLeft;

        Button close = MakeButton(screen.transform,"Btn_CloseTerminal",
            new Vector2(0,-278f),new Vector2(290,64),"닫기",
            new Color(.40f,.25f,.24f,1),font);

        var script = Require<MemoryOfficeDay10TerminalUI>(root);
        SerializedObject serialized = new SerializedObject(script);
        serialized.FindProperty("examination").objectReferenceValue = controller;
        serialized.FindProperty("monitorOpenButton").objectReferenceValue = hotspot;
        serialized.FindProperty("popup").objectReferenceValue = overlay;
        serialized.FindProperty("closeButton").objectReferenceValue = close;
        serialized.FindProperty("deathSearchButton").objectReferenceValue = death;
        serialized.FindProperty("incidentSearchButton").objectReferenceValue = incident;
        serialized.FindProperty("citizenNumberInput").objectReferenceValue = input;
        serialized.FindProperty("resultText").objectReferenceValue = results;
        serialized.FindProperty("headingText").objectReferenceValue = title;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Listeners are attached once at runtime; remove stale persistent events.
        ClearPersistentEvents(hotspot);
        ClearPersistentEvents(death);
        ClearPersistentEvents(incident);
        ClearPersistentEvents(close);

        overlay.SetActive(false);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("DAY 10 검색 단말기 설치 완료",
            "왼쪽 검은 단말기를 누르면 시민번호 검색창이 열립니다.\n" +
            "DAY 10 사망자 명부/과거 사건 기록을 실제 데이터에서 조회합니다.\n\n" +
            "Ctrl+S로 SampleScene을 저장해 주세요.", "확인");
    }

    private static GameObject MakeRect(Transform parent,string name)
    {
        Transform current = parent.Find(name);
        if (current != null) return current.gameObject;
        var go = new GameObject(name,typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go,"UI 생성: "+name);
        go.transform.SetParent(parent,false);
        return go;
    }

    private static GameObject MakePanel(Transform parent,string name,Vector2 position,
        Vector2 size,Color color)
    {
        var go=MakeRect(parent,name);
        Position(Require<RectTransform>(go),position,size);
        var image=Require<Image>(go);
        image.color=color;
        image.raycastTarget=true;
        return go;
    }

    private static TMP_Text MakeText(Transform parent,string name,Vector2 position,
        Vector2 size,float textSize,string value,Color color,TMP_FontAsset font)
    {
        var go=MakeRect(parent,name);
        Position(Require<RectTransform>(go),position,size);
        var tmp=Require<TextMeshProUGUI>(go);
        tmp.fontSize=textSize;
        tmp.color=color;
        tmp.text=value;
        tmp.alignment=TextAlignmentOptions.Center;
        tmp.enableWordWrapping=true;
        tmp.raycastTarget=false;
        if(font!=null) tmp.font=font;
        return tmp;
    }

    private static Button MakeButton(Transform parent,string name,Vector2 position,
        Vector2 size,string label,Color color,TMP_FontAsset font)
    {
        var go=MakePanel(parent,name,position,size,color);
        var button=Require<Button>(go);
        button.targetGraphic=go.GetComponent<Image>();
        button.transition=Selectable.Transition.ColorTint;
        button.navigation=new Navigation { mode = Navigation.Mode.None };
        if (!string.IsNullOrEmpty(label))
            MakeText(go.transform,"Label",Vector2.zero,new Vector2(size.x-24,size.y-10),
                29f,label,Color.white,font);
        return button;
    }

    private static void ClearPersistentEvents(Button button)
    {
        // Avoid replacing existing dynamic handlers added by other runtime scripts.
        // Newly installed buttons are unique to this patch and have no persistent listeners.
        if (button == null) return;
        Undo.RecordObject(button,"검색 버튼 연결 초기화");
        button.onClick = new Button.ButtonClickedEvent();
    }

    private static T Require<T>(GameObject go) where T: Component
    {
        T component=go.GetComponent<T>();
        return component!=null?component:Undo.AddComponent<T>(go);
    }

    private static void Position(RectTransform r,Vector2 position,Vector2 size)
    {
        r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
        r.pivot=new Vector2(.5f,.5f);
        r.localScale=Vector3.one;
        r.localRotation=Quaternion.identity;
        r.anchoredPosition=position;
        r.sizeDelta=size;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin=Vector2.zero;
        r.anchorMax=Vector2.one;
        r.offsetMin=Vector2.zero;
        r.offsetMax=Vector2.zero;
        r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=Vector2.zero;
        r.localScale=Vector3.one;
        r.localRotation=Quaternion.identity;
    }

    private static TMP_FontAsset FindFont(Canvas canvas)
    {
        foreach(var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if(t.name=="StatusText" && t.font!=null)return t.font;
        foreach(var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if(t.font!=null)return t.font;
        return TMP_Settings.defaultFontAsset;
    }
}
#endif
