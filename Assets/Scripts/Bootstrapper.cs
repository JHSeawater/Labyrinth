using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // 1. 모바일 기기의 타겟 프레임 설정 (쾌적한 조작감을 위해 60 강제)
        Application.targetFrameRate = 60;
        
        // 2. 게임 도중 화면이 어두워지거나 꺼지는 현상 방지
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("[Bootstrapper] Mobile execution environment setup complete (60FPS / No Sleep).");
    }
}
