using UnityEngine;

public class TouchEffects : MonoBehaviour
{
    [Header("Touch Feedback")]
    public GameObject touchParticle;
    public float particleLifetime = 1f;

    void Update()
    {
        // Mouse/Touch effects
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            CreateTouchEffect(mousePos);
        }

#if UNITY_ANDROID || UNITY_IOS
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                Vector3 touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                touchPos.z = 0;
                CreateTouchEffect(touchPos);
            }
        }
#endif
    }

    void CreateTouchEffect(Vector3 position)
    {
        if (touchParticle != null)
        {
            GameObject effect = Instantiate(touchParticle, position, Quaternion.identity);
            Destroy(effect, particleLifetime);
        }
    }
}