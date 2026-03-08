using UnityEngine; 
using UnityEngine.UI;
public class RandomNoteSetter : MonoBehaviour
{
    void Start()
    {
        GetComponent<Image>().sprite = Resources.Load<Sprite>("Projectiles/notes" + Random.Range(1, 6));
    }
}