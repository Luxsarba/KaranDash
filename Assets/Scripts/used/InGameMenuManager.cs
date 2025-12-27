using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject storyScreen;


    public void Continue()
    {
        storyScreen.SetActive(false);
        _player.GetComponent<Player>().Continue();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        SceneManager.LoadScene("MenuScene");
        Time.timeScale = 1;
    }
}
