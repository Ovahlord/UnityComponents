* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDController : MonoBehaviour
{
    [Serializable]
    private class PlayerBarData
    {
        public RectTransform MainBar = null;
        public RectTransform LossBar = null;
        [Min(0f)] public float LossBarAnimationThreshold = 0.05f;
        [Min(0f)] public float LossBarAnimationDelay = 1f;
        [Min(0f)] public float LossBarAnimationSpeedRate = 1f;
    }

    [Header("Player Bars")]
    [SerializeField] private PlayerBarData _playerHealthBar = null;
    [SerializeField] private PlayerBarData _playerPowerBar = null;
    [SerializeField] private PlayerBarData _playerStaminaBar = null;

    [Header("Target Lock")]
    [SerializeField] private RectTransform _lockIndicatorPoint = null;

    private static PlayerHUDController _instance = null;

    private Transform _lockTarget = null;
    private Image _lockTargetImage = null;

    private readonly float[] _playerBarInitialWidth = new float[(int)RessourceIndex.Max];
    private readonly float?[] _playerBarLossDelayTimers = new float?[(int)RessourceIndex.Max];
    private UnitStatsController _playerStatsController = null;

    private void Awake()
    {
        // Singleton setup
        if (_instance != null)
            Destroy(_instance);

        _instance = this;
        DontDestroyOnLoad(this);

        // Container initialization
        _playerBarInitialWidth[(int)RessourceIndex.Health] = _playerHealthBar.MainBar.rect.width;
        _playerBarInitialWidth[(int)RessourceIndex.Power] = _playerPowerBar.MainBar.rect.width;
        _playerBarInitialWidth[(int)RessourceIndex.Stamina] = _playerStaminaBar.MainBar.rect.width;
        for (int i = 0; i < (int)RessourceIndex.Max; ++i)
            _playerBarLossDelayTimers[i] = null;

        _lockTargetImage = _lockIndicatorPoint.GetComponent<Image>();
        _lockTargetImage.enabled = false;

        // Player assignment
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
        UpdatePlayerBar(_playerHealthBar, RessourceIndex.Health, currentHealthPct);
        UpdatePlayerBar(_playerPowerBar, RessourceIndex.Power, currentPowerPct);
        UpdatePlayerBar(_playerStaminaBar, RessourceIndex.Stamina, currentStaminaPct);
    }

    private void UpdatePlayerBar(PlayerBarData barData, RessourceIndex ressource, float targetValuePct)
    {
        float originalWidth = _playerBarInitialWidth[(int)ressource];
        float currentPct = barData.MainBar.sizeDelta.x / originalWidth;

        // update the main bar
        barData.MainBar.sizeDelta = new Vector2(originalWidth * targetValuePct, barData.MainBar.sizeDelta.y);

        // update the loss bar
        if (!_playerBarLossDelayTimers[(int)ressource].HasValue)
        {
            if (ressource == RessourceIndex.Stamina)
                Debug.Log($"current: {currentPct}   target: {targetValuePct}");
            // If we have lost more than x% between the last and the current update, trigger the animation delay
            if (currentPct > targetValuePct && (currentPct - targetValuePct) >= barData.LossBarAnimationThreshold)
                _playerBarLossDelayTimers[(int)ressource] = barData.LossBarAnimationDelay;
            else
                barData.LossBar.sizeDelta = barData.MainBar.sizeDelta;
        }
        else
        {
            // Update the loss bar animation
            if (_playerBarLossDelayTimers[(int)ressource] <= Time.deltaTime)
            {
                // If we have reached the target sizeDelta value or already are below the target value already, cancel the animation
                if (barData.MainBar.sizeDelta.x >= barData.LossBar.sizeDelta.x)
                {
                    barData.LossBar.sizeDelta = barData.MainBar.sizeDelta;
                    _playerBarLossDelayTimers[(int)ressource] = null;
                }
                else
                    barData.LossBar.sizeDelta = Vector2.MoveTowards(barData.LossBar.sizeDelta, barData.MainBar.sizeDelta, originalWidth * barData.LossBarAnimationSpeedRate * Time.deltaTime);
            }
            else
                _playerBarLossDelayTimers[(int)ressource] -= Time.deltaTime;
        }
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