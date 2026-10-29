using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// The living heart of the game. Four surface zones cook at different rates depending on how much
    /// they face the heater; slicing removes the outer cooked layer of the zone facing the player,
    /// exposing rawer meat underneath. Meat stock shrinks the spit until it must be reloaded.
    /// </summary>
    public class DonerSpit : MonoBehaviour, ITappable
    {
        public const int ZoneCount = 4;
        public const float MeatBottom = 0.26f;
        public const float MeatHeight = 1.45f;
        public const float RadiusBottom = 0.30f;
        public const float RadiusTop = 0.43f;
        const float MinScale = 0.42f;

        public readonly float[] ZoneCook = new float[ZoneCount];
        public int Heat { get; private set; } = 1;
        public float Meat { get; private set; } = 1f;         // 1 = fresh spit, 0 = empty
        public bool Reloading { get; private set; }
        public float ReloadProgress { get; private set; }
        public bool IsEmpty => Meat <= 0.0001f && !Reloading;

        Transform _pivot;
        readonly Renderer[] _zoneRenderers = new Renderer[ZoneCount];
        readonly Material[] _zoneMats = new Material[ZoneCount];
        readonly Vector3[] _zoneDirLocal = new Vector3[ZoneCount];
        Material _heaterMat;
        Light _heaterLight;
        float _angle;
        float _manualVelocity;
        float _smokeTimer;
        float _dripTimer;

        public float AutoRotateSpeed = 16f;

        public static DonerSpit Create(Transform parent, Vector3 localPos)
        {
            var root = Build.Empty("DonerSpit", parent, localPos);
            var spit = root.AddComponent<DonerSpit>();
            spit.BuildVisuals();
            return spit;
        }

        void BuildVisuals()
        {
            var steel = new Color(0.7f, 0.72f, 0.75f);
            var dark = new Color(0.18f, 0.18f, 0.2f);

            // Skewer, plates and motor
            Build.Prim(PrimitiveType.Cylinder, "Rod", transform, new Vector3(0, 1.05f, 0), new Vector3(0.035f, 1.0f, 0.035f), steel, false, 0.8f, 0.9f);
            Build.Prim(PrimitiveType.Cylinder, "BottomPlate", transform, new Vector3(0, MeatBottom - 0.02f, 0), new Vector3(0.62f, 0.015f, 0.62f), steel, false, 0.8f, 0.9f);
            Build.Prim(PrimitiveType.Cylinder, "TopPlate", transform, new Vector3(0, MeatBottom + MeatHeight + 0.03f, 0), new Vector3(0.36f, 0.012f, 0.36f), steel, false, 0.8f, 0.9f);
            Build.Prim(PrimitiveType.Cube, "Motor", transform, new Vector3(0, MeatBottom + MeatHeight + 0.2f, 0.1f), new Vector3(0.28f, 0.18f, 0.32f), dark, false, 0.5f, 0.4f);

            // Heater panel behind the spit (+Z), glowing towards the meat
            var heater = Build.Empty("Heater", transform, new Vector3(0, MeatBottom + MeatHeight / 2f, 0.68f));
            Build.Prim(PrimitiveType.Cube, "Frame", heater.transform, Vector3.zero, new Vector3(0.95f, MeatHeight + 0.2f, 0.1f), dark, false, 0.4f, 0.6f);
            var glow = Build.Prim(PrimitiveType.Cube, "Glow", heater.transform, new Vector3(0, 0, -0.055f), new Vector3(0.78f, MeatHeight - 0.05f, 0.01f), Color.black);
            _heaterMat = Mats.Emissive(new Color(1f, 0.35f, 0.05f), 1f);
            glow.GetComponent<Renderer>().sharedMaterial = _heaterMat;
            // grill bars over the glow
            for (int i = 0; i < 7; i++)
            {
                float y = -MeatHeight / 2f + 0.12f + i * (MeatHeight - 0.24f) / 6f;
                Build.Prim(PrimitiveType.Cube, "Bar", heater.transform, new Vector3(0, y, -0.065f), new Vector3(0.8f, 0.025f, 0.012f), dark);
            }
            var lightGo = Build.Empty("HeaterLight", transform, new Vector3(0, MeatBottom + MeatHeight / 2f, 0.35f));
            _heaterLight = lightGo.AddComponent<Light>();
            _heaterLight.type = LightType.Point;
            _heaterLight.color = new Color(1f, 0.55f, 0.2f);
            _heaterLight.range = 2.2f;

            // Meat: four quarter sectors on a rotating pivot
            _pivot = Build.Empty("MeatPivot", transform, new Vector3(0, MeatBottom, 0)).transform;
            for (int i = 0; i < ZoneCount; i++)
            {
                float a0 = i * 90f, a1 = a0 + 90f;
                var go = new GameObject("Zone" + i);
                go.transform.SetParent(_pivot, false);
                go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sector(a0 + 0.6f, a1 - 0.6f, MeatHeight, RadiusBottom, RadiusTop, 12);
                var r = go.AddComponent<MeshRenderer>();
                _zoneMats[i] = Mats.Make(MeatSlice.CookColor(10f), 0.5f);
                r.sharedMaterial = _zoneMats[i];
                _zoneRenderers[i] = r;
                float mid = (a0 + 45f) * Mathf.Deg2Rad;
                _zoneDirLocal[i] = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
                ZoneCook[i] = 12f;
            }
            // Thin fat-layer rings for a stacked-meat look
            for (int i = 1; i < 6; i++)
            {
                float y = MeatHeight * i / 6f;
                float r = Mathf.Lerp(RadiusBottom, RadiusTop, i / 6f) * 2.02f;
                var ring = Build.Prim(PrimitiveType.Cylinder, "FatRing", _pivot, new Vector3(0, y, 0), new Vector3(r, 0.004f, r), new Color(0.93f, 0.82f, 0.68f));
                ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Collider used for "is the finger on the doner?"
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.direction = 1;
            col.center = new Vector3(0, MeatBottom + MeatHeight / 2f, 0);
            col.height = MeatHeight + 0.2f;
            col.radius = RadiusTop + 0.08f;
        }

        public void CycleHeat()
        {
            Heat = (Heat + 1) % 3;
            AudioManager.Play(Sfx.Whoosh, 0.6f, 0.8f + Heat * 0.2f);
            Haptics.Light();
        }

        public void AddManualSpin(float degreesPerSecond) => _manualVelocity += degreesPerSecond;
        public void RotateBy(float degrees) => _angle += degrees;

        void Update()
        {
            float dt = Time.deltaTime;
            bool playing = GameManager.I != null && GameManager.I.IsPlaying;

            if (Reloading)
            {
                float reload = GameData.BaseReloadTime;
                ReloadProgress += dt / reload;
                if (ReloadProgress >= 1f) FinishReload();
            }

            // Rotation: steady motor + player flick with damping
            _angle += (AutoRotateSpeed + _manualVelocity) * dt;
            _manualVelocity = Mathf.Lerp(_manualVelocity, 0f, 1f - Mathf.Exp(-dt * 3f));
            _pivot.localRotation = Quaternion.Euler(0f, _angle, 0f);

            // Cooking
            var heaterDir = transform.forward;
            float cookSpeed = GameData.CookSpeed(SaveSystem.Data.Level(UpgradeId.Burner));
            bool anyBurning = false;
            float hottest = 0f;
            if (playing && Meat > 0f)
            {
                for (int i = 0; i < ZoneCount; i++)
                {
                    var wdir = _pivot.TransformDirection(_zoneDirLocal[i]);
                    float facing = Mathf.Clamp01(Vector3.Dot(wdir.normalized, heaterDir));
                    float rate = GameData.HeatRates[Heat] * cookSpeed * (0.25f + 0.75f * facing);
                    if (ZoneCook[i] > 80f) rate *= 0.55f; // a forgiving window before it burns
                    ZoneCook[i] = Mathf.Min(100f, ZoneCook[i] + rate * dt);
                    if (ZoneCook[i] > 92f) anyBurning = true;
                    hottest = Mathf.Max(hottest, ZoneCook[i]);
                }
            }

            // Visuals
            for (int i = 0; i < ZoneCount; i++) Mats.SetColor(_zoneMats[i], MeatSlice.CookColor(ZoneCook[i]));
            float s = Mathf.Lerp(MinScale, 1f, Meat);
            _pivot.localScale = new Vector3(s, 1f, s);
            _pivot.gameObject.SetActive(Meat > 0f || Reloading);

            float heatGlow = playing ? (0.5f + Heat * 0.9f) : 0.3f;
            Mats.SetEmission(_heaterMat, new Color(1f, 0.32f, 0.04f) * heatGlow * (0.92f + 0.08f * Mathf.Sin(Time.time * 7f)));
            _heaterLight.intensity = Mathf.Lerp(_heaterLight.intensity, playing ? 0.6f + Heat * 0.7f : 0.3f, dt * 3f);

            // Audio: sizzle follows heat and doneness
            float sizzle = playing && Meat > 0f ? 0.18f + Heat * 0.12f + (hottest > 80f ? 0.1f : 0f) : 0f;
            AudioManager.SetSizzle(sizzle, anyBurning ? 1.15f : 1f);

            // Burning smoke and fat drips
            if (anyBurning)
            {
                _smokeTimer -= dt;
                if (_smokeTimer <= 0f)
                {
                    _smokeTimer = 0.25f;
                    FX.Smoke(transform.position + Vector3.up * (MeatBottom + Random.Range(0.3f, MeatHeight)) - transform.forward * 0.3f, 1);
                }
            }
            if (playing && Meat > 0f)
            {
                _dripTimer -= dt;
                if (_dripTimer <= 0f)
                {
                    _dripTimer = Random.Range(0.4f, 1.2f) / (1 + Heat);
                    var p = transform.position + Vector3.up * (MeatBottom + Random.Range(0.1f, MeatHeight)) - transform.forward * RadiusTop * s;
                    FX.Grease(p, 1);
                }
            }
        }

        /// <summary>Index of the zone facing the camera (the one the knife cuts).</summary>
        public int FrontZone
        {
            get
            {
                var cam = Camera.main;
                var toCam = cam != null ? cam.transform.position - transform.position : -transform.forward;
                toCam.y = 0f;
                toCam.Normalize();
                int best = 0;
                float bestDot = -2f;
                for (int i = 0; i < ZoneCount; i++)
                {
                    float d = Vector3.Dot(_pivot.TransformDirection(_zoneDirLocal[i]).normalized, toCam);
                    if (d > bestDot) { bestDot = d; best = i; }
                }
                return best;
            }
        }

        public bool AnyZoneBurning
        {
            get
            {
                for (int i = 0; i < ZoneCount; i++) if (ZoneCook[i] > 92f) return true;
                return false;
            }
        }

        public float FrontRadius => Mathf.Lerp(MinScale, 1f, Meat) * Mathf.Lerp(RadiusBottom, RadiusTop, 0.5f);

        public float RadiusAt(float worldY)
        {
            float k = Mathf.Clamp01((worldY - (transform.position.y + MeatBottom)) / MeatHeight);
            return Mathf.Lerp(RadiusBottom, RadiusTop, k) * Mathf.Lerp(MinScale, 1f, Meat);
        }

        public float MeatMinY => transform.position.y + MeatBottom;
        public float MeatMaxY => transform.position.y + MeatBottom + MeatHeight;

        /// <summary>Shave one piece off the front zone. Returns its doneness, or -1 if there is no meat.</summary>
        public float ShaveFront()
        {
            if (Meat <= 0f || Reloading) return -1f;
            int z = FrontZone;
            float cook = ZoneCook[z];
            // The layer underneath has been warming too, but is less done than the surface.
            ZoneCook[z] = Mathf.Max(8f, cook * GameData.FreshLayerCarry + 4f);
            Meat = Mathf.Max(0f, Meat - 1f / GameData.PiecesPerSpit);
            Juice.Punch(_zoneRenderers[z].transform, 0.04f, 0.15f);
            return cook;
        }

        public void ResetForDay()
        {
            Reloading = false;
            ReloadProgress = 0f;
            Meat = 1f;
            Heat = 1;
            _manualVelocity = 0f;
            for (int i = 0; i < ZoneCount; i++) ZoneCook[i] = 30f + i * 12f; // a little pre-heated so day starts fast
        }

        public void StartReload()
        {
            if (Reloading || Meat > 0f) return;
            Reloading = true;
            ReloadProgress = 0f;
            AudioManager.Play(Sfx.Whoosh, 0.8f, 0.7f);
        }

        void FinishReload()
        {
            Reloading = false;
            ReloadProgress = 0f;
            Meat = 1f;
            for (int i = 0; i < ZoneCount; i++) ZoneCook[i] = 10f;
            for (int i = 0; i < ZoneCount; i++) Juice.Punch(_zoneRenderers[i].transform, 0.12f, 0.35f);
            AudioManager.Play(Sfx.Pop, 0.8f, 0.7f);
            Haptics.Medium();
        }

        public void OnTap()
        {
            if (Meat <= 0f && !Reloading) StartReload();
        }
    }
}
