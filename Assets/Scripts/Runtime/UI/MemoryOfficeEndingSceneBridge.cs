using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Uses the existing seven-ending decision only; transitions after it resolves.
/// Lives on the persistent GameManager and never changes the verdict itself.
/// </summary>
[DisallowMultipleComponent]
public sealed class MemoryOfficeEndingSceneBridge : MonoBehaviour
{
    [SerializeField] private string endingSceneName = "EndingScene";
    private bool requested;

    private void Update()
    {
        if (requested || SceneManager.GetActiveScene().name == endingSceneName) return;
        EndingManager endings = GetComponent<EndingManager>();
        if (endings == null || !endings.HasResolvedEnding) return;
        if (!Application.CanStreamedLevelBeLoaded(endingSceneName))
        {
            // One clear error rather than repeatedly attempting a missing scene.
            Debug.LogError("엔딩 씬을 열 수 없습니다. Build Settings에 " + endingSceneName + "을 추가해 주세요.");
            requested = true;
            return;
        }
        requested = true;
        SceneManager.LoadScene(endingSceneName);
    }
}
