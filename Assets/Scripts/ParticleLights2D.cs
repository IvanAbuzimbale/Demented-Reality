using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleLights2D : MonoBehaviour
{
    #region LightVariables
        [SerializeField] private Light2D lightPrefab;
        [SerializeField] private int maxLights = 32;
        [SerializeField] private float intensityMultiplier = 1f;
        [SerializeField] private float radiusMultiplier = 1f;
    #endregion

    #region InternalVariables
        private ParticleSystem ps;
        private ParticleSystem.Particle[] particles;
        private Light2D[] lights;
        private float baseLightIntensity;
        private bool isLocalSpace;
    #endregion

    void Awake()
    {
        // Get the ParticleSystem component and initialize arrays for particles and lights
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[maxLights];
        lights = new Light2D[maxLights];
        baseLightIntensity = lightPrefab.intensity;
        isLocalSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;

        // Instantiate Light2D objects from the prefab and store them 
        // in the lights array, initially deactivating them
        for (int i = 0; i < maxLights; i++)
        {
            Light2D l = Instantiate(lightPrefab);
            l.gameObject.SetActive(false);
            l.transform.parent = transform;
            lights[i] = l;
        }
    }


    void LateUpdate()
    {
        int count = ps.GetParticles(particles, maxLights);

        // Loop through the active particles and update the corresponding Light2D objects
        for (int i = 0; i < maxLights; i++)
        {
            if (i < count)
            {
                // Get the current particle and corresponding light, activate the light
                // and set its position based on the particle's position
                ParticleSystem.Particle p = particles[i];
                Light2D l = lights[i];
                l.gameObject.SetActive(true);                
                Vector3 worldPos = isLocalSpace
                    ? transform.TransformPoint(p.position)
                    : p.position;
                l.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

                // Set the light's intensity and radius based on 
                // the particle's size and the defined multipliers
                l.intensity = baseLightIntensity * intensityMultiplier;
                l.pointLightOuterRadius = p.GetCurrentSize(ps) * radiusMultiplier;
            }
            else lights[i].gameObject.SetActive(false);
        }
    }
    void OnDestroy()
    {
        // Clean up instantiated Light2D objects when this component is destroyed
        if (lights == null) return;
        foreach (var l in lights)
            if (l != null) Destroy(l.gameObject);
    }
}