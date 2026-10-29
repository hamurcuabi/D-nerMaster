using UnityEngine;

namespace DonerMaster
{
    /// <summary>Trauma-based camera shake plus a tiny "zoom punch" for big moments.</summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake I { get; private set; }

        Vector3 _basePos;
        Quaternion _baseRot;
        float _trauma;
        float _zoom;
        Camera _cam;
        float _baseFov;
        float _seed;

        void Awake()
        {
            I = this;
            _cam = GetComponent<Camera>();
            _seed = Random.value * 100f;
        }

        /// <summary>Call after the camera has been positioned/fitted.</summary>
        public void Capture()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            if (_cam != null) _baseFov = _cam.fieldOfView;
        }

        public static void Add(float trauma)
        {
            if (I == null) return;
            I._trauma = Mathf.Clamp01(I._trauma + trauma);
        }

        public static void ZoomPunch(float amount = 1.5f)
        {
            if (I == null) return;
            I._zoom = Mathf.Max(I._zoom, amount);
        }

        void LateUpdate()
        {
            if (_baseFov <= 0f) return;
            float shake = _trauma * _trauma;
            float t = Time.time * 25f;
            var offset = new Vector3(
                (Mathf.PerlinNoise(_seed, t) - 0.5f),
                (Mathf.PerlinNoise(_seed + 10f, t) - 0.5f),
                0f) * 0.25f * shake;
            float roll = (Mathf.PerlinNoise(_seed + 20f, t) - 0.5f) * 4f * shake;
            transform.localPosition = _basePos + transform.localRotation * offset;
            transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, roll);
            _trauma = Mathf.Max(0f, _trauma - Time.deltaTime * 1.8f);

            _zoom = Mathf.MoveTowards(_zoom, 0f, Time.deltaTime * 8f);
            if (_cam != null) _cam.fieldOfView = _baseFov - _zoom;
        }
    }
}
