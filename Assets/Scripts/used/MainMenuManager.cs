using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject menuCamera;
    private Animator cameraAnimator;
    public GameObject mainMenu;
    public GameObject settings;
    public GameObject level_selector;

    private void Start()
    {
        cameraAnimator = menuCamera.GetComponent<Animator>();
    }

    public void StartGame_1()
    {
        SceneManager.LoadScene("kvk");
    }
    public void StartGame_2()
    {
        SceneManager.LoadScene("office");
    }
    public void StartGame_3()
    {
        SceneManager.LoadScene("tutor");
    }
    public void StartGame_4()
    {
        SceneManager.LoadScene("1loka");
    }

    public void Settings()
    {
        cameraAnimator.SetBool("IsSettings", true);
        mainMenu.SetActive(false);
        settings.SetActive(true);
    }
    public void Return_from_settings()
    {
        cameraAnimator.SetBool("IsSettings", false);
        mainMenu.SetActive(true);
        settings.SetActive(false);
    }

    public void Level_selector()
    {
        cameraAnimator.SetBool("IsSettings", true);
        mainMenu.SetActive(false);
        level_selector.SetActive(true);
    }
    public void Return_from_level_selector()
    {
        cameraAnimator.SetBool("IsSettings", false);
        mainMenu.SetActive(true);
        level_selector.SetActive(false);
    }


    public void Quit()
    {
        Application.Quit();
    }
}
