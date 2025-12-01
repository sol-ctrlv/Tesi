using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : ActorMovement
{
    [SerializeField] GameObject flipWithPlayer;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] float movementSpeed = 5f;
    Vector2 moveInput;

    [SerializeField] AudioSource walkAudioSource;

    InputAction movementAction;

    [SerializeField] float distanceToPlaySound;
    Vector2 lastWalkSFXPosition;

    private void Start()
    {
        var camera = CinemachineCore.GetVirtualCamera(0);
        camera.transform.position = new Vector3(transform.position.x, transform.position.y, camera.transform.position.z);
    }

    private void OnEnable()
    {
        movementAction = playerInput.actions.actionMaps[0].FindAction("Movement");
        movementAction.performed += OnPlayerMovement;
        movementAction.canceled += OnPlayerMovement;
    }

    private void OnDisable()
    {
        movementAction.performed -= OnPlayerMovement;
        movementAction.canceled -= OnPlayerMovement;
    }

    public void OnPlayerMovement(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        movDir = moveInput;
        ManageAnimator(moveInput.magnitude);
        flipWithPlayer.transform.localScale = actorRenderer.flipX ? new Vector3(-1, 1, 1) : Vector3.one;
    }

    private void Update()
    {
        rb2d.linearVelocity = moveInput * movementSpeed * Time.fixedDeltaTime;

        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);

        if ((currentPosition - lastWalkSFXPosition).sqrMagnitude > distanceToPlaySound)
        {
            lastWalkSFXPosition = currentPosition;
            walkAudioSource.pitch = Random.Range(0.8f, 1.2f);
            walkAudioSource.Play();
        }
    }
}
