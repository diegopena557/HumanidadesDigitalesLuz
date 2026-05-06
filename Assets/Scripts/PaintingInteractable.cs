using UnityEngine;
using UnityEngine.SceneManagement;

public class PaintingInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Nombre EXACTO de la escena en Build Settings")]
    [SerializeField] private string targetSceneName = "";

    public string InteractPrompt => "Presiona E para entrar";

    public void OnInteract(RaycastHit hit)
    {
        SceneManager.LoadScene(targetSceneName);
    }
}