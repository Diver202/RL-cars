using UnityEngine;
using UnityEngine.SceneManagement; // Required for reloading the scene

public class CarReset : MonoBehaviour
{
    [Tooltip("The Y position at which the scene will reset.")]
    public float fallThreshold = -10f;

    void Update()
    {
        // Check if the car's position on the Y axis is below the threshold
        if (transform.position.y < fallThreshold)
        {
            resetCurrentScene();
        }
    }

    void resetCurrentScene()
    {
        // Get the name of the currently active scene and load it again
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}