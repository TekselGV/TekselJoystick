using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JoystickControl : MonoBehaviour
{
    private const float INCHES_IN_CM = 0.393701f;

    [Tooltip("Joystick radius in cantimiters")]
    [Range(0.25f, 4f)]
    [SerializeField] private float _joystickRadiusCm = 2;

    [Tooltip("Joystick auto smooth speed")]
    [Range(10f, 40f)]
    [SerializeField] private float _joystickSmoothSpeed = 25;

    [SerializeField] private RectTransform _originRectTr;
    [SerializeField] private RectTransform _pointerRectTr;
    [SerializeField] private Canvas _canvas;

    [Header("Debug")]
    [SerializeField] private Transform _debugGO;

    private float _joystickRadiusPx;
    private float _horizontalValueRaw;
    private float _verticalValueRaw;
    private float _horizontalValueSmoothed;
    private float _verticalValueSmoothed;

    private bool _originInitialized;

    private void OnEnable()
    {
        _joystickRadiusPx = _joystickRadiusCm * Screen.dpi * INCHES_IN_CM / _canvas.scaleFactor;
        UpdateOriginSize();
    }

    private void OnDisable()
    {
        DisableJoystick();
    }

    private void Update()
    {
        var joystikPos = GetJoystickPitchAndYawOutput();
        _debugGO.position = new Vector3(joystikPos.x, joystikPos.y, 0);
    }

    /// <summary>
    /// Update size of the origin image considering screen DPI and joystic radius parameter
    /// </summary>
    private void UpdateOriginSize()
    {
        _originRectTr.sizeDelta = new Vector2(_joystickRadiusPx * 2, _joystickRadiusPx * 2);
        Debug.Log($"UpdateOriginSize DPI: {Screen.dpi} sizeDelta: {_originRectTr.sizeDelta.x} Canvas Scale: {_canvas.scaleFactor}");
    }

    /// <summary>
    /// Calculate joystick input state in this moment
    /// </summary>
    /// <returns>Smoothed joystick offset values from the touch origin</returns>
    public Vector2 GetJoystickPitchAndYawOutput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            EnableJoystick();
            CalculateTargetAndOriginPositions(Input.mousePosition);
            CalculateRawPitchYaw();
        }
        else
        {
            DisableJoystick();
        }
#else
        int touchCount = Input.touchCount;
        if (touchCount > 0)
        {
            EnableJoystick();
            CalculateTargetAndOriginPositions(Input.GetTouch(0).position);
            CalculateRawPitchYaw();

            if (touchCount > 1 && Input.GetTouch(1).phase == TouchPhase.Began)
            {
               Debug.Log("Second Tap registered");
            }
        }
        else
        {
            DisableJoystick();
        }
#endif

        _horizontalValueSmoothed = Mathf.MoveTowards(_horizontalValueSmoothed, _horizontalValueRaw, _joystickSmoothSpeed * Time.deltaTime);
        _verticalValueSmoothed = Mathf.MoveTowards(_verticalValueSmoothed, _verticalValueRaw, _joystickSmoothSpeed * Time.deltaTime);

        return new Vector2(_verticalValueSmoothed, -1 * _horizontalValueSmoothed);
    }

    private void CalculateTargetAndOriginPositions(Vector2 touchPosition)
    {
        Vector2 anchoredPos;
        // recalculate screen position to Rect anchored position
        anchoredPos.x = touchPosition.x / _canvas.scaleFactor;
        anchoredPos.y = touchPosition.y / _canvas.scaleFactor;

        InitializeOrigin(anchoredPos);

        // Clamp pointer position to joystick radius
        _pointerRectTr.anchoredPosition = Vector2.ClampMagnitude(anchoredPos - _originRectTr.anchoredPosition, _joystickRadiusPx) + _originRectTr.anchoredPosition;
    }

    private void CalculateRawPitchYaw()
    {
        Vector2 normalizedTarget = (_pointerRectTr.anchoredPosition - _originRectTr.anchoredPosition) / _joystickRadiusPx;

        _horizontalValueRaw = normalizedTarget.x;
        _verticalValueRaw = normalizedTarget.y;
    }

    /// <summary>
    /// Move origin to certain anchored position in the canvas space
    /// </summary>
    /// <param name="anchoredPos">Desired origin's rectTransform anchored position</param>
    private void InitializeOrigin(Vector2 anchoredPos)
    {
        if (_originInitialized == false)
        {
            _originRectTr.anchoredPosition = anchoredPos;
            _originInitialized = true;
        }
    }

    private void EnableJoystick()
    {
        _originRectTr.gameObject.SetActive(true);
        _pointerRectTr.gameObject.SetActive(true);
    }

    public void DisableJoystick()
    {
        _originRectTr.gameObject.SetActive(false);
        _originInitialized = false;
        _pointerRectTr.gameObject.SetActive(false);

        _horizontalValueRaw = 0f;
        _verticalValueRaw = 0f;
    }
}
