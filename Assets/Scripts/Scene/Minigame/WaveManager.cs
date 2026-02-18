using System.Threading.Tasks;
using UnityEngine;
public class WaveManager : MonoBehaviour
{
    public float descentInterval = 2f;
    private float descentInterval2 = 2f;
    private float nextDescentTime = 0f;
    public float descentStep = 0.5f;
    private float loseY = -1.2f;

    void Start()
    {
        nextDescentTime = Time.time + descentInterval;
    }
    public void Init()
    {
        nextDescentTime = Time.time + descentInterval+2f;
    }
    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            
            return;
        }
        if (Time.time >= nextDescentTime)
        {
            _ = DescendEnemies();
            nextDescentTime = Time.time + descentInterval2;
        }
    }

    async Task DescendEnemies()
    {
        
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (enemies.Length == 0) return;
        float meanMoveSpeed = 0f;
        foreach (var enemy in enemies)
        {
            meanMoveSpeed += enemy.speed;
        }
        foreach (var enemy in enemies)
        {
            enemy.transform.position += Vector3.down * descentStep;
            if (enemy.transform.position.y < loseY)
            {
                Debug.Log("[WaveManager] Enemy reached " + enemy.transform.position.y + ". Game Over.");
                await GameManager.Instance.GameOver();
            }
        }
        descentInterval2 = Mathf.Max(0.5f, descentInterval - (meanMoveSpeed / enemies.Length) * 0.1f);
        //Debug.Log("[WaveManager] New descent interval: " + descentInterval2);
    }
}
