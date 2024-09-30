using System.Collections;
using UnityEngine;

public class Anchor : MonoBehaviour
{
    // Reference to the player's transform, set from ClientLogic
    public Transform playerTransform;
    public ClientLogic client;

    // LayerMask to specify which layers the raycast should interact with
    public LayerMask raycastLayerMask;

    // Variables to control raycasting frequency
    private float timeSinceLastRaycast = 0f;
    public float raycastInterval = 0.2f; // Raycast every 0.2 seconds

    public GameObject linePrefab;
    public GameObject lineInstance;

    public string id;

    private bool DebugMode = true;
    private bool isUICurrentlyHit = false;

    void Start()
    {
        StartCoroutine(SelfDestroy());

        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform is not assigned in Anchor. Please assign it from ClientLogic.");
        }
        if (DebugMode)
        {
            lineInstance = Instantiate(linePrefab, transform.position, Quaternion.identity);
        }

        // Ensure the raycastLayerMask includes the UI layer
        // If not set in the inspector, you can set it here
        if (raycastLayerMask == 0)
        {
            // Include all layers
            raycastLayerMask = Physics.DefaultRaycastLayers;
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            // Update the time since the last raycast
            timeSinceLastRaycast += Time.deltaTime;

            if (timeSinceLastRaycast >= raycastInterval)
            {
                timeSinceLastRaycast = 0f;

                // Calculate the direction from the anchor to the player
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

                // Perform the raycast
                RaycastHit hitInfo;
                if (Physics.Raycast(transform.position, directionToPlayer, out hitInfo, Mathf.Infinity, raycastLayerMask))
                {
                    // The raycast hit something
                    if (hitInfo.transform == playerTransform)
                    {
                        if (DebugMode) Debug.Log("Anchor raycast hit the player!");
                        HandleUIHit(false);
                    }
                    else if (hitInfo.transform.gameObject.layer == LayerMask.NameToLayer("UI"))
                    {
                        if (DebugMode) Debug.Log("Anchor raycast hit the UI!");
                        HandleUIHit(true);
                    }
                    else
                    {
                        if (DebugMode) Debug.Log("Anchor raycast hit: " + hitInfo.transform.name);
                        HandleUIHit(false);
                    }
                }
                else
                {
                    // The raycast did not hit anything
                    if (DebugMode) Debug.Log("Anchor raycast did not hit anything.");
                    HandleUIHit(false);
                }

                if (DebugMode)
                {
                    UpdateLine();
                }
            }
        }
        else
        {
            Debug.LogWarning("PlayerTransform is not assigned in Anchor.");
        }
    }

    private void HandleUIHit(bool isHit)
    {
        if (isHit && !isUICurrentlyHit)
        {
            isUICurrentlyHit = true;
            if (client != null)
            {
                client.MoveUIOutOfWay();
            }
        }
        else if (!isHit && isUICurrentlyHit)
        {
            isUICurrentlyHit = false;
            if (client != null)
            {
                client.ReturnUIToOriginalPosition();
            }
        }
    }

    private void UpdateLine()
    {
        if (lineInstance == null) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position);
        float distance = directionToPlayer.magnitude;

        // Set the position halfway between the anchor and the player
        lineInstance.transform.position = transform.position + directionToPlayer / 2;

        // Scale the cylinder to match the distance
        lineInstance.transform.localScale = new Vector3(0.05f, distance / 2.2f, 0.05f);

        // Rotate the cylinder to face the player, making sure the Y-axis is aligned
        lineInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, directionToPlayer);
    }

    IEnumerator SelfDestroy()
    {
        yield return new WaitForSeconds(3);

        // Check if the UI was being obstructed by this anchor
        if (isUICurrentlyHit)
        {
            // Inform the client to decrement uiObstructedCount
            HandleUIHit(false);
        }

        client.DeleteAnchor(id);

        if (lineInstance != null)
        {
            Destroy(lineInstance);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (isUICurrentlyHit)
        {
            if (client != null)
            {
                client.ReturnUIToOriginalPosition();
            }
        }
    }
}
