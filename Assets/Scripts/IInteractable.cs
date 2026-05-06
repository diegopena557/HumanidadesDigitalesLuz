using UnityEngine;

public interface IInteractable
{
    /// Se llama cuando el jugador presiona E mirando este objeto.
    void OnInteract(RaycastHit hit);

    /// Texto que se muestra en el UI de crosshair al apuntar al objeto.
    string InteractPrompt { get; }
}