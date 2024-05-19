using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementHandler), typeof(PlayerCameraHandler))]
public class PlayerCharacterController : MonoBehaviour
{
    private static PlayerCharacterController _instance = null;

    private PlayerMovementHandler _movementHandler = null;
    private PlayerCameraHandler _cameraHandler = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = this;

        _movementHandler = GetComponent<PlayerMovementHandler>();
        _cameraHandler = GetComponent<PlayerCameraHandler>();
    }

    public void OnOpenIngameMenu(InputValue value)
    {
        PlayerHUDController.ToggleIngameMenu(!PlayerHUDController.IngameMenuOpened);
    }

    public static void ToggleInput(bool active)
    {
        _instance._movementHandler.InputDisabled = !active;
        _instance._cameraHandler.InputDisabled = !active;
    }
}
