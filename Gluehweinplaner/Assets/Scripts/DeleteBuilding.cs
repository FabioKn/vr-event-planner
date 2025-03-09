using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingDeletion : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial; // Material für Markierung
    [SerializeField] private float deleteTimeout = 3f; // Zeit, nach der die Markierung entfernt wird
    [SerializeField] private InputActionReference deleteBuildingAction; // Input Action für Trigger-Button
    [SerializeField] private Transform handTransform; // Transform des VR-Controllers

    AgentManager am;

    private GameObject selectedBuilding;
    private Material originalMaterial;
    private bool isMarkedForDeletion = false;
    private Coroutine deletionCoroutine;

    public void Start()
    {
        am = GameObject.Find("AgentManager").GetComponent<AgentManager>();
    }


    private void OnEnable()
    {
        deleteBuildingAction.action.performed += OnDeleteBuildingPressed;
        deleteBuildingAction.action.Enable();
    }

    private void OnDisable()
    {
        deleteBuildingAction.action.performed -= OnDeleteBuildingPressed;
        deleteBuildingAction.action.Disable();
    }

    private void OnDeleteBuildingPressed(InputAction.CallbackContext context)
    {
        if (isMarkedForDeletion && selectedBuilding != null)
        {
            DeleteBuilding(); // Bestätigung des Löschens
        }
        else
        {
            MarkBuildingForDeletion(); // Erste Auswahl
        }
    }

    void MarkBuildingForDeletion()
    {
        Ray ray = new Ray(handTransform.position, handTransform.forward); // Raycast vom VR-Controller
        float maxDistance = 10f;
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.gameObject.CompareTag("Bude")) // Prüft, ob das getroffene Objekt "dieBuuude" ist
            {
                GameObject parentBuilding = hit.collider.transform.parent?.gameObject; // Holt das "Cool"-Objekt

                if (parentBuilding == null)
                {
                    Debug.Log("Kein Parent-Objekt gefunden!"); 
                    return;
                }

                Debug.Log("Bude markiert: " + parentBuilding.name);
                selectedBuilding = parentBuilding; // Speichert das gesamte "Cool"-Objekt zur Löschung

                // Originalmaterial speichern & Markierung setzen
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalMaterial = renderer.material;
                    renderer.material = highlightMaterial; // Material für Markierung setzen
                }

                isMarkedForDeletion = true;
                deletionCoroutine = StartCoroutine(ResetDeletionAfterTimeout());
            }
            else
            {
                Debug.Log("Objekt getroffen, aber kein 'Bude'-Tag: " + hit.collider.gameObject.name);
            }
        }
        else
        {
            Debug.Log("Kein Objekt getroffen.");
        }
    }

    void DeleteBuilding()
    {
        if (selectedBuilding != null)
        {
            Debug.Log("Gebäude gelöscht: " + selectedBuilding.name);
            Buden b = selectedBuilding.GetComponent<Buden>();
            b.ToBeDestroyed();
            am.RemoveBude(b);
            Destroy(selectedBuilding);
        }

        if (deletionCoroutine != null)
        {
            StopCoroutine(deletionCoroutine);
        }

        isMarkedForDeletion = false;
        selectedBuilding = null;
    }

    IEnumerator ResetDeletionAfterTimeout()
    {
        yield return new WaitForSeconds(deleteTimeout);

        if (selectedBuilding != null)
        {
            Renderer renderer = selectedBuilding.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = originalMaterial; // Originalmaterial zurücksetzen
            }
        }

        isMarkedForDeletion = false;
        selectedBuilding = null;
    }
}
