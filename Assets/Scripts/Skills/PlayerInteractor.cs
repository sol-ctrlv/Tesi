using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float interactRadius = 1.5f;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Color sphereColor = new Color(0f, 1f, 1f, 0.34f);
    [SerializeField] AudioSource interactAudioSource;
    InputAction interactMap;

    private void Awake()
    {
        interactMap = playerInput.actions.actionMaps[0].FindAction("Interact");
        interactMap.performed += Interact;
    }
    private void OnDestroy()
    {
        interactMap.performed -= Interact;
    }

    private void Interact(InputAction.CallbackContext context)
    {
        var aroundMe = Physics2D.CircleCastAll(transform.position, interactRadius, Vector2.zero);

        bool playAudio = true;

        for (int i = 0; i < aroundMe.Length; i++)
        {
            IInteractable interactable = aroundMe[i].collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (interactable.Interact(gameObject.transform.parent.gameObject) && playAudio)
                {
                    playAudio = false;
                    interactAudioSource.Play();
                }
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = sphereColor;

        // Convert the local coordinate values into world
        // coordinates for the matrix transformation.
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero, interactRadius);
    }
}
