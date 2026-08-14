// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering;

// namespace Sydewa
// {
// [ExecuteAlways]
// public class LightingManager : MonoBehaviour, IDataPersistence
// {
//     #region Parameters

//     [Header("Sun")]
//     [SerializeField] private Light SunDirectionalLight;
//     public float sunYAngle = 170f;
//     public float dayLightIntensity = 1.2f;

//     [Space(5)]
//     [Header("Ambient")]
//     public Color dayAmbientColor = new Color(0.35f, 0.35f, 0.35f);
//     public Color nightAmbientColor = Color.black;

//     [Space(5)]
//     [Header("Fog")]
//     public Color dayFogColor = new Color(0.4f, 0.4f, 0.4f);
//     public Color nightFogColor = Color.black;
//     [Range(0f, 0.1f)] public float dayFogDensity = 0.015f;
//     [Range(0f, 0.1f)] public float nightFogDensity = 0.003f;

//     [Space(5)]
//     [Header("Skybox")]
//     public Material daySkyboxMaterial;
//     public Material nightSkyboxMaterial;
//     private Material _activeSkybox;

//     [Space(5)]
//     [Header("Moon")]
//     public bool IsMoonActive;
//     public Light MoonDirectionalLight;
//     [Range(0f, 1f)] public float moonNightIntensity = 0.05f;

//     [Space(10)]
//     [Header("Day Cycle")]
//     public bool IsDayCycleOn = true;
//     public bool RandomStartTime;
//     [Range(0, 24)] public float TimeOfDay = 12f;
//     [Range(0, 24)] public float StartTime = 12f;
//     public float CycleDuration = 360f;
//     [Range(1, 1200)] public float DayCycleDuration = 80f;
//     [Range(1, 1200)] public float nightCycleDuration = 240f;

//     [Space(10)]
//     [Header("Events")]
//     public bool IsEventsOn;
//     public List<EventInfo> events;
//     [SerializeField] private float eventsTolerance = 0.2f;
//     [SerializeField][Range(0f, 24f)] private float ResetEventsTime = 0.1f;
//     private bool DayCycleCompleted;

//     #endregion

//     private void Awake()
//     {
//         RenderSettings.ambientMode = AmbientMode.Flat;
//         RenderSettings.fog = true;
//         RenderSettings.fogMode = FogMode.ExponentialSquared;

//         TimeOfDay = RandomStartTime ? Random.Range(0f, 24f) : StartTime % 24f;

//         if (IsEventsOn) ResetEvents();
//     }

//     public void loadData(GameData data)
//     {
//         TimeOfDay = data.timeOfDay;
//     }

//     public void saveData(ref GameData data)
//     {
//         data.timeOfDay = TimeOfDay;
//     }

//     private void Update()
//     {
//         if (Application.isPlaying)
//         {
//             if (IsDayCycleOn)
//             {
//                 // Freeze time near midnight when lure is crafted
//                 if (GameManager.instance != null && GameManager.instance.lureCrafted
//                     && TimeOfDay >= 0f && TimeOfDay <= 1f)
//                 {
//                     CycleDuration = 1000f;
//                     TimeOfDay = 0f;
//                 }

//                 // Smoothly ramp cycle speed: slow during night, fast during day
//                 bool isNightNow = GameManager.instance != null && GameManager.instance.isNight;
//                 if (isNightNow && CycleDuration < nightCycleDuration)
//                     CycleDuration += 6;
//                 else if (!isNightNow && CycleDuration > DayCycleDuration)
//                     CycleDuration -= 6;

//                 TimeOfDay += (Time.deltaTime / CycleDuration) * 24f;
//                 TimeOfDay %= 24f;
//             }
//         }

//         UpdateLighting();

//         if (IsEventsOn) HandleEvents();
//     }

//     private void UpdateLighting()
//     {
//         if (SunDirectionalLight == null) return;

//         // Rotate sun on X: noon (TimeOfDay=12) → pointing straight down, midnight → pointing straight up
//         float timePercent = TimeOfDay / 24f;
//         SunDirectionalLight.transform.rotation = Quaternion.Euler(timePercent * 360f - 90f, sunYAngle, 0f);

//         // sunHeight: -1 = midnight (below horizon), 0 = horizon, 1 = noon (above horizon)
//         float sunHeight = -SunDirectionalLight.transform.forward.y;
//         bool aboveHorizon = sunHeight > 0f;

//         // Drive isNight from sun position
//         if (GameManager.instance != null)
//             GameManager.instance.isNight = !aboveHorizon;

//         // Sun intensity: 0 below horizon, rises naturally with sun height
//         SunDirectionalLight.intensity = aboveHorizon ? Mathf.Clamp01(sunHeight) * dayLightIntensity : 0f;

//         // Ambient: smooth blend around horizon (-0.2 to 0.2 range gives a nice dusk/dawn window)
//         float ambientT = Mathf.Clamp01((sunHeight + 0.2f) / 0.4f);
//         RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, ambientT);

//         // Fog: blend based on sun height
//         float fogT = Mathf.Clamp01((sunHeight + 0.3f) / 0.6f);
//         RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, fogT);
//         RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, fogT);
//         if(GameManager.instance != null)
//             {
//                 if (GameManager.instance.holdingtorch)
//                 {
//                     RenderSettings.fogDensity -= 0.03f;
//                 }
//                 if(GameManager.instance.emberstoneactive)
//                 {
//                     RenderSettings.fogDensity -= 0.009f;
//                 }
                
//             }

//         // Skybox: switch at midnight
//         SetSkybox(aboveHorizon ? daySkyboxMaterial : nightSkyboxMaterial);  

//         // Moon: always directly opposite the sun
//         if (IsMoonActive && MoonDirectionalLight != null)
//         {
//             MoonDirectionalLight.transform.rotation = Quaternion.LookRotation(-SunDirectionalLight.transform.forward);
//             MoonDirectionalLight.intensity = aboveHorizon ? 0f : moonNightIntensity;
//         }
//     }

//     private void SetSkybox(Material mat)
//     {
//         if (mat == null || mat == _activeSkybox) return;
//         _activeSkybox = mat;
//         RenderSettings.skybox = mat;
//         DynamicGI.UpdateEnvironment();
//     }

//     public void ResetEvents()
//     {
//         foreach (var e in events)
//             e.executed = false;
//     }

//     private void HandleEvents()
//     {
//         foreach (var eventInfo in events)
//         {
//             if (Mathf.Abs(eventInfo.Time - TimeOfDay) <= eventsTolerance && !eventInfo.executed)
//             {
//                 eventInfo.executed = true;
//                 eventInfo.Event.Invoke();
//                 Debug.Log("Event: " + eventInfo.eventName);
//             }
//         }

//         if (!DayCycleCompleted && TimeOfDay < ResetEventsTime)
//         {
//             DayCycleCompleted = true;
//             ResetEvents();
//             Debug.Log("Day completed + reset");
//         }
//         else if (TimeOfDay > ResetEventsTime)
//         {
//             DayCycleCompleted = false;
//         }
//     }

//     private void OnValidate()
//     {
//         if (SunDirectionalLight == null && RenderSettings.sun != null)
//             SunDirectionalLight = RenderSettings.sun;
//     }
// }
// }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Events;

namespace Sydewa
{
[ExecuteAlways]
public class LightingManager : MonoBehaviour, IDataPersistence
{
        //Credits to "Probably Spoonie" in youtube and his video https://www.youtube.com/watch?v=m9hj9PdO328&ab_channel=ProbablySpoonie
        //He made the original script and i've HEAVILY modified it to fit my needs. 
        //Hopefully this modified version is useful to you
    #region Parameters
        //Scene References
        [SerializeField] private Light SunDirectionalLight;
        // [SerializeField] private LightingPreset Preset;

        //Rotation axis
        public enum RotationAxis{X,Y}
        [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;

        [Space(10)]
        //Everything needed for the day cycle
        //morningInterval and afterNoonInterval is from where you consider morning and the after noon starts and ends. Only values from 0 to 1 (to calculate: TimeOfDayYouWant/24 )
        [Header ("Day Cycle Parameters")]
        public bool IsDayCycleOn = true;
        public bool RandomStartTime;
        public float startOfNight = 12f;
        public float endOfNight = 12f;
        [Range(0, 24)] public float TimeOfDay = 12f;
        [Range(0, 24)] public float StartTime = 12f;
            //How long the day cycle will be in seconds
        public float CycleDuration = 360f;
        [Range(1, 1200)] public float DayCycleDuration = 80f;
        [Range(1, 1200)] public float nightCycleDuration = 240f;
        
        public Vector2 morningInterval = new Vector2(0f, 0.5f);
        public Vector2 afterNoonInterval = new Vector2(0.5f, 1f);
        public Vector2 lightIntensity = new Vector2(0f, 1f);
        private float intensity;

        [Space(5)]
        [Header("Skybox")]
        public Material daySkyboxMaterial;
        public Material nightSkyboxMaterial;
        [Tooltip("Sun height at which the skybox swaps. Keep <= the fog fully-night point (-0.3) so the swap happens while it's already dark instead of mid-transition.")]
        public float skyboxSwitchHeight = -0.3f;
        private Material _activeSkybox;


        [Space(10)]

        [Header ("Shadows Parameters")]
        public bool IsShadowChangeOn;
        [Range (0f, 1f)]public float shadowStrength = 0.5f;
        private float _shadowStrength;

        [Space(10)]

        //You can delete everything that uses this parameters if you don't have a custom skybox that you want to edit with this script or just uncheck the IsSkyBoxOn in the inspector and you'll be fine
        [Header ("Skybox Parameters")]
        public bool IsSkyBoxOn;
        public Material skyboxMat;
        public string customPropertyName;
        private float skyboxParam;

        [Space(10)]

        [Header("Moon Parameters")]
        public bool IsMoonActive;
        public Light MoonDirectionalLight;
        public bool IsMoonRotationOn;
        public Vector2 MoonIntensity = new Vector2(0f, 1f);
        [Range(0f, 1f)] public float MoonShadowStrength = 0.5f;

        [Space(10)]
        [Header("Events")]
        //Enable or disable the events
        public bool IsEventsOn;
        //Create events
        public List<EventInfo> events;
            //The events won't happen EXACTLY at the time you want, but it will be as close as possible. To make the script invoke the event you'll need to have this tolerance. I recommend values from 0.2 and 0.05
            //but you can experiment and try it yourself. This is only to prevent the script skiping the event because it didn't exactly get to 6pm or whatever the time of the event was. 
        [SerializeField] private float eventsTolerance = 0.2f;
        [SerializeField][Range(0f, 24f)] private float ResetEventsTime = 0.1f;
        private bool DayCycleCompleted;

        [Space(10)]
        [Header("Fog")]
        public Color dayFogColor = new Color(0.4f, 0.4f, 0.4f);
        public Color nightFogColor = Color.black;
        [Range(0f, 0.1f)] public float dayFogDensity = 0.015f;
        [Range(0f, 0.1f)] public float nightFogDensity = 0.003f;

    #endregion

        private void Awake()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            if(IsDayCycleOn)
            {
                if(RandomStartTime)
                {
                    TimeOfDay = Random.Range(0f, 24f);
                    Debug.Log("Random Start Time: "+TimeOfDay);
                }
                else if(!RandomStartTime)
                {
                    TimeOfDay = StartTime;
                    TimeOfDay %= 24;
                }
            }

            if(IsEventsOn)
            {
                ResetEvents();
            }
        }

        public void loadData(GameData data)
        {
            TimeOfDay = data.timeOfDay;
        }

        public void saveData(ref GameData data)
        {
            data.timeOfDay = TimeOfDay;
        }

        private void Update()
        {
            if (GameManager.instance != null)
            {
                if(TimeOfDay <= endOfNight || TimeOfDay >= startOfNight)
                {
                    
                    GameManager.instance.isNight = true;
                    if (GameManager.instance.lureCrafted && (TimeOfDay >= 0.0f && TimeOfDay <= 1.0f))
                    {
                        CycleDuration = 1000.0f;
                        TimeOfDay = 0.0f;
                    }
                    if (CycleDuration <= nightCycleDuration)
                        {
                            CycleDuration += 6;
                        }
                }
                else
                {
                    GameManager.instance.isNight = false;
                    if(CycleDuration > DayCycleDuration)
                    {
                        CycleDuration -= 6;
                    }
                }
            }


            // if (Preset == null)
            //     return;

            if (Application.isPlaying)
            {


                //(Replace with a reference to your game time if needed)
                if(IsDayCycleOn)
                {
                    TimeOfDay += (Time.deltaTime / CycleDuration) * 24f;
                    TimeOfDay %= 24; //Modulus to ensure always between 0-24
                }
                UpdateLighting(TimeOfDay / 24f);

                if (IsMoonActive && MoonDirectionalLight != null)
                {
                    UpdateMoonLighting(TimeOfDay / 24f);
                }
            }
            else
            {
                UpdateLighting(TimeOfDay / 24f);
            }




            //Detects when an event should trigger
            if(IsEventsOn)
            {
                foreach (var eventInfo in events)
                {
                    float timeDifference = Mathf.Abs(eventInfo.Time - TimeOfDay);
                    if (timeDifference <= eventsTolerance && !eventInfo.executed)
                    {
                        eventInfo.executed = true;
                        eventInfo.Event.Invoke();
                        Debug.Log("Event: " + eventInfo.eventName);
                    }
                }

                if (!DayCycleCompleted && TimeOfDay < ResetEventsTime)
                {
                    DayCycleCompleted = true;
                    ResetEvents();
                    
                    Debug.Log("Day completed + reset");
                }
                else if(TimeOfDay > ResetEventsTime)
                {
                    DayCycleCompleted = false;
                }
            }
        }

        private void SetSkybox(Material mat)
        {
            if (mat == null || mat == _activeSkybox) return;
            _activeSkybox = mat;
            RenderSettings.skybox = mat;
            DynamicGI.UpdateEnvironment();
        }

        //This function is called whenever we want to reset all events
        public void ResetEvents()
        {
            foreach(var eventInfo in events)
            {
                eventInfo.executed = false;
            }
        }

        private void UpdateLighting(float timePercent)
        {
            //Set ambient and fog
            // RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
            // RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

            //If the directional light is set then rotate and set it's color, I actually rarely use the localRotation because it casts tall shadows unless you clamp the value
            if (SunDirectionalLight != null)
            {
                // SunDirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);

                Vector3 rotationEuler = Vector3.zero;

                switch (rotationAxis)
                {
                    case RotationAxis.X:
                        rotationEuler = new Vector3((timePercent * 360f) - 90f, SunDirectionalLight.transform.localRotation.y, SunDirectionalLight.transform.localRotation.z);
                        break;
                    case RotationAxis.Y:
                        rotationEuler = new Vector3(SunDirectionalLight.transform.localRotation.x, (timePercent * 360f) - 90f, SunDirectionalLight.transform.localRotation.z);
                        break;
                }
                SunDirectionalLight.transform.rotation = Quaternion.Euler(rotationEuler);

                // Fog: blend based on sun height (-1 = midnight, 0 = horizon, 1 = noon), gives a much darker night than the timePercent-driven fog did
                float sunHeight = -SunDirectionalLight.transform.forward.y;
                float fogT = Mathf.Clamp01((sunHeight + 0.3f) / 0.6f);
                RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, fogT);
                RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, fogT);
                if (GameManager.instance != null)
                {
                    if (GameManager.instance.holdingtorch && GameManager.instance.isNight)
                        RenderSettings.fogDensity -= 0.03f;
                    if (GameManager.instance.emberstoneactive && GameManager.instance.isNight)
                        RenderSettings.fogDensity -= 0.009f;
                    RenderSettings.fogDensity = Mathf.Max(0f, RenderSettings.fogDensity);
                }

                // Skybox: swap once fog is already fully in its night state, so the swap is hidden in darkness
                SetSkybox(sunHeight > skyboxSwitchHeight ? daySkyboxMaterial : nightSkyboxMaterial);

                //Changes light intensity (and a shader parameter) depending on timePercent
                if (timePercent < morningInterval.x || timePercent > afterNoonInterval.y)
                {
                    // Night
                    intensity = lightIntensity.x;
                    skyboxParam = 1f;
                    _shadowStrength = 0f;
                }
                else if (timePercent >= morningInterval.x && timePercent <= morningInterval.y)
                {
                    // Morning
                    float morningNormalizedTime = (timePercent - morningInterval.x) / (morningInterval.y - morningInterval.x);
                    intensity = Mathf.Lerp(lightIntensity.x, lightIntensity.y, morningNormalizedTime);

                    if(IsSkyBoxOn)
                        skyboxParam = Mathf.Lerp(1f, 0f, morningNormalizedTime);

                    _shadowStrength = Mathf.Lerp(0f, shadowStrength, morningNormalizedTime);
                }
                else if (timePercent > morningInterval.y && timePercent < afterNoonInterval.x)
                {
                    // Day
                    intensity = lightIntensity.y;
                    skyboxParam = 0f;
                    _shadowStrength = shadowStrength;
                }
                else if(timePercent >= afterNoonInterval.x && timePercent <= afterNoonInterval.y)
                {
                    // Afternoon
                    float afternoonNormalizedTime = (timePercent - afterNoonInterval.x) / (afterNoonInterval.y - afterNoonInterval.x);
                    intensity = Mathf.Lerp(lightIntensity.y, lightIntensity.x, afternoonNormalizedTime);

                    if(IsSkyBoxOn)
                        skyboxParam = Mathf.Lerp(0f, 1f, afternoonNormalizedTime);
                        
                    _shadowStrength = Mathf.Lerp(0f, shadowStrength, afternoonNormalizedTime);
                }

                // Set light intensity and shadow strength - hard gate off sunHeight so the light is
                // truly off below the horizon, regardless of how the morning/afternoon intervals are tuned
                bool sunAboveHorizon = sunHeight > 0f;
                SunDirectionalLight.intensity = sunAboveHorizon ? intensity : 0f;
                SunDirectionalLight.enabled = sunAboveHorizon;
                if (IsShadowChangeOn)
                    SunDirectionalLight.shadowStrength = sunAboveHorizon ? _shadowStrength : 0f;

                // Set skybox parameter
                if(IsSkyBoxOn)
                {
                    skyboxMat.SetFloat(customPropertyName, skyboxParam);
                }

            }
        }

        private void UpdateMoonLighting(float timePercent)
        {
            if (timePercent < morningInterval.x || timePercent > afterNoonInterval.y)
            {
                // Night
                MoonDirectionalLight.intensity = MoonIntensity.y;
                MoonDirectionalLight.shadowStrength = MoonShadowStrength;
            }
            else if (timePercent >= morningInterval.x && timePercent <= morningInterval.y)
            {
                // Morning
                float morningNormalizedTime = (timePercent - morningInterval.x) / (morningInterval.y - morningInterval.x);
                float morningIntensity = Mathf.Lerp(MoonIntensity.y, MoonIntensity.x, morningNormalizedTime);
                float morningShadowStrength = Mathf.Lerp(MoonShadowStrength, 1f, morningNormalizedTime);
                MoonDirectionalLight.intensity = morningIntensity;
                MoonDirectionalLight.shadowStrength = morningShadowStrength;
            }
            else if (timePercent > morningInterval.y && timePercent < afterNoonInterval.x)
            {
                // Day
                MoonDirectionalLight.intensity = MoonIntensity.x;
                MoonDirectionalLight.shadowStrength = 0f;
            }
            else if (timePercent >= afterNoonInterval.x && timePercent <= afterNoonInterval.y)
            {
                // Afternoon
                float afternoonNormalizedTime = (timePercent - afterNoonInterval.x) / (afterNoonInterval.y - afterNoonInterval.x);
                float afternoonIntensity = Mathf.Lerp(MoonIntensity.x, MoonIntensity.y, afternoonNormalizedTime);
                float afternoonShadowStrength = Mathf.Lerp(0f, MoonShadowStrength, afternoonNormalizedTime);
                MoonDirectionalLight.intensity = afternoonIntensity;
                MoonDirectionalLight.shadowStrength = afternoonShadowStrength;
            }

            if (IsMoonRotationOn)
            {
                Vector3 rotationEuler = Vector3.zero;
                switch (rotationAxis)
                {
                    case RotationAxis.X:
                        rotationEuler = new Vector3((timePercent * 360f) + 90f, MoonDirectionalLight.transform.localRotation.y, MoonDirectionalLight.transform.localRotation.z);
                        break;
                    case RotationAxis.Y:
                        rotationEuler = new Vector3(MoonDirectionalLight.transform.localRotation.x, (timePercent * 360f) + 90f, MoonDirectionalLight.transform.localRotation.z);
                        break;
                }
                MoonDirectionalLight.transform.localRotation = Quaternion.Euler(rotationEuler);
            }
        }

        //Try to find a directional light and skybox material to use if we haven't set one
        private void OnValidate()
        {
            //---------------------------Directional Light ----------------------------
            if (SunDirectionalLight != null)
                return;

            //Search for lighting tab sun
            if (RenderSettings.sun == null)
            {
                SunDirectionalLight = RenderSettings.sun;
            }
            //Search scene for light that fits criteria (directional)
            else
            {
                Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        SunDirectionalLight = light;
                        return;
                    }
                }
            }

            //--------------------------Skybox-------------------------------

            if(skyboxMat != null)
                return;

            if(RenderSettings.skybox != null)
            {
                skyboxMat = RenderSettings.skybox;
            }

            //------Moon
            if (IsMoonActive && MoonDirectionalLight != null)
            {
                UpdateMoonLighting(TimeOfDay / 24f);
            }

        }
    }
}