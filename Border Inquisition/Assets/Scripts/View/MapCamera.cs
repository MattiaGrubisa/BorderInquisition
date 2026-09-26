using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace View
{
    // Drives the camera while the map is loaded: drag to pan, wheel to zoom towards the cursor, and
    // the view never leaves the map. The camera itself lives in Bootstrap, so it is found by tag.
    // Focus glides the view to a point (the AI's battles); any pan or zoom by hand cancels it.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MapCamera : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _map;
        [SerializeField] private float _minSize = 1.5f;
        [SerializeField] private float _zoomStep = 0.12f;
        [SerializeField] private float _keyboardPanSpeed = 1.5f;
        // Roughly how long a focus glide takes, in seconds.
        [SerializeField] private float _focusTime = 0.4f;
        // A focus zooms in to at most this share of the widest view, and never closer than it finds it.
        [SerializeField, Range(0.1f, 1f)] private float _focusZoom = 0.55f;

        private Camera _camera;
        private Bounds _bounds;
        private bool _dragging;
        private Vector2 _dragOrigin;
        private Vector3? _focus;
        private float _focusSize;
        private Vector3 _focusVelocity;
        private float _focusSizeVelocity;

        public bool IsFocusing => _focus != null;

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
            var byHand = Zoom() | Drag() | PanWithKeyboard();
            if (byHand)
                _focus = null;
            else
                MoveToFocus();
            Clamp();
        }

        // Glides to the middle of the two points, zoomed so both stay in view.
        public void Focus(Vector2 a, Vector2 b)
        {
            if (!enabled)
                return;

            var fit = Vector2.Distance(a, b) * 0.75f;
            _focusSize = Mathf.Clamp(Mathf.Max(fit, Mathf.Min(_camera.orthographicSize, MaxSize * _focusZoom)),
                Mathf.Min(_minSize, MaxSize), MaxSize);

            var middle = (a + b) / 2f;
            _focus = Clamped(new Vector3(middle.x, middle.y, _camera.transform.position.z), _focusSize);
        }

        private void MoveToFocus()
        {
            if (_focus == null)
                return;

            var target = _focus.Value;
            var view = _camera.transform;
            view.position = Vector3.SmoothDamp(view.position, target, ref _focusVelocity, _focusTime);
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _focusSize, ref _focusSizeVelocity, _focusTime);

            if (Vector3.Distance(view.position, target) < 0.02f && Mathf.Abs(_camera.orthographicSize - _focusSize) < 0.02f)
                _focus = null;
        }

        private bool Zoom()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return false;

            // Scrolling over the HUD belongs to the HUD.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            // Keep whatever sits under the cursor in place, so zooming follows the pointer.
            var screen = mouse.position.ReadValue();
            var before = ScreenToWorld(screen);

            _camera.orthographicSize *= 1f - Mathf.Sign(scroll) * _zoomStep;
            ClampSize();

            _camera.transform.position += (Vector3)(before - ScreenToWorld(screen));
            return true;
        }

        private bool Drag()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            if (!mouse.rightButton.isPressed && !mouse.middleButton.isPressed)
            {
                _dragging = false;
                return false;
            }

            var screen = mouse.position.ReadValue();
            if (!_dragging)
            {
                _dragging = true;
                _dragOrigin = ScreenToWorld(screen);
                return true;
            }

            _camera.transform.position += (Vector3)(_dragOrigin - ScreenToWorld(screen));
            return true;
        }

        private bool PanWithKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            var move = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;

            if (move == Vector2.zero)
                return false;

            // Scaled by the zoom level, so panning covers the same share of the screen either way.
            var step = _keyboardPanSpeed * _camera.orthographicSize * Time.deltaTime;
            _camera.transform.position += (Vector3)(move.normalized * step);
            return true;
        }

        private void Clamp()
        {
            ClampSize();
            _camera.transform.position = Clamped(_camera.transform.position, _camera.orthographicSize);
        }

        // Where a view of the given size may stand without leaving the map.
        private Vector3 Clamped(Vector3 position, float size)
        {
            position.x = ClampAxis(position.x, _bounds.center.x, _bounds.extents.x - size * _camera.aspect);
            position.y = ClampAxis(position.y, _bounds.center.y, _bounds.extents.y - size);
            return position;
        }

        private void ClampSize() =>
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, Mathf.Min(_minSize, MaxSize), MaxSize);

        // With no room left on an axis the view is pinned to the middle of the map.
        private static float ClampAxis(float value, float center, float slack) =>
            slack <= 0f ? center : Mathf.Clamp(value, center - slack, center + slack);

        private Vector2 ScreenToWorld(Vector2 screen) => _camera.ScreenToWorldPoint(screen);
    }
}
