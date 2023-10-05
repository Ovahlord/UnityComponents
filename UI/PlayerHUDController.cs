/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private RectTransform _playerHealthBar;
    [SerializeField] private RectTransform _playerPowerBar;
    [SerializeField] private RectTransform _playerStaminaBar;
    [SerializeField] private RectTransform _lockIndicatorPoint;

    private static PlayerHUDController _instance = null;

    private Transform _lockTarget = null;
    private Image _lockTargetImage = null;

    private readonly float[] _playerBarInitialWidth = new float[(int)RessourceIndex.Max];
    private UnitStatsController _playerStatsController = null;

    private void Awake()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = this;
        DontDestroyOnLoad(this);

        _playerBarInitialWidth[(int)RessourceIndex.Health] = _playerHealthBar.rect.width;
        _playerBarInitialWidth[(int)RessourceIndex.Power] = _playerPowerBar.rect.width;
        _playerBarInitialWidth[(int)RessourceIndex.Stamina] = _playerStaminaBar.rect.width;
        _lockTargetImage = _lockIndicatorPoint.GetComponent<Image>();
        _lockTargetImage.enabled = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.Log("PlayerHUDController: Could not find a GameObject tagged as 'Player'. Make sure you have such a GameObject in the scene and that it holds the 'UnitStatsController' component");
            Destroy(gameObject);
            return;
        }

        if (player.TryGetComponent(out UnitStatsController playerStatsController))
            _playerStatsController = playerStatsController;
        else
        {
            Debug.Log("PlayerHUDController: could not find a 'UnitStatsController' component in the Player GameObject");
            Destroy(gameObject);
        }
    }

    public void LateUpdate()
    {
        UpdatePlayerBars(_playerStatsController.CurrentHealthPct, _playerStatsController.CurrentPowerPct, _playerStatsController.CurrentStaminaPct);
        UpdateLockIndicatorPoisition();
    }

    public void UpdatePlayerBars(float currentHealthPct, float currentPowerPct, float currentStaminaPct)
    {
        SetBarSize(_playerHealthBar, _playerBarInitialWidth[(int)RessourceIndex.Health], currentHealthPct);
        SetBarSize(_playerPowerBar, _playerBarInitialWidth[(int)RessourceIndex.Power], currentPowerPct);
        SetBarSize(_playerStaminaBar, _playerBarInitialWidth[(int)RessourceIndex.Stamina], currentStaminaPct);
    }

    private void SetBarSize(RectTransform barTransform, float originalSize, float pct)
    {
        Vector2 sizeDelta = barTransform.sizeDelta;
        sizeDelta.x = originalSize * pct;
        barTransform.sizeDelta = sizeDelta;
    }

    public static void SetLockOnTarget(Transform lockTarget)
    {
        if (_instance == null)
            return;

        _instance._lockTarget = lockTarget;
        _instance._lockTargetImage.enabled = lockTarget != null;
    }

    private void UpdateLockIndicatorPoisition()
    {
        if (_lockTarget == null)
            return;

        _lockIndicatorPoint.position = Camera.main.WorldToScreenPoint(_lockTarget.position);
    }
}