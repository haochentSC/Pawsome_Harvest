using UnityEngine;

public class TutorialManager : MonoBehaviour {
    public static TutorialManager Instance;

    public GameObject tutorialCubePrefab;

    private void Awake() {
        Instance = this;
    }

    public void ShowTutorial(string message) {
        // Spawn cube 1 meter in front of the player
        Vector3 pos = Camera.main.transform.position + Camera.main.transform.forward * 1.0f;

        GameObject cube = Instantiate(tutorialCubePrefab, pos, Quaternion.identity);

        // Optional: write text on the cube if it has a TMP child
        var text = cube.GetComponentInChildren<TMPro.TextMeshPro>();
        if (text != null)
            text.text = message;

        // Auto-destroy after 4 seconds
        Destroy(cube, 4f);
    }
}
