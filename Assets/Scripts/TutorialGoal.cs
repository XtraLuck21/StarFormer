using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TutorialGoal : MonoBehaviour
{
    [SerializeField] private PossessableBody tutorialBox;
    [SerializeField] private GameObject tutorialDoor;
    [SerializeField] private TutorialIntroUI tutorialUI;

    private bool isCompleted;
    private float previousTimeScale;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (tutorialBox == null ||
            tutorialDoor == null ||
            tutorialUI == null)
        {
            Debug.LogError(
                "Assign the tutorial box, door, and UI.",
                this
            );

            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckGoal(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckGoal(other);
    }

    private void CheckGoal(Collider2D other)
    {
        if (!isActiveAndEnabled ||
            !tutorialUI.CanComplete ||
            isCompleted ||
            !tutorialUI.isActiveAndEnabled ||
            tutorialDoor.activeSelf ||
            !tutorialBox.IsControlled)
        {
            return;
        }

        if (other.attachedRigidbody != tutorialBox.Body)
        {
            return;
        }

        isCompleted = true;

        // Match the visible body to its actual physics pose before pausing.
        Rigidbody2D body = tutorialBox.Body;
        Vector2 finalPosition = body.position;
        float finalRotation = body.rotation;

        body.interpolation = RigidbodyInterpolation2D.None;

        body.transform.SetPositionAndRotation(
            new Vector3(
                finalPosition.x,
                finalPosition.y,
                body.transform.position.z
            ),
            Quaternion.Euler(0f, 0f, finalRotation)
        );

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;

        tutorialUI.ShowCompletion();

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Debug.Log("Tutorial completed.", this);
            }

            private void OnDisable()
            {
                if (isCompleted)
                {
                    Time.timeScale = previousTimeScale;
                }
            }
}