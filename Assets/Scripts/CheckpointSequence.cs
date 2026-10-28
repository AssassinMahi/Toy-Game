using UnityEngine;

/// <summary>Activates checkpoints in order and advances when the car reaches one.</summary>
public class CheckpointSequence : MonoBehaviour
{
    [Tooltip("Checkpoints in the order they must be reached. Add more by extending this array.")]
    [SerializeField] private GameObject[] checkpoints;

    [SerializeField, HideInInspector] private int currentCheckpointIndex;

    public int TotalCheckpoints
    {
        get
        {
            if (checkpoints == null)
                return 0;

            int count = 0;
            foreach (GameObject checkpoint in checkpoints)
            {
                if (checkpoint != null)
                    count++;
            }

            return count;
        }
    }

    public int CompletedCheckpoints
    {
        get
        {
            if (checkpoints == null)
                return 0;

            int count = 0;
            int completedBeforeIndex = Mathf.Min(currentCheckpointIndex, checkpoints.Length);
            for (int i = 0; i < completedBeforeIndex; i++)
            {
                if (checkpoints[i] != null)
                    count++;
            }

            return count;
        }
    }

    private void Start()
    {
        if (checkpoints == null || checkpoints.Length == 0)
        {
            Debug.LogWarning("No checkpoints assigned to CheckpointSequence.", this);
            return;
        }

        foreach (GameObject checkpoint in checkpoints)
        {
            if (checkpoint != null)
                checkpoint.SetActive(false);
        }

        currentCheckpointIndex = FindNextCheckpointIndex(0);
        ActivateCurrentCheckpoint();
    }

    /// <summary>Returns true when the touched object is the current checkpoint.</summary>
    public bool TryReachCheckpoint(Transform touchedTransform)
    {
        if (checkpoints == null || currentCheckpointIndex >= checkpoints.Length)
            return false;

        // Resolve nested colliders to the direct checkpoint child of this parent.
        Transform checkpointTransform = touchedTransform;
        while (checkpointTransform != null && checkpointTransform.parent != transform)
            checkpointTransform = checkpointTransform.parent;

        if (checkpointTransform == null || checkpoints[currentCheckpointIndex] != checkpointTransform.gameObject)
            return false;

        checkpoints[currentCheckpointIndex].SetActive(false);
        currentCheckpointIndex = FindNextCheckpointIndex(currentCheckpointIndex + 1);
        ActivateCurrentCheckpoint();

        if (currentCheckpointIndex >= checkpoints.Length)
            FindFirstObjectByType<PauseMenuUI>()?.ShowChapterComplete();

        return true;
    }

    private int FindNextCheckpointIndex(int startIndex)
    {
        while (startIndex < checkpoints.Length && checkpoints[startIndex] == null)
            startIndex++;

        return startIndex;
    }

    private void ActivateCurrentCheckpoint()
    {
        if (currentCheckpointIndex < checkpoints.Length)
            checkpoints[currentCheckpointIndex].SetActive(true);
        else
            Debug.Log("All checkpoints reached!", this);
    }
}
