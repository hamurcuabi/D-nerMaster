using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// A customer with a personality: patience, an order, and readable reactions
    /// (walk in, wait, get impatient, celebrate or storm off).
    /// </summary>
    public class Customer : MonoBehaviour, ITappable
    {
        public enum State { Entering, Waiting, Leaving }

        public CustomerType Type;
        public Order Order;
        public int Slot;
        public State Current { get; private set; }
        public float PatienceMax { get; private set; }
        public float Patience { get; private set; }
        public float Patience01 => PatienceMax <= 0 ? 0 : Mathf.Clamp01(Patience / PatienceMax);
        public bool IsWaiting => Current == State.Waiting;
        public bool Paused;               // tutorial: patience frozen
        public Vector3 BubbleAnchor => transform.position + Vector3.up * (1.95f * Type.Height + 0.15f);
        public Vector3 HandPoint => transform.position + Vector3.up * 1.15f + Vector3.back * 0.3f;

        Transform _visual;
        Transform _head;
        Transform _mouth;
        Transform _browL, _browR;
        Transform _armL, _armR;
        GameObject _cheeks;
        Material _skinMat;
        Color _skin;
        Vector3 _from, _to;
        float _walkT;
        float _walkDuration = 1.1f;
        float _tickTimer;
        float _seed;
        float _happyTimer;
        bool _angry;
        System.Action<Customer> _onGone;

        static readonly Color[] SkinTones =
        {
            new Color(0.98f, 0.84f, 0.72f), new Color(0.91f, 0.72f, 0.56f), new Color(0.78f, 0.57f, 0.42f),
            new Color(0.6f, 0.42f, 0.3f), new Color(0.42f, 0.29f, 0.2f)
        };

        public static Customer Create(CustomerType type, Order order, int slot, Vector3 slotPos, Vector3 enterFrom, float patienceMult, System.Action<Customer> onGone)
        {
            var go = new GameObject("Customer_" + type.Id);
            go.transform.position = enterFrom;
            var c = go.AddComponent<Customer>();
            c.Type = type;
            c.Order = order;
            c.Slot = slot;
            c._from = enterFrom;
            c._to = slotPos;
            c.PatienceMax = type.Patience * patienceMult;
            c.Patience = c.PatienceMax;
            c._seed = Random.value * 10f;
            c._onGone = onGone;
            c.Current = State.Entering;
            c.BuildVisual();

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.3f * type.Height, 0);
            col.height = 1.4f * type.Height;
            col.radius = 0.42f;
            return c;
        }

        void BuildVisual()
        {
            float h = Type.Height;
            _visual = Build.Empty("Visual", transform, Vector3.zero).transform;
            _skin = SkinTones[Random.Range(0, SkinTones.Length)];
            _skinMat = Mats.Make(_skin, 0.35f);
            var dark = new Color(0.08f, 0.07f, 0.07f);
            var hairColors = new[] { new Color(0.12f, 0.08f, 0.05f), new Color(0.35f, 0.2f, 0.1f), new Color(0.75f, 0.6f, 0.3f), new Color(0.55f, 0.55f, 0.58f) };
            var hair = hairColors[Random.Range(0, hairColors.Length)];

            // Body + arms
            Build.Prim(PrimitiveType.Capsule, "Body", _visual, new Vector3(0, 0.95f * h, 0), new Vector3(0.66f, 0.62f * h, 0.5f), Type.Body, false, 0.3f);
            _armL = Build.Prim(PrimitiveType.Capsule, "ArmL", _visual, new Vector3(-0.38f, 1.12f * h, 0), new Vector3(0.17f, 0.32f, 0.17f), Type.Body * 0.9f, false, 0.3f).transform;
            _armR = Build.Prim(PrimitiveType.Capsule, "ArmR", _visual, new Vector3(0.38f, 1.12f * h, 0), new Vector3(0.17f, 0.32f, 0.17f), Type.Body * 0.9f, false, 0.3f).transform;

            // Head
            _head = Build.Empty("Head", _visual, new Vector3(0, 1.78f * h, 0)).transform;
            var headBall = Build.Prim(PrimitiveType.Sphere, "Skull", _head, Vector3.zero, new Vector3(0.46f, 0.5f, 0.46f), _skin);
            headBall.GetComponent<Renderer>().sharedMaterial = _skinMat;
            Build.Prim(PrimitiveType.Sphere, "Hair", _head, new Vector3(0, 0.08f, 0.04f), new Vector3(0.48f, 0.42f, 0.47f), hair, false, 0.2f);
            Build.Prim(PrimitiveType.Sphere, "EyeL", _head, new Vector3(-0.09f, 0.03f, -0.21f), new Vector3(0.065f, 0.08f, 0.04f), dark, false, 0.9f);
            Build.Prim(PrimitiveType.Sphere, "EyeR", _head, new Vector3(0.09f, 0.03f, -0.21f), new Vector3(0.065f, 0.08f, 0.04f), dark, false, 0.9f);
            Build.Prim(PrimitiveType.Sphere, "Nose", _head, new Vector3(0, -0.03f, -0.235f), new Vector3(0.07f, 0.07f, 0.06f), _skin * 0.95f, false, 0.3f);
            _browL = Build.Prim(PrimitiveType.Cube, "BrowL", _head, new Vector3(-0.09f, 0.12f, -0.215f), new Vector3(0.09f, 0.018f, 0.02f), hair * 0.8f).transform;
            _browR = Build.Prim(PrimitiveType.Cube, "BrowR", _head, new Vector3(0.09f, 0.12f, -0.215f), new Vector3(0.09f, 0.018f, 0.02f), hair * 0.8f).transform;
            _mouth = Build.Prim(PrimitiveType.Cube, "Mouth", _head, new Vector3(0, -0.11f, -0.215f), new Vector3(0.1f, 0.02f, 0.02f), new Color(0.45f, 0.12f, 0.12f)).transform;
            _cheeks = Build.Empty("Cheeks", _head, Vector3.zero);
            Build.Prim(PrimitiveType.Sphere, "CheekL", _cheeks.transform, new Vector3(-0.15f, -0.06f, -0.185f), new Vector3(0.07f, 0.045f, 0.03f), new Color(1f, 0.5f, 0.5f));
            Build.Prim(PrimitiveType.Sphere, "CheekR", _cheeks.transform, new Vector3(0.15f, -0.06f, -0.185f), new Vector3(0.07f, 0.045f, 0.03f), new Color(1f, 0.5f, 0.5f));
            _cheeks.SetActive(false);

            AddAccessories(dark);
            Juice.PopIn(_visual, 0.35f);
        }

        void AddAccessories(Color dark)
        {
            var a = Type.Accent;
            switch (Type.Id)
            {
                case "student":
                    Build.Prim(PrimitiveType.Cylinder, "Cap", _head, new Vector3(0, 0.2f, 0), new Vector3(0.44f, 0.06f, 0.44f), a, false, 0.3f);
                    Build.Prim(PrimitiveType.Cube, "Brim", _head, new Vector3(0, 0.17f, -0.25f), new Vector3(0.3f, 0.02f, 0.18f), a, false, 0.3f);
                    Build.Prim(PrimitiveType.Cube, "StrapL", _visual, new Vector3(-0.18f, 1.2f * Type.Height, -0.24f), new Vector3(0.05f, 0.4f, 0.02f), dark);
                    Build.Prim(PrimitiveType.Cube, "StrapR", _visual, new Vector3(0.18f, 1.2f * Type.Height, -0.24f), new Vector3(0.05f, 0.4f, 0.02f), dark);
                    break;
                case "office":
                    Build.Prim(PrimitiveType.Cube, "Shirt", _visual, new Vector3(0, 1.27f * Type.Height, -0.245f), new Vector3(0.16f, 0.22f, 0.02f), Color.white);
                    Build.Prim(PrimitiveType.Cube, "Tie", _visual, new Vector3(0, 1.22f * Type.Height, -0.26f), new Vector3(0.06f, 0.3f, 0.02f), a);
                    break;
                case "taxi":
                    Build.Prim(PrimitiveType.Cylinder, "FlatCap", _head, new Vector3(0, 0.19f, 0.01f), new Vector3(0.47f, 0.05f, 0.47f), new Color(0.3f, 0.25f, 0.2f), false, 0.2f);
                    Build.Prim(PrimitiveType.Cube, "Brim", _head, new Vector3(0, 0.17f, -0.24f), new Vector3(0.28f, 0.02f, 0.12f), new Color(0.3f, 0.25f, 0.2f));
                    Build.Prim(PrimitiveType.Cube, "Mustache", _head, new Vector3(0, -0.075f, -0.23f), new Vector3(0.16f, 0.035f, 0.03f), dark);
                    break;
                case "tourist":
                    Build.Prim(PrimitiveType.Cylinder, "BucketHat", _head, new Vector3(0, 0.18f, 0), new Vector3(0.62f, 0.02f, 0.62f), a, false, 0.2f);
                    Build.Prim(PrimitiveType.Cylinder, "HatTop", _head, new Vector3(0, 0.23f, 0), new Vector3(0.42f, 0.06f, 0.42f), a, false, 0.2f);
                    Build.Prim(PrimitiveType.Cube, "Camera", _visual, new Vector3(0, 1.15f * Type.Height, -0.3f), new Vector3(0.2f, 0.13f, 0.08f), dark, false, 0.6f);
                    var lens = Build.Prim(PrimitiveType.Cylinder, "Lens", _visual, new Vector3(0, 1.15f * Type.Height, -0.36f), new Vector3(0.08f, 0.03f, 0.08f), new Color(0.3f, 0.3f, 0.35f), false, 0.9f, 0.5f);
                    lens.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case "auntie":
                    Build.Prim(PrimitiveType.Sphere, "Scarf", _head, new Vector3(0, 0.05f, 0.05f), new Vector3(0.53f, 0.56f, 0.5f), a, false, 0.2f);
                    Build.Prim(PrimitiveType.Cube, "Glasses", _head, new Vector3(0, 0.03f, -0.225f), new Vector3(0.28f, 0.015f, 0.01f), dark);
                    break;
                case "critic":
                    Build.Prim(PrimitiveType.Sphere, "Beret", _head, new Vector3(0.04f, 0.2f, 0), new Vector3(0.5f, 0.12f, 0.48f), a, false, 0.2f);
                    Build.Prim(PrimitiveType.Cube, "Mustache", _head, new Vector3(0, -0.075f, -0.23f), new Vector3(0.18f, 0.03f, 0.03f), dark);
                    var gl = Build.Prim(PrimitiveType.Cylinder, "Monocle", _head, new Vector3(0.09f, 0.03f, -0.235f), new Vector3(0.1f, 0.005f, 0.1f), new Color(0.85f, 0.7f, 0.3f), false, 0.9f, 0.9f);
                    gl.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time + _seed;

            switch (Current)
            {
                case State.Entering:
                case State.Leaving:
                    _walkT += dt / _walkDuration;
                    if (_walkT < 0f) break;   // reacting before walking off
                    float k = Juice.EaseInOutQuad(Mathf.Clamp01(_walkT));
                    transform.position = Vector3.Lerp(_from, _to, k);
                    _visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 12f)) * 0.06f, 0);
                    _visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 12f) * 4f);
                    SwingArms(t * 12f, 25f);
                    if (_walkT >= 1f)
                    {
                        if (Current == State.Entering)
                        {
                            Current = State.Waiting;
                            _visual.localRotation = Quaternion.identity;
                        }
                        else
                        {
                            _onGone?.Invoke(this);
                            Destroy(gameObject);
                        }
                    }
                    break;

                case State.Waiting:
                    if (!Paused && GameManager.I != null && GameManager.I.IsPlaying) Patience -= dt;
                    float p = Patience01;
                    // idle breathing, faster and twitchier when impatient
                    float breathe = p > 0.35f ? Mathf.Sin(t * 2f) * 0.015f : Mathf.Abs(Mathf.Sin(t * 9f)) * 0.03f;
                    _visual.localPosition = new Vector3(p > 0.35f ? 0 : Mathf.Sin(t * 30f) * 0.008f, breathe, 0);
                    _head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 4f, Mathf.Sin(t * 0.5f) * 10f, 0);
                    SwingArms(t * 2f, 3f);

                    if (_happyTimer <= 0f) SetFace(p > 0.6f ? 0 : p > 0.35f ? 1 : 2);
                    Mats.SetColor(_skinMat, Color.Lerp(_skin, new Color(1f, 0.35f, 0.3f), Mathf.Clamp01((0.35f - p) / 0.35f) * 0.45f));

                    if (p < 0.35f)
                    {
                        _tickTimer -= dt;
                        if (_tickTimer <= 0f)
                        {
                            _tickTimer = Mathf.Lerp(0.5f, 1.3f, p / 0.35f);
                            AudioManager.Play(Sfx.Tick, 0.35f, 1f, 0.1f, 0.15f);
                        }
                    }
                    if (Patience <= 0f) GiveUp();
                    break;
            }

            if (_happyTimer > 0f) _happyTimer -= dt;
        }

        void SwingArms(float phase, float amount)
        {
            if (_armL == null) return;
            _armL.localRotation = Quaternion.Euler(Mathf.Sin(phase) * amount, 0, 8f);
            _armR.localRotation = Quaternion.Euler(-Mathf.Sin(phase) * amount, 0, -8f);
        }

        /// <summary>0 = content, 1 = meh, 2 = angry, 3 = delighted.</summary>
        void SetFace(int mood)
        {
            _cheeks.SetActive(mood == 3);
            switch (mood)
            {
                case 0:
                    _mouth.localScale = new Vector3(0.1f, 0.02f, 0.02f);
                    _mouth.localPosition = new Vector3(0, -0.11f, -0.215f);
                    _browL.localRotation = Quaternion.identity; _browR.localRotation = Quaternion.identity;
                    break;
                case 1:
                    _mouth.localScale = new Vector3(0.07f, 0.015f, 0.02f);
                    _browL.localRotation = Quaternion.Euler(0, 0, -8f); _browR.localRotation = Quaternion.Euler(0, 0, 8f);
                    break;
                case 2:
                    _mouth.localScale = new Vector3(0.09f, 0.035f, 0.02f);
                    _browL.localRotation = Quaternion.Euler(0, 0, -25f); _browR.localRotation = Quaternion.Euler(0, 0, 25f);
                    break;
                case 3:
                    _mouth.localScale = new Vector3(0.15f, 0.06f, 0.02f);
                    _mouth.localPosition = new Vector3(0, -0.1f, -0.215f);
                    _browL.localRotation = Quaternion.Euler(0, 0, 12f); _browR.localRotation = Quaternion.Euler(0, 0, -12f);
                    break;
            }
        }

        /// <summary>React to the food and walk away. quality 0..100.</summary>
        public void Served(int quality)
        {
            if (Current != State.Waiting) return;
            if (quality >= GameData.ComboBreakQuality)
            {
                SetFace(3);
                _happyTimer = 2f;
                Juice.Punch(_visual, 0.15f, 0.4f);
                StartCoroutineSafe(Hop());
                AudioManager.Play(Sfx.Mmm, 0.7f, Type.Height > 1f ? 0.85f : 1.15f, 0.08f);
                if (quality >= GameData.PerfectQuality) FX.Sparkle(BubbleAnchor - Vector3.up * 0.3f, 18, new Color(1f, 0.4f, 0.55f));
                LeaveAfter(1.1f);
            }
            else if (quality >= GameData.RefuseQuality)
            {
                SetFace(1);
                _happyTimer = 2f;
                LeaveAfter(0.9f);
            }
            else
            {
                Storm();
            }
        }

        void GiveUp()
        {
            GameManager.I?.OnCustomerGaveUp(this);
            Storm();
        }

        void Storm()
        {
            if (_angry) return;
            _angry = true;
            SetFace(2);
            _happyTimer = 3f;
            Juice.Shake(_visual, 0.06f, 0.4f);
            AudioManager.Play(Sfx.Grumble, 0.7f, Type.Height > 1f ? 0.85f : 1.1f);
            FX.Smoke(_head.position + Vector3.up * 0.3f, 4, new Color(0.9f, 0.9f, 0.9f, 0.6f));
            LeaveAfter(0.7f);
        }

        void LeaveAfter(float delay)
        {
            Current = State.Leaving;
            _walkT = -delay / _walkDuration;  // stand still for "delay" seconds first
            _from = transform.position;
            _to = new Vector3(transform.position.x + (transform.position.x < 0.7f ? -4.5f : 4.5f), transform.position.y, transform.position.z + 0.6f);
        }

        System.Collections.IEnumerator Hop()
        {
            float time = 0f;
            while (time < 0.35f && _visual != null)
            {
                time += Time.deltaTime;
                _visual.localPosition = new Vector3(0, Mathf.Sin(time / 0.35f * Mathf.PI) * 0.25f, 0);
                yield return null;
            }
        }

        void StartCoroutineSafe(System.Collections.IEnumerator r)
        {
            if (isActiveAndEnabled) StartCoroutine(r);
        }

        public void OnTap() => GameManager.I?.OnCustomerTapped(this);

        void OnDestroy()
        {
            if (_skinMat != null) Destroy(_skinMat);
        }
    }
}
