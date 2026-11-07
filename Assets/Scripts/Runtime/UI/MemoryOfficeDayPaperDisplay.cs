using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the live GameManager.currentDay on the upper-left paper.
/// The TMP label is on the same Canvas GameObject as this component.
/// Works with scenes created by the original date-paper UI installer.
/// </summary>
public sealed class MemoryOfficeDayPaperDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;

    [SerializeField, Tooltip("Optional DAY 1 calendar date (yyyy-MM-dd). Leave blank to display the day number only.")]
    private string dayOneCalendarDate = "";

    private GameManager gameManager;
    private int lastDisplayedDay = -1;
    private string lastDateSetting;

    private void Awake()
    {
        ConnectText();
    }

    private void OnEnable()
    {
        ConnectText();
        lastDisplayedDay = -1;
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void OnValidate()
    {
        // Fix already-installed Canvas objects without re-running the editor menu.
        ConnectText();
        lastDisplayedDay = -1;
    }

    private void ConnectText()
    {
        if (dayText == null)
            dayText = GetComponent<TMP_Text>();
    }

    private void Refresh()
    {
        ConnectText();
        if (dayText == null)
            return;

        // A persistent GameManager may be created after the Canvas is enabled.
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
        }

        // Do not overwrite the editor preview until the GameManager exists.
        if (gameManager == null)
            return;

        int currentDay = Mathf.Max(1, gameManager.currentDay);
        if (lastDisplayedDay == currentDay && lastDateSetting == dayOneCalendarDate)
            return;

        lastDisplayedDay = currentDay;
        lastDateSetting = dayOneCalendarDate;

        string dateLine = string.Format(CultureInfo.InvariantCulture, "심사 {0}일차", currentDay);
        if (!string.IsNullOrWhiteSpace(dayOneCalendarDate) &&
            DateTime.TryParseExact(dayOneCalendarDate.Trim(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime firstDate))
        {
            dateLine = firstDate.AddDays(currentDay - 1).ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
        }

        dayText.text =
            "<size=23><b>기억관리국</b></size>\n" +
            "<size=19>업무 기록</size>\n\n" +
            string.Format(CultureInfo.InvariantCulture, "<size=40><b>DAY {0:00}</b></size>\n", currentDay) +
            "<size=21>" + dateLine + "</size>";
    }
}
