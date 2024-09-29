using System.Collections;
using UnityEngine;

public class Anchor : MonoBehaviour
{
    // Reference to the player's transform, set from ClientLogic
    public Transform playerTransform;

    public ClientLogic client;

    // LayerMask to specify which layers the raycast should interact with (optional)
    public LayerMask raycastLayerMask = Physics.DefaultRaycastLayers;

    // Variables to control raycasting frequency
    private float timeSinceLastRaycast = 0f;
    public float raycastInterval = 0.2f; // Raycast every 0.2 seconds

    private LineRenderer lineRenderer;

    public string id;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        StartCoroutine(SelfDestroy());

        // Optional: Check if playerTransform is assigned
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform is not assigned in Anchor. Please assign it from ClientLogic.");
        }
    }

    IEnumerator SelfDestroy()
    {
        yield return new WaitForSeconds(3);
        client.DeleteAnchor(id);
        Destroy(gameObject,0);
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
                        Debug.Log("Anchor raycast hit the player!");
                    }
                    else
                    {
                        Debug.Log("Anchor raycast hit: " + hitInfo.transform.name);
                    }
                }
                else
                {
                    // The raycast did not hit anything
                    Debug.Log("Anchor raycast did not hit anything.");
                }

                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, playerTransform.position);
            }
        }
        else
        {
            Debug.LogWarning("PlayerTransform is not assigned in Anchor.");
        }
    }
}
