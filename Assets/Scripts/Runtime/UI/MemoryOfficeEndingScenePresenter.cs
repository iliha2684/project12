using TMPro;
using UnityEngine;

/// <summary>Displays the outcome selected by the existing EndingManager.</summary>
[DisallowMultipleComponent]
public sealed class MemoryOfficeEndingScenePresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text detail;
    [SerializeField] private TMP_FontAsset koreanFont;

    private void Start()
    {
        // The editor assigns Assets/Fonts/TMP/MulmaruMono SDF.asset to this field.
        // Do not replace it at runtime with an OS-installed font: that override
        // can hide Korean glyphs or fail on machines without the font.
        if (koreanFont != null)
        {
            if (heading != null) heading.font = koreanFont;
            if (detail != null) detail.font = koreanFont;
        }

        GameManager game = GameManager.Instance;
        EndingManager endings = game != null ? game.GetComponent<EndingManager>() : null;
        if (endings == null || !endings.HasResolvedEnding)
        {
            if (heading != null) heading.text = "엔딩 정보가 없습니다";
            if (detail != null) detail.text = "SampleScene에서 게임을 진행해 주세요.";
            return;
        }

        if (heading != null) heading.text = EndingTitle(endings.ResolvedEnding);
        if (detail != null)
        {
            detail.text = "DAY " + game.currentDay.ToString("00") +
                " · " + endings.ResolvedEnding + "\n" + endings.ResolutionReason;
        }
    }

    private static string EndingTitle(MemoryOfficeEnding ending)
    {
        switch (ending)
        {
            case MemoryOfficeEnding.Bad1Dismissal: return "BAD ENDING 1 · 해고";
            case MemoryOfficeEnding.Bad2WhiteOrganization: return "BAD ENDING 2 · 백색서고";
            case MemoryOfficeEnding.Bad3Priest: return "BAD ENDING 3 · 성직자";
            case MemoryOfficeEnding.Bad4Arrest: return "BAD ENDING 4 · 체포";
            case MemoryOfficeEnding.Normal: return "NORMAL ENDING";
            case MemoryOfficeEnding.Happy: return "HAPPY ENDING";
            case MemoryOfficeEnding.Obedience: return "OBEDIENCE ENDING";
            default: return "ENDING";
        }
    }
}
