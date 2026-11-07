using ProjectED.Cases;
using ProjectED.Days;
using TMPro;
using UnityEngine;

namespace ProjectED.Examination
{
    /// <summary>
    /// 새 UI의 상단 검은 모니터에 남은 시민 수를 보여준다.
    /// 배정 인원 - 판정을 확정한 인원으로 계산하여 결과 화면에서도 정확하게 표시한다.
    /// 기존 심사, NPC, DAY, 엔딩 코드는 수정하지 않는다.
    /// </summary>
    public sealed class MemoryOfficeRemainingCitizenDisplay : MonoBehaviour
    {
        [SerializeField] private ExaminationController examinationController;
        [SerializeField] private TMP_Text counterText;

        private ExaminationController subscribedController;
        private int judgedPosition;
        private string lastText;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            EnsureSubscribed();
            Refresh();
        }

        private void Update()
        {
            // DAY 로드 직후, 다음 시민 로드 직후, 게임 종료 등 UI 변화에 대응한다.
            EnsureSubscribed();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void EnsureSubscribed()
        {
            if (examinationController == null)
                examinationController = GetComponentInParent<ExaminationController>();
            if (examinationController == null)
                examinationController = FindFirstObjectByType<ExaminationController>();
            if (subscribedController == examinationController) return;

            Unsubscribe();
            subscribedController = examinationController;
            if (subscribedController == null) return;

            subscribedController.DayLoaded += HandleDayLoaded;
            subscribedController.CitizenJudged += HandleCitizenJudged;
            subscribedController.DayCompleted += HandleDayCompleted;
        }

        private void Unsubscribe()
        {
            if (subscribedController == null) return;
            subscribedController.DayLoaded -= HandleDayLoaded;
            subscribedController.CitizenJudged -= HandleCitizenJudged;
            subscribedController.DayCompleted -= HandleDayCompleted;
            subscribedController = null;
        }

        private void HandleDayLoaded(DayPlan plan)
        {
            judgedPosition = 0;
            Refresh();
        }

        private void HandleCitizenJudged(CitizenCaseInstance citizen, CaseDisposition disposition, bool correct)
        {
            // 이벤트는 시민 판정을 실제로 확정할 때 한 번 발생한다.
            if (examinationController != null)
                judgedPosition = Mathf.Max(judgedPosition, examinationController.CurrentCitizenPosition);
            Refresh();
        }

        private void HandleDayCompleted(DayPlan plan)
        {
            judgedPosition = examinationController != null ? examinationController.TotalCitizenCount : 0;
            Refresh();
        }

        private void Refresh()
        {
            if (counterText == null) return;
            if (examinationController == null)
            {
                Show("남은 사람 --명");
                return;
            }

            int total = examinationController.TotalCitizenCount;
            int remaining = Mathf.Max(0, total - judgedPosition);
            Show($"남은 사람 {remaining}명");
        }

        private void Show(string value)
        {
            if (lastText == value) return;
            lastText = value;
            counterText.text = value;
        }
    }
}
