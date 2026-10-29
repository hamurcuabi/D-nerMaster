// Minimal UnityEngine API surface used by the game, so the scripts can be type-checked with plain .NET.
// NOT part of the Unity project (folder is git-ignored). Signatures mirror Unity's public API.
#pragma warning disable CS0067, CS0649, CS0414, CS1998
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object => null;
        public static implicit operator bool(Object o) => o != null;
        public static T Instantiate<T>(T o) where T : Object => o;
    }
    public enum FindObjectsSortMode { None, InstanceID }
    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public string tag { get; set; }
        public T GetComponent<T>() => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
    }
    public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => true; }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator r) => null;
        public void StopCoroutine(Coroutine c) { }
    }
    public sealed class Coroutine : YieldInstruction { }
    public class YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string n) { }
        public GameObject(string n, params Type[] c) { }
        public Transform transform => null;
        public string tag { get; set; }
        public bool activeSelf => true;
        public bool activeInHierarchy => true;
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => null;
        public Component AddComponent(Type t) => null;
        public T GetComponent<T>() => default;
        public static GameObject CreatePrimitive(PrimitiveType t) => null;
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }
    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Transform parent { get; set; }
        public int childCount => 0;
        public Transform GetChild(int i) => null;
        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetAsFirstSibling() { }
        public void LookAt(Vector3 p) { }
        public void Rotate(Vector3 axis, float angle, Space s) { }
        public Vector3 TransformPoint(Vector3 p) => p;
        public Vector3 TransformDirection(Vector3 p) => p;
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public IEnumerator GetEnumerator() { yield break; }
    }
    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, offsetMin, offsetMax;
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default; public static Vector2 one => default;
        public float magnitude => 0;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator *(Vector2 a, float b) => a;
        public static float Distance(Vector2 a, Vector2 b) => 0;
        public static implicit operator Vector3(Vector2 v) => default;
        public static implicit operator Vector2(Vector3 v) => default;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default; public static Vector3 one => default; public static Vector3 up => default; public static Vector3 down => default;
        public static Vector3 forward => default; public static Vector3 back => default; public static Vector3 right => default; public static Vector3 left => default;
        public Vector3 normalized => this; public float magnitude => 0;
        public void Normalize() { }
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
        public static Vector3 operator *(float b, Vector3 a) => a;
        public static Vector3 operator /(Vector3 a, float b) => a;
        public static bool operator ==(Vector3 a, Vector3 b) => true;
        public static bool operator !=(Vector3 a, Vector3 b) => false;
        public override bool Equals(object o) => true; public override int GetHashCode() => 0;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static float Dot(Vector3 a, Vector3 b) => 0;
        public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static float Distance(Vector3 a, Vector3 b) => 0;
    }
    public struct Vector4 { public Vector4(float x, float y, float z, float w) { } }
    public struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x = x; this.y = y; } }
    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion AngleAxis(float a, Vector3 axis) => default;
        public static Quaternion LookRotation(Vector3 f) => default;
        public static Quaternion LookRotation(Vector3 f, Vector3 up) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion a, Vector3 b) => b;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default; public static Color black => default;
        public static Color operator *(Color a, float b) => a;
        public static Color Lerp(Color a, Color b, float t) => a;
        public static Color HSVToRGB(float h, float s, float v) => default;
        public static implicit operator Color32(Color c) => default;
    }
    public struct Color32 { }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public static class Mathf
    {
        public const float PI = 3.14159f, Deg2Rad = 0.0174f, Rad2Deg = 57.29f;
        public static float Sin(float f) => 0; public static float Cos(float f) => 0; public static float Atan(float f) => 0;
        public static float Atan2(float y, float x) => 0; public static float Sqrt(float f) => 0; public static float Pow(float f, float p) => 0;
        public static float Exp(float f) => 0; public static float Abs(float f) => 0; public static int Abs(int f) => 0; public static float Sign(float f) => 0;
        public static float Floor(float f) => 0; public static float Min(float a, float b) => 0; public static int Min(int a, int b) => 0;
        public static float Max(float a, float b) => 0; public static int Max(int a, int b) => 0;
        public static float Clamp(float v, float a, float b) => 0; public static int Clamp(int v, int a, int b) => 0;
        public static float Clamp01(float v) => 0; public static float Lerp(float a, float b, float t) => 0;
        public static float MoveTowards(float a, float b, float d) => 0; public static float PingPong(float t, float l) => 0;
        public static float PerlinNoise(float x, float y) => 0;
        public static int RoundToInt(float f) => 0; public static int CeilToInt(float f) => 0;
    }
    public static class Random
    {
        public static float value => 0;
        public static float Range(float a, float b) => 0;
        public static int Range(int a, int b) => 0;
        public static Vector3 insideUnitSphere => default;
        public static Vector3 onUnitSphere => default;
    }
    public static class Time { public static float time, deltaTime, unscaledTime; }
    public static class Screen { public static int width, height; public static int sleepTimeout; }
    public static class SleepTimeout { public const int NeverSleep = -1; }
    public static class Application { public static int targetFrameRate; public static bool isPlaying; public static bool isBatchMode; }
    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } }
    public static class PlayerPrefs
    {
        public static string GetString(string k, string d) => d; public static void SetString(string k, string v) { }
        public static void Save() { } public static void DeleteKey(string k) { }
    }
    public static class JsonUtility { public static string ToJson(object o) => ""; public static T FromJson<T>(string s) => default; }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default; return true; } }
    public static class Resources { public static T GetBuiltinResource<T>(string path) where T : Object => null; }
    public static class Handheld { public static void Vibrate() { } }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public float fieldOfView, nearClipPlane, farClipPlane, aspect;
        public CameraClearFlags clearFlags; public Color backgroundColor;
        public Ray ScreenPointToRay(Vector3 p) => default;
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }
    public enum CameraClearFlags { Skybox, SolidColor }
    public class AudioListener : Behaviour { public static float volume; }
    public class Light : Behaviour
    {
        public LightType type; public Color color; public float intensity, range, shadowStrength; public LightShadows shadows;
    }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public enum FogMode { Linear = 1 }
    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode; public static Color ambientSkyColor, ambientEquatorColor, ambientGroundColor, fogColor;
        public static bool fog; public static FogMode fogMode; public static float fogStartDistance, fogEndDistance;
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; } public Material material { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
    }
    public class MeshRenderer : Renderer { }
    public class MeshFilter : Component { public Mesh sharedMesh { get; set; } }
    public sealed class Mesh : Object
    {
        public void SetVertices(List<Vector3> v) { } public void SetNormals(List<Vector3> v) { }
        public void SetTriangles(List<int> t, int sub) { } public void RecalculateBounds() { }
    }
    public class Shader : Object { public static Shader Find(string n) => null; public bool isSupported => true; }
    public class Material : Object
    {
        public Material(Shader s) { }
        public Shader shader { get; set; }
        public int renderQueue { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public bool HasProperty(string n) => true;
        public void SetColor(string n, Color c) { } public void SetFloat(string n, float f) { } public void SetTexture(string n, Texture t) { }
        public void EnableKeyword(string k) { }
    }
    [Flags] public enum MaterialGlobalIlluminationFlags { None = 0, RealtimeEmissive = 1 }
    public class Texture : Object { public TextureWrapMode wrapMode { get; set; } public FilterMode filterMode { get; set; } }
    public sealed class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixels(Color[] c) { } public void Apply() { }
    }
    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 p) => null;
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m, Vector4 border) => null;
    }
    public enum SpriteMeshType { FullRect, Tight }
    public sealed class Font : Object { }
    public enum FontStyle { Normal, Bold, Italic }
    public enum TextAnchor { UpperLeft, UpperCenter, MiddleLeft, MiddleCenter }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay }
    public static class RectTransformUtility { public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 p, Camera c) => false; }
    public sealed class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }

    public class Collider : Component { }
    public class BoxCollider : Collider { public Vector3 center, size; }
    public class CapsuleCollider : Collider { public Vector3 center; public float height, radius; public int direction; }
    public class Rigidbody : Component
    {
        public float mass; public Vector3 linearVelocity, angularVelocity; public float linearDamping, angularDamping;
        public RigidbodyInterpolation interpolation; public CollisionDetectionMode collisionDetectionMode;
    }
    public enum RigidbodyInterpolation { None, Interpolate }
    public enum CollisionDetectionMode { Discrete, ContinuousSpeculative }
    public struct Ray { public Vector3 GetPoint(float d) => default; }
    public struct RaycastHit { public Collider collider => null; public float distance => 0; }
    public struct Plane
    {
        public Plane(Vector3 n, Vector3 p) { }
        public bool Raycast(Ray r, out float enter) { enter = 0; return true; }
    }
    public static class Physics
    {
        public static bool Raycast(Ray r, out RaycastHit h, float d) { h = default; return false; }
        public static RaycastHit[] RaycastAll(Ray r, float d) => null;
    }

    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string n, int len, int ch, int freq, bool stream) => null;
        public bool SetData(float[] d, int offset) => true;
    }
    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip; public bool playOnAwake, loop; public float spatialBlend, volume, pitch;
        public void Play() { }
    }

    public struct Keyframe { }
    public class AnimationCurve
    {
        public static AnimationCurve EaseInOut(float a, float b, float c, float d) => null;
        public static AnimationCurve Linear(float a, float b, float c, float d) => null;
    }
    public class Gradient { public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) { } }
    public struct GradientColorKey { public GradientColorKey(Color c, float t) { } }
    public struct GradientAlphaKey { public GradientAlphaKey(float a, float t) { } }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemSimulationSpace { Local, World }
    public enum ParticleSystemShapeType { Sphere }
    public enum ParticleSystemRenderMode { Billboard, Mesh }
    public sealed class ParticleSystemRenderer : Renderer { public ParticleSystemRenderMode renderMode; public Mesh mesh; }
    public sealed class ParticleSystem : Component
    {
        public void Stop(bool children, ParticleSystemStopBehavior b) { }
        public void Play() { }
        public void Emit(EmitParams p, int count) { }
        public MainModule main => default; public EmissionModule emission => default; public ShapeModule shape => default;
        public ColorOverLifetimeModule colorOverLifetime => default; public SizeOverLifetimeModule sizeOverLifetime => default;
        public struct MainModule
        {
            public bool playOnAwake { get; set; } public bool loop { get; set; }
            public MinMaxCurve startLifetime { get; set; } public MinMaxCurve startSpeed { get; set; } public MinMaxCurve startSize { get; set; }
            public MinMaxGradient startColor { get; set; } public MinMaxCurve gravityModifier { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; } public int maxParticles { get; set; }
        }
        public struct EmissionModule { public MinMaxCurve rateOverTime { get; set; } }
        public struct ShapeModule { public ParticleSystemShapeType shapeType { get; set; } public float radius { get; set; } }
        public struct ColorOverLifetimeModule { public bool enabled { get; set; } public MinMaxGradient color { get; set; } }
        public struct SizeOverLifetimeModule { public bool enabled { get; set; } public MinMaxCurve size { get; set; } }
        public struct MinMaxCurve
        {
            public MinMaxCurve(float c) { } public MinMaxCurve(float a, float b) { } public MinMaxCurve(float m, AnimationCurve c) { }
            public static implicit operator MinMaxCurve(float c) => default;
        }
        public struct MinMaxGradient
        {
            public static implicit operator MinMaxGradient(Color c) => default;
            public static implicit operator MinMaxGradient(Gradient g) => default;
        }
        public struct EmitParams { public Vector3 position { get; set; } public bool applyShapeToPosition { get; set; } public Color32 startColor { get; set; } }
    }

    public static class Input
    {
        public static int touchCount => 0; public static Touch GetTouch(int i) => default;
        public static Vector3 mousePosition => default; public static bool GetMouseButton(int b) => false;
    }
    public struct Touch { public Vector2 position => default; public TouchPhase phase => default; }
    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
}

namespace UnityEngine.Rendering
{
    public class RenderPipelineAsset : Object { }
    public static class GraphicsSettings { public static RenderPipelineAsset currentRenderPipeline => null; }
    public enum BlendMode { SrcAlpha = 5, OneMinusSrcAlpha = 10 }
    public enum RenderQueue { Transparent = 3000 }
    public enum ShadowCastingMode { Off, On }
    public enum AmbientMode { Skybox, Trilight, Flat }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public class UnityEvent { public void AddListener(UnityAction a) { } }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class EventSystem : UIBehaviour { public static EventSystem current => null; public void RaycastAll(PointerEventData d, List<RaycastResult> r) { } }
    public class BaseEventData { public BaseEventData(EventSystem es) { } }
    public class PointerEventData : BaseEventData { public PointerEventData(EventSystem es) : base(es) { } public Vector2 position { get; set; } }
    public struct RaycastResult { public GameObject gameObject { get; set; } }
    public class StandaloneInputModule : UIBehaviour { }
}

namespace UnityEngine.UI
{
    public class Graphic : EventSystems.UIBehaviour
    {
        public Color color { get; set; } public bool raycastTarget { get; set; } public RectTransform rectTransform => null;
    }
    public class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic { public enum Type { Simple, Sliced } public Sprite sprite { get; set; } public Type type { get; set; } }
    public class Text : MaskableGraphic
    {
        public Font font { get; set; } public string text { get; set; } public int fontSize { get; set; } public TextAnchor alignment { get; set; }
        public FontStyle fontStyle { get; set; } public bool supportRichText { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; } public VerticalWrapMode verticalOverflow { get; set; }
    }
    public class BaseMeshEffect : EventSystems.UIBehaviour { }
    public class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
    public class Outline : Shadow { }
    public struct ColorBlock { public Color highlightedColor { get; set; } public Color pressedColor { get; set; } public Color disabledColor { get; set; } }
    public class Selectable : EventSystems.UIBehaviour { public Graphic targetGraphic { get; set; } public ColorBlock colors { get; set; } public bool interactable { get; set; } }
    public class Button : Selectable { public class ButtonClickedEvent : Events.UnityEvent { } public ButtonClickedEvent onClick { get; set; } }
    public class CanvasScaler : EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize }
        public ScaleMode uiScaleMode { get; set; } public Vector2 referenceResolution { get; set; } public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : EventSystems.UIBehaviour { }
}

namespace UnityEngine.SceneManagement { public struct Scene { } }

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)] public class MenuItem : Attribute { public MenuItem(string p) { } }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } public string path => ""; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public enum UIOrientation { Portrait, AutoRotation }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2 }
    public enum AndroidSdkVersions { AndroidApiLevel24 = 24 }
    public enum BuildTarget { Android = 13 }
    public enum BuildTargetGroup { Android = 7 }
    [Flags] public enum BuildOptions { None = 0 }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildTargetGroup targetGroup; public BuildOptions options; }
    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => null; }
    public static class EditorUserBuildSettings { public static bool buildAppBundle, development; }
    public static class EditorApplication { public static void Exit(int c) { } }
    public static class AssetDatabase { public static void Refresh() { } }
    public static class PlayerSettings
    {
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static string companyName, productName, bundleVersion;
        public static void SetApplicationIdentifier(Build.NamedBuildTarget t, string id) { }
        public static void SetScriptingBackend(Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static class Android
        {
            public static int bundleVersionCode; public static AndroidArchitecture targetArchitectures; public static AndroidSdkVersions minSdkVersion;
            public static bool useCustomKeystore; public static string keystoreName, keystorePass, keyaliasName, keyaliasPass;
        }
    }
}
namespace UnityEditor.Build { public struct NamedBuildTarget { public static NamedBuildTarget Android => default; } }
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public BuildResult result; public int totalErrors; public ulong totalSize; }
    public class BuildReport { public BuildSummary summary => default; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p) => true;
    }
}
