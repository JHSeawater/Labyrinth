using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Labyrinth/StageData", order = 0)]
public class StageData : ScriptableObject
{
    [Tooltip("해당 스테이지의 고유 레벨 번호 (1, 2, 3...)")]
    public int LevelID = 1;

    [Tooltip("별 3개를 받기 위한 최대 클리어 시간 (이 시간 이하일 때 3별)")]
    public float TimeLimitFor3Stars = 15f;

    [Tooltip("별 2개를 받기 위한 최대 클리어 시간 (이 시간 이하일 때 2별, 초과 시 1별)")]
    public float TimeLimitFor2Stars = 30f;
}
