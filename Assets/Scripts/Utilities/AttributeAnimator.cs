using UnityEngine;

public class AttributeAnimator : MonoBehaviour
{
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private bool updateSpeed = false;

    void Awake()
    {
        if (gameObject.name == "3") SetSpeed(0.85f);
        if (gameObject.name == "2") SetSpeed(0.85f);
    }

    void Update()
    {
        if (updateSpeed) SetSpeed(speed);
    }

    public void RandomizeAndStart()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
            return;
        }

        float randomOffset = Random.Range(0f, 1f);
        animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, -1, randomOffset);
        animator.Update(0f);
        animator.speed = speed;
    }

    public void ResetToIdleAndStop()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
            return;
        }

        animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, -1, 0f);
        animator.Update(0f);
        animator.speed = 0f;
    } 

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    public void HidePedMesh()
    {
        // get child by name that includes "geo"
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("geo"))
            {
                child.gameObject.SetActive(false);
                return;
            }
        }
    }

    public void ShowPedMesh()
    {
        // get child by name that includes "geo"
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("geo"))
            {
                child.gameObject.SetActive(true);
                return;
            }
        }
    }
}
