using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;

public class MenuSystem : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject optionsMenuObject;

    [Header("UI Elements (Main)")]
    public TextMeshProUGUI seedField;

    [Header("UI Elements (Options)")]
    public Slider viewDistance;
    public Slider mouseSensitivity;
    public TextMeshProUGUI viewDstText;
    public TextMeshProUGUI mouseSenseText;
    public Toggle threadingToggle;
    public Toggle animatedChunkToggle;
    public TextMeshProUGUI version;
    public TMP_Dropdown clouds;

    UserSettings settings;

    private void Awake()
    {
        if(!File.Exists(Application.dataPath + "/game.cfg"))
        {
            Debug.Log("No Settings file found. Creating new game.cfg");
            settings = new UserSettings();
            string settingsExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/game.cfg", settingsExport);
        } else
        {
            Debug.Log("Settings file found. Loading settings.");
            string settingsImport = File.ReadAllText(Application.dataPath + "/game.cfg");
            settings = JsonUtility.FromJson<UserSettings>(settingsImport);
        }
        version.text = "Build version: \n" + settings.version; 
    } 

    public void StartGame()
    {
        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.worldSizeInChunks;
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void ChangeOptions()
    {
        viewDistance.value = settings.viewDistanceInChunks;
        UpdateViewSlider();

        mouseSensitivity.value = settings.mouseSensitivity;
        UpdateMouseSlider();

        threadingToggle.isOn = settings.enableMultiThreading;
        animatedChunkToggle.isOn = settings.enableAnimatedChunkLoading;

        clouds.value = (int)settings.clouds;

        mainMenuObject.SetActive(false);
        optionsMenuObject.SetActive(true);
    }

    public void SaveSettings()
    {
        settings.viewDistanceInChunks = (int)viewDistance.value;
        settings.mouseSensitivity = mouseSensitivity.value;
        settings.enableAnimatedChunkLoading = animatedChunkToggle.isOn;
        settings.enableMultiThreading = threadingToggle.isOn;
        settings.clouds = (CloudStyle)clouds.value;

        string settingsExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/game.cfg", settingsExport);

        optionsMenuObject.SetActive(false);
        mainMenuObject.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateViewSlider()
    {
        viewDstText.text = "View Distance: " + viewDistance.value;
    }

    public void UpdateMouseSlider()
    {
        mouseSenseText.text = "Mouse Sensitivity: " + mouseSensitivity.value.ToString("F1");
    }
}
