using UnityEngine;

public class WaterPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float waterAmount = 20f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;
    //public ParticleSystem pickupEffect;

    private Vector3 startPosition;
    private bool collected = false;

    void Start()
    {
        startPosition = transform.position;
        gameObject.tag = "WaterPickup";
    }

    void Update()
    {
        if (!collected)
        {
            // شناور کردن آبی
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // چرخش
            transform.Rotate(0, 50f * Time.deltaTime, 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !collected)
        {
            collected = true;

            // اضافه کردن آب به بازیکن
            GameManager.Instance?.AddWater(waterAmount);

            // // افکت
            // if (pickupEffect != null)
            // {
            //     Instantiate(pickupEffect, transform.position, Quaternion.identity);
            // }

            // صدا (اختیاری)
            //AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("WaterPickup"), transform.position);

            Destroy(gameObject);
        }
    }
}