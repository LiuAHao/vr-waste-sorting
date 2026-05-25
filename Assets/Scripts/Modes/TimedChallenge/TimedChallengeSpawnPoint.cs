using UnityEngine;

public sealed class TimedChallengeSpawnPoint : MonoBehaviour
{
    [SerializeField] private string groupId = "default";

    private bool _isOccupied;

    public string GroupId => groupId;
    public bool IsOccupied => _isOccupied;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public void SetOccupied(bool occupied)
    {
        _isOccupied = occupied;
    }

    public bool MatchesGroup(string targetGroupId)
    {
        if (string.IsNullOrWhiteSpace(targetGroupId))
        {
            return true;
        }

        return groupId == targetGroupId;
    }
}
