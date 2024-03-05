using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : Singleton<SettingsManager>
{
    [Header("UI Elements")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_InputField musicVolumeText;
    [SerializeField] private Slider effectsVolumeSlider;
    [SerializeField] private TMP_InputField effectsVolumeText;


    [SerializeField] private Toggle color1;
    [SerializeField] private Toggle color2;
    [SerializeField] private Toggle color3;
    [SerializeField] private Toggle color4;
    [SerializeField] private Toggle color5;
    [SerializeField] private Toggle color6;

    private Color GetColorByName(string name)
    {
        return name switch
        {
            "Red" => Color.red,
            "Green" => Color.green,
            "Blue" => Color.blue,
            "Yellow" => Color.yellow,
            "Magenta" => Color.magenta,
            _ => Color.cyan,
        };
    }

    public void ChangeMusicVolume(bool isSlider)
    {
        if (isSlider)
        {
            AudioManager.Instance.ChangeMusicVolume(musicVolumeSlider.value);
        }
        else
        {
            AudioManager.Instance.ChangeMusicVolume(Mathf.Clamp(int.Parse(musicVolumeText.text), 0, 100) / 100.0f);
        }

        SetUpVolumeSliders();
    }

    public void ChangeEffectsVolume(bool isSlider)
    {
        if (isSlider)
        {
            AudioManager.Instance.ChangeEffectsVolume(effectsVolumeSlider.value);
        }
        else
        {
            AudioManager.Instance.ChangeEffectsVolume(Mathf.Clamp(int.Parse(effectsVolumeText.text), 0, 100) / 100.0f);
        }

        SetUpVolumeSliders();
    }

    private void SetUpVolumeSliders()
    {
        musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        musicVolumeText.text = Mathf.RoundToInt(musicVolumeSlider.value * 100).ToString();
        effectsVolumeSlider.value = AudioManager.Instance.GetEffectsVolume();
        effectsVolumeText.text = Mathf.RoundToInt(effectsVolumeSlider.value * 100).ToString();
    }

    private void SetUpColorToggles()
    {
        SetUpToggleListenerAndColor(color1, "Red");
        SetUpToggleListenerAndColor(color2, "Green");
        SetUpToggleListenerAndColor(color3, "Blue");
        SetUpToggleListenerAndColor(color4, "Yellow");
        SetUpToggleListenerAndColor(color5, "Magenta");
        SetUpToggleListenerAndColor(color6, "Cyan");
    }

    private void SetUpToggleListenerAndColor(Toggle toggle, string color)
    {
        if (toggle == null) return;

        toggle.GetComponentInChildren<Image>().color = GetColorByName(color);
        toggle.isOn = ColorData.Instance.activeColors.Contains(color);
        toggle.onValueChanged.AddListener(delegate
        {
            ColorToggleChange(toggle, color);
        });
    }

    private void ColorToggleChange(Toggle change, string color)
    {
        if (change.isOn)
        {
            if (!ColorData.Instance.activeColors.Contains(color))
                ColorData.Instance.activeColors.Add(color);
        }else
        {
            if (ColorData.Instance.activeColors.Count < 3)
            {
                change.isOn = true;
            }
            else
            {
                ColorData.Instance.activeColors.Remove(color);
            }
        }
    }

    void Start()
    {
        SceneManager.activeSceneChanged += ChangedActiveScene;
        SetUpColorToggles();
        SetUpVolumeSliders();
    }

    private void ChangedActiveScene(Scene current, Scene next)
    {
        if (next == null) return;

        if (next.name.Equals("Main Menu Scene"))
        {
            SetUpColorToggles();
            SetUpVolumeSliders();
        }
    }
}
