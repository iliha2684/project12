using UnityEngine;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    /// <summary>
    /// 새 UI의 오른쪽 긴급 버튼 -> 기존 피검자 구금 판정.
    /// 심사/스토리/오류 처리 코드는 변경하지 않는다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MemoryOfficeEmergencyDetentionButton : MonoBehaviour
    {
        [SerializeField] private ExaminationController examinationController;
        [SerializeField] private Button emergencyButton;

        private void Awake()
        {
            if (emergencyButton == null)
                emergencyButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (emergencyButton == null)
                emergencyButton = GetComponent<Button>();
            if (emergencyButton != null)
                emergencyButton.onClick.AddListener(Detain);
        }

        private void OnDisable()
        {
            if (emergencyButton != null)
                emergencyButton.onClick.RemoveListener(Detain);
        }

        private void Update()
        {
            if (emergencyButton != null)
            {
                bool allowed = CanDetain();
                if (emergencyButton.interactable != allowed)
                    emergencyButton.interactable = allowed;
            }
        }

        private bool CanDetain()
        {
            if (examinationController == null || !examinationController.HasActiveCase)
                return false;

            // DAY 에셋을 실제로 로드한 심사에서만 사용; DAY 1~6은 불가.
            var plan = examinationController.CurrentDayPlan;
            if (plan == null || plan.Definition == null || plan.Definition.DayNumber < 7)
                return false;

            var manager = GameManager.Instance;
            if (manager != null)
            {
                if (manager.currentDay < 7 || manager.IsGameOver())
                    return false;
                var ending = manager.GetComponent<EndingManager>();
                if (ending != null && ending.HasResolvedEnding)
                    return false;
            }

            return true;
        }

        public void Detain()
        {
            if (!CanDetain())
                return;

            // 기존 심사 규칙 및 NPC 이벤트로 구금을 판정한다.
            examinationController.SelectDetention();
            // 선택이 거부됐을 경우 기본 '추출 승인'으로 잘못 확정되는 것을 방지.
            if (examinationController.SelectedDisposition !=
                ProjectED.Cases.CaseDisposition.Detention)
            {
                Debug.LogWarning("[기억관리국 UI] 현재 DAY에는 긴급 구금을 사용할 수 없습니다.");
                return;
            }

            examinationController.StampSelectedDisposition();
            if (examinationController.HasActiveCase)
                return;

            // 게임 오버 및 엔딩 확정 시에는 다음 시민으로 넘어가지 않는다.
            var manager = GameManager.Instance;
            if (manager != null)
            {
                if (manager.IsGameOver())
                    return;
                var ending = manager.GetComponent<EndingManager>();
                if (ending != null && ending.HasResolvedEnding)
                    return;
            }

            // 기존 '결과 -> 다음 시민/다음 DAY' 처리 경로를 재사용한다.
            // 상인 제안이 대기 중이면 기존 컨트롤러가 자동 진행을 차단한다.
            examinationController.StampSelectedDisposition();
        }
    }
}
