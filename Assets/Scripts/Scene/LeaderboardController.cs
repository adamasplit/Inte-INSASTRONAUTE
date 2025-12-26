using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class LeaderboardController : MonoBehaviour
{
    public GameObject leaderboardElementPrefab;
    public Transform contentParent;

    void Start()
    {
        Debug.Log("Populating leaderboard with dummy data.");
        DummyPopulate();
    }

    public void AddLeaderboardEntry(int rank, string playerName, int score, Sprite userIcon = null, Color? backgroundColor = null)
    {
        GameObject newEntry = Instantiate(leaderboardElementPrefab, contentParent);
        LeaderboardElement element = newEntry.GetComponent<LeaderboardElement>();
        element.SetData(rank, playerName, score, userIcon, backgroundColor);
    }

    public void DummyPopulate()
    {
        AddLeaderboardEntry(1, "Adamasploots", 1500, null, Color.yellow);
        AddLeaderboardEntry(2, "Offieks", 1200, null, Color.gray);
        AddLeaderboardEntry(3, "Loris", 1000, null, Color.brown);
        AddLeaderboardEntry(4, "Fhystel", 800);
        AddLeaderboardEntry(5, "Maitr", 600);
        AddLeaderboardEntry(6, "Tim", 400);
        AddLeaderboardEntry(7, "Elisa", 300);
        AddLeaderboardEntry(8, "πrkiroul", 200);
        AddLeaderboardEntry(9, "Yamatinou", 100);
        AddLeaderboardEntry(10, "RJ", 50);
        AddLeaderboardEntry(11, "Guest123", 25);
        AddLeaderboardEntry(12, "PlayerX", 10);
        AddLeaderboardEntry(13, "NoobMaster", 5);
        AddLeaderboardEntry(14, "GamerGal", 2);
        AddLeaderboardEntry(15, "Speedy", 1);
        AddLeaderboardEntry(16, "Shadow", 0);

    }

}