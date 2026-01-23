using System.Threading.Tasks;
using UnityEngine;
public class WaveManager : MonoBehaviour
{
    public float descentInterval = 2f;
    public float descentStep = 0.5f;
    private float loseY = -1.2f;

    void Start()
    {
        InvokeRepeating(nameof(DescendEnemies), descentInterval, descentInterval);
    }

    async Task DescendEnemies()
    {
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.transform.position += Vector3.down * descentStep;

            if (enemy.transform.position.y < loseY)
                await GameManager.Instance.GameOver();
        }
    }
}
