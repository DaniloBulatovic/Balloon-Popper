using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    private GraphicRaycaster graphicRaycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    private void Start()
    {
        graphicRaycaster = GetComponent<GraphicRaycaster>();
        eventSystem = GetComponent<EventSystem>();
    }

    private void Update()
    {
        if (InputManager.inputs == null) return;

        if (InputManager.wiimote != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (InputManager.inputs.GetWiimoteButton(Button.B)) // InputManager.inputs.GetWiimoteButton(Button.B)
        {
            pointerEventData = new PointerEventData(eventSystem)
            {
                position = InputManager.inputs.GetPointerPositionViewport() // InputManager.inputs.GetPointerPosition()
            };

            List<RaycastResult> results = new();

            graphicRaycaster.Raycast(pointerEventData, results);

            foreach (RaycastResult result in results)
            {
                if (InputManager.inputs.GetWiimoteButtonDown(Button.B)) // InputManager.inputs.GetWiimoteButtonDown(Button.B)
                {
                    if (result.gameObject.TryGetComponent(out UnityEngine.UI.Button button))
                    {
                        button.onClick.Invoke();
                    }
                    else if (result.gameObject.TryGetComponent(out Toggle toggle))
                    {
                        toggle.isOn = !toggle.isOn;
                    }
                }
                if (result.gameObject.TryGetComponent(out Slider slider))
                {
                    slider.OnDrag(pointerEventData);
                }
            }
        }
    }

    private void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void PlayClicked()
    {
        LoadScene(1);
    }

    public void OptionsClicked()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    public void OptionsBackClicked()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }
}
