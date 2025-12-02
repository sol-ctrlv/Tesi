using UnityEngine;

/// <summary>
/// Chest che, alla prima interazione del player, assegna
/// uno fra tre buff possibili e poi si disattiva.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RewardChest : MonoBehaviour, IInteractable
{
    [Header("Interazione")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource openAudioSource;
    private bool opened = false;

    private void OpenChest(GameObject currentPlayer)
    {
        opened = true;

        animator.SetTrigger("Open");
        openAudioSource.Play();

        Instantiate(RewardsManager.Instance.GetPowerUp(), currentPlayer.transform);

    }

    public bool Interact(GameObject interactor)
    {
        if (opened)
            return false;

        OpenChest(interactor);

        return true;
    }
}
