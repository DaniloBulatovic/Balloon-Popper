using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WiimoteApi;

public class WiiGameManager : Singleton<WiiGameManager>
{
    private List<string> allColors = new();

    private string currentColorTag = string.Empty;
    private int currentNumberOfBallons = 0;

    [SerializeField] private float backgroundChangeSpeed = 3.0f;
    [SerializeField] private float balloonSpawnSpeed = 0.3f;
    [SerializeField] private int maxBallons = 20;

    [SerializeField] private float timerStart = 60.0f;
    [SerializeField] private float bonusChance = 0.30f;
    [SerializeField] private float bonusTime = 5.0f;

    private float timer;

    private Vector3 mousePosition;
    private bool mouseClicked = false;
    private bool wiimoteClicked = false;
    private int score = 0;
    private int hearts = 3;
    private bool isGameOver = false;
    public float spawnDepth = -5f;
    private int bonusMultiplier = 1;

    private Camera m_camera;

    [SerializeField] private GameObject backgroundWall;
    [SerializeField] private GameObject ballonPrefab;
    [SerializeField] private GameObject ballonPoppedPrefab;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image heart1;
    [SerializeField] private Image heart2;
    [SerializeField] private Image heart3;
    [SerializeField] private GameObject nextColorIndicator;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip[] balloonPops;
    [SerializeField] private AudioClip success;
    [SerializeField] private AudioClip damage;
    [SerializeField] private AudioClip[] victory;

    protected override void Awake()
    {
        base.Awake();

        m_camera = Camera.main;

        allColors = ColorData.Instance.activeColors;

        float wallHalfHeight = m_camera.orthographicSize;
        float wallHalfWidth = m_camera.orthographicSize * m_camera.aspect;
        backgroundWall.transform.localScale = new Vector3(2 * wallHalfWidth, 2 * wallHalfHeight, 1);

        timer = timerStart;

        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = canvas.GetComponent<EventSystem>();
    }

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

    private IEnumerator ChangeBackgroundColor()
    {
        int lastIndex = Random.Range(0, allColors.Count);
        while (!isGameOver)
        {
            int index = Random.Range(0, allColors.Count);
            if (index == lastIndex)
            {
                index++;
                index %= allColors.Count;
            }
            backgroundWall.GetComponent<Renderer>().material.color = GetColorByName(allColors[lastIndex]);
            currentColorTag = allColors[lastIndex];
            lastIndex = index;

            nextColorIndicator.transform.localScale = Vector3.one;
            nextColorIndicator.GetComponent<Image>().color = GetColorByName(allColors[index]);
            StartCoroutine(ShrinkNextColorIndicator(Vector3.one, new Vector3(0.0f, 1.0f, 1.0f), backgroundChangeSpeed));
            yield return new WaitForSeconds(backgroundChangeSpeed);
        }
    }

    private IEnumerator ShrinkNextColorIndicator(Vector3 startScale, Vector3 endScale, float time)
    {
        float t = 0.0f;
        float rate = 1.0f / time;

        while (t < 1.0f)
        {
            t += Time.deltaTime * rate;
            nextColorIndicator.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
    }

    private IEnumerator SpawnBallons()
    {
        while (!isGameOver)
        {
            if (currentNumberOfBallons > maxBallons)
            {
                yield return new WaitForSeconds(balloonSpawnSpeed);
            }

            int index = Random.Range(0, allColors.Count);

            float width = m_camera.orthographicSize * m_camera.aspect - 1.0f;
            Vector3 spawnPosition = new Vector3(Random.Range(-width, width), spawnDepth, 0);
            GameObject newBallon = Instantiate(ballonPrefab, spawnPosition, ballonPrefab.transform.rotation);
            newBallon.tag = allColors[index];
            newBallon.GetComponentInChildren<Renderer>(true).material.color = GetColorByName(allColors[index]);

            if (bonusChance > Random.value)
            {
                newBallon.GetComponent<BallonController>().AddRandomBonus();
            }

            currentNumberOfBallons++;
            yield return new WaitForSeconds(balloonSpawnSpeed);
        }
    }

    private IEnumerator TimerCountdown()
    {
        while (!isGameOver)
        {
            if (timer > 0.0f)
            {
                timer--;
                timerText.text = timer.ToString();
            }else {
                GameOver();
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    void Start()
    {
        StartCoroutine(SpawnBallons());
        StartCoroutine(ChangeBackgroundColor());
        StartCoroutine(TimerCountdown());

        if (InputManager.wiimote != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        UpdateHearts();
    }

    private void FixedUpdate()
    {
        Vector3 position = mousePosition;
        
        if (InputManager.wiimote != null && wiimoteClicked)
        {
            wiimoteClicked = false;
            GameObject obj = InputManager.inputs.GetAimedAtObject();
            ClickedObject(obj);
        }

        Ray ray = m_camera.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit) && mouseClicked)
        {
            mouseClicked = false;
            ClickedObject(hit.transform.gameObject);
        }
    }

    private void ClickedObject(GameObject obj)
    {
        if (obj == null) return;

        if (obj.CompareTag("Background") || obj.CompareTag("TopCollider")) return;

        if (obj.CompareTag(currentColorTag))
        {
            ClickedCorrectBalloon(obj);
        }
        else
        {
            ClickedWrongBalloon(obj);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isGameOver)
        {
            mousePosition = Input.mousePosition;
            mouseClicked = true;
        }
        
        if (InputManager.inputs == null) return;

        if (InputManager.wiimote != null && InputManager.inputs.GetWiimoteButtonDown(Button.B))
        {
            wiimoteClicked = true;
            pointerEventData = new PointerEventData(eventSystem)
            {
                position = InputManager.inputs.GetPointerPositionViewport()
            };

            List<RaycastResult> results = new();

            graphicRaycaster.Raycast(pointerEventData, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.TryGetComponent(out UnityEngine.UI.Button button))
                {
                    button.onClick.Invoke();
                }
            }
        }
    }

    private void ClickedCorrectBalloon(GameObject go)
    {
        AudioManager.Instance.PlayRandomSound(balloonPops);
        AudioManager.Instance.PlaySound(success);
        PopBalloon(go);
        ReduceNumberOfBalloons();
        score += 10 * bonusMultiplier;
        scoreText.text = score.ToString();
        bonusMultiplier = 1;
    }

    private void ClickedWrongBalloon(GameObject go)
    {
        AudioManager.Instance.PlayRandomSound(balloonPops);
        AudioManager.Instance.PlaySound(damage);
        PopBalloon(go);
        ReduceNumberOfBalloons();
        hearts -= 1;
        UpdateHearts();
        bonusMultiplier = 1;
    }

    private void PopBalloon(GameObject go)
    {
        GameObject popped = Instantiate(ballonPoppedPrefab, go.transform.position, go.transform.rotation);

        Color balloonColor = GetColorByName(go.tag);

        foreach (Renderer renderer in popped.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = balloonColor;
        }
        Destroy(go);
        ActivateBonus(go);
        popped.GetComponent<Rigidbody>().AddExplosionForce(2.0f, popped.transform.position, 2.0f);
        Destroy(popped, 0.3f);
    }

    private void ActivateBonus(GameObject go)
    {
        string bonus = go.GetComponent<BallonController>().GetBonus();

        switch (bonus) {
            case "2x":
                bonusMultiplier = 2;
                break;
            case "4x":
                bonusMultiplier = 4;
                break;
            case "timer":
                timer += bonusTime;
                break;
            default:
                break;
        }
    }

    private void UpdateHearts()
    {
        switch (hearts)
        {
            case 0:
                heart1.gameObject.SetActive(false);
                heart2.gameObject.SetActive(false);
                heart3.gameObject.SetActive(false);
                InputManager.wiimote?.SendPlayerLED(false, false, false, false);
                GameOver();
                break;
            case 1:
                heart1.gameObject.SetActive(true);
                heart2.gameObject.SetActive(false);
                heart3.gameObject.SetActive(false);
                InputManager.wiimote?.SendPlayerLED(true, false, false, false);
                break;
            case 2:
                heart1.gameObject.SetActive(true);
                heart2.gameObject.SetActive(true);
                heart3.gameObject.SetActive(false);
                InputManager.wiimote?.SendPlayerLED(true, true, false, false);
                break;
            default:
                heart1.gameObject.SetActive(true);
                heart2.gameObject.SetActive(true);
                heart3.gameObject.SetActive(true);
                InputManager.wiimote?.SendPlayerLED(true, true, true, false);
                break;
        }
    }

    public void ReduceNumberOfBalloons()
    {
        currentNumberOfBallons--;
    }

    private void GameOver()
    {
        AudioManager.Instance.PlayRandomSound(victory);
        isGameOver = true;
        gameOverPanel.SetActive(true);
        gameOverScoreText.text = score.ToString();

        if (InputManager.wiimote != null)
            InputManager.inputs.RumbleWiimoteForSeconds(0.5f);
        StartCoroutine(PlayGameOverLEDEffect(3, 1.0f));
    }

    private IEnumerator PlayGameOverLEDEffect(int numberOfRepeats, float interval)
    {
        if (numberOfRepeats <= 0)
            yield return null;

        for (int i = 0; i < numberOfRepeats; i++)
        {
            InputManager.wiimote?.SendPlayerLED(true, true, true, true);
            yield return new WaitForSeconds(interval);
            InputManager.wiimote?.SendPlayerLED(false, false, false, false);
        }
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
