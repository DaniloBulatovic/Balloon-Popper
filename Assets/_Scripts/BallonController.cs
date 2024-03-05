using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallonController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 0.05f;
    private Rigidbody rb;

    private List<string> bonuses = new List<string>() { "2x", "4x", "timer"};
    [SerializeField] private Sprite[] BonusSprites;

    private Sprite activeBonus;
    private string bonus = string.Empty;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.transform.Translate(Vector3.up * movementSpeed);
        rb.AddForce(new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, 0.0f) * 5f, ForceMode.Force);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("TopCollider"))
        {
            WiiGameManager.Instance.ReduceNumberOfBalloons();
            Destroy(gameObject);
        }
    }

    public void AddRandomBonus()
    {
        int index = Random.Range(0, BonusSprites.Count());
        activeBonus = BonusSprites[index];
        gameObject.GetComponentInChildren<SpriteRenderer>().sprite = activeBonus;
        bonus = bonuses[index];
    }

    public string GetBonus()
    {
        return bonus;
    }
}
