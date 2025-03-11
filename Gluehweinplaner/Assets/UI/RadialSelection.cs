using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEngine.InputSystem;
using TMPro;


public class RadicalSelection : MonoBehaviour
{
    [Range(2, 10)]
    public int numberOfRadialPart;
    [Range(2, 10)]
    public int numberOfHouseParts;
    [Range(2, 10)]
    public int numberOfLeftParts;
    public GameObject radialPartPrefab;
    public Transform radialPartCanvas;
    public float angleBetweenPart = 10;
    public Transform handTransform;
    public Transform handTransformLeft;
    public ObjectSpawner objectSpawner;
    public List<UnityEvent<int>> partToFunction;
    public List<UnityEvent<int>> partToFunctionHouse;
    public List<UnityEvent<int>> partToFunctionLeft;
    public Gradient gradient = new Gradient();

    private List<GameObject> spawnedParts = new List<GameObject>();
    private int currentSelectedRadialPart = -1;

    public List<string> buttonLabels;  // Text for each button
    public List<string> buttonLabelsHouse;  // Text for each button
    public List<string> buttonLabelsLeft;  // Text for each button
    public float textRotationOffset = 90f;  // Keep text upright

    public InputActionReference menuActivateAction;
    public InputActionReference menuActivateActionLeft;

    public float waitTimeUntilActivation = 1.0f;
    public float timeWaited = 0;
    public int waitingAt=-1;

    [SerializeField] private AudioClip spawnRadialPartSoundClip;
    [SerializeField] private AudioClip[] selectionChangeSoundClip;
    private int previousSelected = -1; // Track previous selection
    [SerializeField] private AudioClip selectionConfirmSoundClip;

    public GameObject volumeCanvas;
    public UnityEvent onVolumeMenuOpen;
    public UnityEvent onVolumeMenuClose;

    private bool inSubMenu = false;
    public bool isBude = false;
    private Buden selectedBude = null;
    private Material before;
    [SerializeField] private Material highlightMaterial;
    private AgentManager am;
    private bool left;


    void Start()
    {
    
        am = GameObject.Find("AgentManager").GetComponent<AgentManager>();

        //Debug.Log($"RadialPartCanvas Active: {radialPartCanvas.gameObject.activeSelf}");
        //Debug.Log($"Hand Position: {handTransform.position}, Rotation: {handTransform.rotation}");
        //Debug.Log($"Current Selected Part: {currentSelectedRadialPart}");


        var colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(Color.white, 0.0f);
        colors[1] = new GradientColorKey(Color.yellow, 1.0f);

        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

        gradient.SetKeys(colors, alphas);
        radialPartCanvas.gameObject.SetActive(false);

    }

    void Update()
    {
        //Debug.Log($"RadialPartCanvas Active: {radialPartCanvas.gameObject.activeSelf}");
        //Debug.Log($"Hand Position: {handTransform.position}, Rotation: {handTransform.rotation}");
        //Debug.Log($"Current Selected Part: {currentSelectedRadialPart}");
        
        // Check if the menu activation button is pressed
        if (menuActivateAction.action.triggered && !objectSpawner.IsPlacing)
        {
            left = false;
            radialPartCanvas.gameObject.SetActive(true); // Show the radial menu
            SpawnRadialPart(); // Populate the radial menu
        }else if (menuActivateActionLeft.action.triggered && !objectSpawner.IsPlacing)
        {   
            Debug.Log("Left Hand Menu Activation");
            left = true;
            radialPartCanvas.gameObject.SetActive(true); // Show the radial menu
            SpawnRadialPart(); // Populate the radial menu
        }

        // Keep the menu active and update selection
        if (radialPartCanvas.gameObject.activeSelf)
        {
            Debug.Log("Radial Part Canvas is active");
            GetSelectedRadialPart(Time.deltaTime);

            // Hide and trigger the selected part when the button is released
            if (menuActivateAction.action.WasReleasedThisFrame() || menuActivateActionLeft.action.WasReleasedThisFrame())
            {
                Debug.Log("Menu Activation Button Released");
                HideAndTriggerSelected();
            }
        }
    }


    private void ResetWaitTimer(int newWait)
    {
        timeWaited = 0;
        waitingAt = newWait;
    }

    private void HideAndTriggerSelected()
    {
        Debug.Log("Hiding and triggering selected part");
        int count = (isBude) ? partToFunctionHouse.Count : ((left) ? partToFunctionLeft.Count : partToFunction.Count);
        if (timeWaited > waitTimeUntilActivation && currentSelectedRadialPart < count)
        {
            // Play confirmation sound before invoking action
            if(selectionConfirmSoundClip != null)
            {
                SoundFXManager.instance.PlaySoundFXClip(selectionConfirmSoundClip, transform, 1f);
            }

            if (isBude)
            {
                if(selectedBude){ selectedBude.gameObject.GetComponentInChildren<Renderer>().material = before; }
                partToFunctionHouse[currentSelectedRadialPart].Invoke(currentSelectedRadialPart);
            }
            else
            {
                if (left)
                {
                    partToFunctionLeft[currentSelectedRadialPart].Invoke(currentSelectedRadialPart);
                }
                else
                {
                    partToFunction[currentSelectedRadialPart].Invoke(currentSelectedRadialPart);
                }
            }
        }
            radialPartCanvas.gameObject.SetActive(false);
    }

    public void GetSelectedRadialPart(float time)
    {
        int number = (isBude) ? numberOfHouseParts : ((left) ? numberOfLeftParts : numberOfRadialPart);
        Vector3 centerToHand = ((left)? handTransformLeft.position:handTransform.position) - radialPartCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);

        float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, -radialPartCanvas.forward);

        if (angle < 0)
        {
            angle += 360;
        }

        //Debug.Log("ANGLE: " + angle);

        currentSelectedRadialPart = (int)angle * number / 360;

        // Play sound when selection changes
        if (previousSelected != currentSelectedRadialPart)
        {
            SoundFXManager.instance.PlayRandomSoundFXClip(selectionChangeSoundClip, transform, 0.8f);
            previousSelected = currentSelectedRadialPart;
        }

        if(currentSelectedRadialPart != waitingAt)
        {
            ResetWaitTimer(currentSelectedRadialPart);
        }
        timeWaited += time;

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedRadialPart)
            {
                spawnedParts[i].GetComponent<Image>().color = gradient.Evaluate(timeWaited/waitTimeUntilActivation);
                spawnedParts[i].transform.localScale = 1.1f * UnityEngine.Vector3.one;
            }
            else
            {
                spawnedParts[i].GetComponent<Image>().color = Color.white;
                spawnedParts[i].transform.localScale = 1 * UnityEngine.Vector3.one;
            }
        }
    }

     public void ToggleVolumeMenu()
    {
        inSubMenu = !inSubMenu;
        volumeCanvas.SetActive(inSubMenu);
        radialPartCanvas.gameObject.SetActive(!inSubMenu);
        
        if(inSubMenu) 
        {
            onVolumeMenuOpen.Invoke();
            
        Transform referenceTransform = Camera.main.transform;
        float distance = 1.75f; 

        // Positionierung des Volume-Menüs
        Vector3 forwardDirection = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
        volumeCanvas.transform.position = referenceTransform.position + forwardDirection * distance;

        Quaternion lookRotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
        volumeCanvas.transform.rotation = lookRotation;
        }
        else
        {
            onVolumeMenuClose.Invoke();
            radialPartCanvas.gameObject.SetActive(false);
        }
    }

    public void SpawnRadialPart()
    {
        Ray ray = new Ray(handTransform.position, handTransform.forward);
        float maxDistance = 20f;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            isBude = hit.collider.gameObject.CompareTag("Bude");
        }
        if (isBude)
        {
            selectedBude = hit.collider.transform.parent?.gameObject.GetComponent<Buden>();
            if (selectedBude)
            {
                Renderer r = hit.collider.GetComponent<Renderer>();
                before = r.material;
                r.material = highlightMaterial;
            }
        }
        else
        {
            selectedBude = null;
        }

        radialPartCanvas.gameObject.SetActive(true);

        //play Placement Sound Effect
        SoundFXManager.instance.PlaySoundFXClip(spawnRadialPartSoundClip, transform, 1f);

        radialPartCanvas.position = (left) ? handTransformLeft.position : handTransform.position;
        radialPartCanvas.position = (left) ? handTransformLeft.position : handTransform.position;
        radialPartCanvas.rotation = (left) ? handTransformLeft.rotation : handTransform.rotation;

        foreach (var item in spawnedParts)
        {
            Destroy(item);
        }

        spawnedParts.Clear();

        int number = (isBude) ? numberOfHouseParts : numberOfRadialPart;
        number = (left) ? numberOfLeftParts : number;

        for (int i = 0; i < number; i++)
        {

            float angle = -i * 360 / number - angleBetweenPart / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnedRadialPart.GetComponent<Image>().fillAmount = (1 / (float)number) - (angleBetweenPart / 360);
            spawnedParts.Add(spawnedRadialPart);

            // Add icon setup
            //var iconImage = spawnedRadialPart.transform.GetChild(0).GetComponent<Image>();
            //iconImage.sprite = buttonIcons[i];
            //iconImage.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            TextMeshProUGUI buttonText = spawnedRadialPart.GetComponentInChildren<TextMeshProUGUI>();
            int count = (isBude) ? partToFunctionHouse.Count : (left)?partToFunctionLeft.Count :partToFunction.Count;
            if (buttonText != null && i < count)
            {
                buttonText.text = (isBude) ? buttonLabelsHouse[i] : (left) ? buttonLabelsLeft[i] :buttonLabels[i];
                // Counteract radial rotation
            }

            //// Add icon implementation (optional)
            //Image iconImage = spawnedRadialPart.GetComponentInChildren<Image>();
            //if (iconImage != null && i < buttonIcons.Count)
            //{
            //    iconImage.sprite = buttonIcons[i];
            //    iconImage.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            //}
        }
    }

    public void IncreaseAttrakSelecBude()
    {
        if (selectedBude != null)
        {
            selectedBude.increaseAttraktivität();
        }
    }

    public void DecreaseAttrakSelecBude()
    {
        if (selectedBude != null)
        {
            selectedBude.decreaseAttraktivität();
        }
    }

    public void IncreaseWaitTime()
    {
        if (selectedBude != null)
        {
            selectedBude.increaseWaittime();
        }
    }

    public void DecreaseWaitTime()
    {
        if (selectedBude != null)
        {
            selectedBude.decreaseWaittime();
        }
    }

    public void DeleteBuilding()
    {
        if (selectedBude != null)
        {
            selectedBude.ToBeDestroyed();
            am.RemoveBude(selectedBude);
            Destroy(selectedBude.gameObject);
        }
    }


}

