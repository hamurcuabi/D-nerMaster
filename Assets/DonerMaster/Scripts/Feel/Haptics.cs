using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// Short vibrations. Android gets millisecond-precise pulses; iOS only supports the system vibration,
    /// so it is used for heavy events only (Core Haptics can be added later via a plugin).
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _initTried;

        static void Vibrate(long ms)
        {
            try
            {
                if (_vibrator == null && !_initTried)
                {
                    _initTried = true;
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                _vibrator?.Call("vibrate", ms);
            }
            catch (System.Exception)
            {
                // Vibration is optional — never break gameplay for it.
            }
        }
#endif

        public static void Light()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Vibrate(12);
#endif
        }

        public static void Medium()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Vibrate(30);
#endif
        }

        public static void Heavy()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Vibrate(70);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>Never called; referencing Handheld.Vibrate makes Unity add the VIBRATE permission on Android.</summary>
        static void EnsureVibratePermission()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
