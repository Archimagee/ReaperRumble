using UnityEngine;



public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    public void AddScore(int playerNumber, int newScore)
    {
        Debug.Log("Player " + playerNumber + " scored " + newScore + " souls!");
    }
}
