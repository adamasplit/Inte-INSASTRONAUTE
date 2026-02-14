using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ParticleClick : MonoBehaviour
{
    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem particlePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 10f; // Distance from camera
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int poolSize = 10;
    
    [Header("Canvas/UI Mode")]
    [SerializeField] private bool isUIMode = false;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform particleContainer;
    
    [Header("Options")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private LayerMask ignoreLayers;
    
    private Queue<ParticleSystem> particlePool;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (!particlePrefab)
        {
            Debug.LogWarning("[ParticleClick] No particle prefab assigned!");
            return;
        }

        if (useObjectPooling)
        {
            InitializePool();
        }
    }

    private void Update()
    {
        // Handle touch input (mobile) - New Input System
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.press.wasPressedThisFrame)
                {
                    SpawnParticleAtPosition(touch.position.ReadValue());
                }
            }
        }
        
        // Handle mouse input (editor/PC testing) - New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnParticleAtPosition(Mouse.current.position.ReadValue());
        }
    }

    private void SpawnParticleAtPosition(Vector3 screenPosition)
    {
        if (!particlePrefab || !mainCamera) return;

        Vector3 worldPosition;

        if (isUIMode && targetCanvas)
        {
            // UI/Canvas mode
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                particleContainer ? particleContainer : targetCanvas.GetComponent<RectTransform>(),
                screenPosition,
                targetCanvas.worldCamera ? targetCanvas.worldCamera : mainCamera,
                out worldPosition
            );
        }
        else
        {
            // World space mode
            worldPosition = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, spawnDistance)
            );
        }

        // Get or create particle system
        ParticleSystem ps = useObjectPooling ? GetPooledParticle() : Instantiate(particlePrefab);
        
        if (ps)
        {
            ps.transform.position = worldPosition;
            ps.transform.rotation = Quaternion.identity;
            ps.gameObject.SetActive(true);
            ps.Play();

            // Haptic feedback
            TriggerHaptic();

            // Auto-return to pool or destroy
            if (useObjectPooling)
            {
                StartCoroutine(ReturnToPool(ps, ps.main.duration + ps.main.startLifetime.constantMax));
            }
            else
            {
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }
    }

    private void InitializePool()
    {
        particlePool = new Queue<ParticleSystem>();
        
        Transform poolContainer = new GameObject("ParticlePool").transform;
        poolContainer.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            ParticleSystem ps = Instantiate(particlePrefab, poolContainer);
            ps.gameObject.SetActive(false);
            particlePool.Enqueue(ps);
        }
    }

    private ParticleSystem GetPooledParticle()
    {
        if (particlePool == null || particlePool.Count == 0)
        {
            // Pool exhausted, create new instance
            ParticleSystem ps = Instantiate(particlePrefab);
            return ps;
        }

        ParticleSystem pooledPS = particlePool.Dequeue();
        return pooledPS;
    }

    private System.Collections.IEnumerator ReturnToPool(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (ps && ps.gameObject)
        {
            ps.Stop();
            ps.Clear();
            ps.gameObject.SetActive(false);
            particlePool.Enqueue(ps);
        }
    }

    private void TriggerHaptic()
    {
        if (!enableHaptics) return;
        
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    // Public method to spawn particles programmatically
    public void SpawnAtWorldPosition(Vector3 worldPos)
    {
        if (!particlePrefab) return;

        ParticleSystem ps = useObjectPooling ? GetPooledParticle() : Instantiate(particlePrefab);
        
        if (ps)
        {
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();

            if (useObjectPooling)
            {
                StartCoroutine(ReturnToPool(ps, ps.main.duration + ps.main.startLifetime.constantMax));
            }
            else
            {
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }
    }
}
