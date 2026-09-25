using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace View
{
    // Drives the camera while the map is loaded: drag to pan, wheel to zoom towards the cursor, and
    // the view never leaves the map. The camera itself lives in Bootstrap, so it is found by tag.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MapCamera : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _map;
        [SerializeField] private float _minSize = 1.5f;
        [SerializeField] private float _zoomStep = 0.12f;
        [SerializeField] private float _keyboardPanSpeed = 1.5f;

        private Camera _camera;
        private Bounds _bounds;
        private bool _dragging;
        private Vector2 _dragOrigin;

        // The widest view that still fits inside the map, on whichever axis runs out first.
        private float MaxSize => Mathf.Min(_bounds.extents.y, _bounds.extents.x / _camera.aspect);

        private void Awake()
        {
            if (_map == null)
                _map = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _camera = Camera.main;
            if (_camera == null || _map == null)
            {
                Debug.LogWarning("MapCamera needs a main camera and a map sprite.", this);
                enabled = false;
                return;
            }

            _bounds = _map.bounds;
            _camera.orthographicSize = MaxSize;
            _camera.transform.position = new Vector3(_bounds.center.x, _bounds.center.y, _camera.transform.position.z);
        }

        private void LateUpdate()
        {
            Zoom();
            Drag();
            PanWithKeyboard();
            Clamp();
        }

        private void Zoom()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            // Scrolling over the HUD belongs to the HUD.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Keep whatever sits under the cursor in place, so zooming follows the pointer.
            var screen = mouse.position.ReadValue();
            var before = ScreenToWorld(screen);

            _camera.orthographicSize *= 1f - Mathf.Sign(scroll) * _zoomStep;
            ClampSize();

            _camera.transform.position += (Vector3)(before - ScreenToWorld(screen));
        }

        private void Drag()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (!mouse.rightButton.isPressed && !mouse.middleButton.isPressed)
            {
                _dragging = false;
                return;
            }

            var screen = mouse.position.ReadValue();
            if (!_dragging)
            {
                _dragging = true;
                _dragOrigin = ScreenToWorld(screen);
                return;
            }

            _camera.transform.position += (Vector3)(_dragOrigin - ScreenToWorld(screen));
        }

        private void PanWithKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var move = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;

            if (move == Vector2.zero)
                return;

            // Scaled by the zoom level, so panning covers the same share of the screen either way.
            var step = _keyboardPanSpeed * _camera.orthographicSize * Time.deltaTime;
            _camera.transform.position += (Vector3)(move.normalized * step);
        }

        private void Clamp()
        {
            ClampSize();

            var position = _camera.transform.position;
            position.x = ClampAxis(position.x, _bounds.center.x, _bounds.extents.x - _camera.orthographicSize * _camera.aspect);
            position.y = ClampAxis(position.y, _bounds.center.y, _bounds.extents.y - _camera.orthographicSize);
            _camera.transform.position = position;
        }

        private void ClampSize() =>
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, Mathf.Min(_minSize, MaxSize), MaxSize);

        // With no room left on an axis the view is pinned to the middle of the map.
        private static float ClampAxis(float value, float center, float slack) =>
            slack <= 0f ? center : Mathf.Clamp(value, center - slack, center + slack);

        private Vector2 ScreenToWorld(Vector2 screen) => _camera.ScreenToWorldPoint(screen);
    }
}
