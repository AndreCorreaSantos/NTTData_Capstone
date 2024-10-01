using System.Collections;
using System.Collections.Generic;
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

    public List<Transform> raycastOrigins;

    public GameObject linePrefab;
    public GameObject lineInstance;

    public string id;

    private bool DebugMode = false;
    private bool isUICurrentlyMoved = false;

    private enum RaycastHitType
    {
        None,
        MainCollider,
        SideCollider,
        Player,
        Other
    }

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

        // Ensure the raycastLayerMask includes the layers of the colliders
        if (raycastLayerMask == 0)
        {
            // Include all layers
            raycastLayerMask = Physics.DefaultRaycastLayers;
            // Include UI layer explicitly
            raycastLayerMask |= 1 << LayerMask.NameToLayer("UI");
            // Include layers for mainCollider and sideCollider if they are on different layers
            // For example:
            // raycastLayerMask |= 1 << LayerMask.NameToLayer("YourMainColliderLayer");
            // raycastLayerMask |= 1 << LayerMask.NameToLayer("YourSideColliderLayer");
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
                DoAllRaycasts();
            }
        }
        else
        {
            Debug.LogWarning("PlayerTransform is not assigned in Anchor.");
        }
    }

    void DoAllRaycasts()
    {
        bool mainColliderHit = false;
        bool sideColliderHit = false;

        foreach (Transform point in raycastOrigins)
        {
            Vector3 origin = point.position;
            RaycastHitType hitType = PerformRaycast(origin);

            if (hitType == RaycastHitType.MainCollider)
            {
                mainColliderHit = true;
            }
            else if (hitType == RaycastHitType.SideCollider)
            {
                sideColliderHit = true;
            }
        }

        HandleUIHit(mainColliderHit, sideColliderHit);
    }

    private RaycastHitType PerformRaycast(Vector3 origin)
    {
        // Calculate the direction from the raycast origin to the player
        Vector3 directionToPlayer = (playerTransform.position - origin).normalized;

        // Perform the raycast
        RaycastHit hitInfo;
        if (Physics.Raycast(origin, directionToPlayer, out hitInfo, Mathf.Infinity, raycastLayerMask))
        {
            // Visualize the raycast in the Scene view
            if (DebugMode)
            {
                Debug.DrawRay(origin, directionToPlayer * 1000f, Color.red, raycastInterval);
            }

            // The raycast hit something
            if (hitInfo.transform == playerTransform)
            {
                if (DebugMode) Debug.Log("Anchor raycast hit the player!");
                return RaycastHitType.Player;
            }
            else if (hitInfo.transform.CompareTag("mainCollider"))
            {
                if (DebugMode) Debug.Log("Anchor raycast hit the mainCollider!");
                return RaycastHitType.MainCollider;
            }
            else if (hitInfo.transform.CompareTag("sideCollider"))
            {
                if (DebugMode) Debug.Log("Anchor raycast hit a sideCollider!");
                return RaycastHitType.SideCollider;
            }
            else
            {
                if (DebugMode) Debug.Log("Anchor raycast hit: " + hitInfo.transform.name);
                return RaycastHitType.Other;
            }
        }
        else
        {
            // The raycast did not hit anything
            if (DebugMode) Debug.Log("Anchor raycast did not hit anything.");
            return RaycastHitType.None;
        }
    }

    private void HandleUIHit(bool mainColliderHit, bool sideColliderHit)
    {
        if ((mainColliderHit || sideColliderHit) && !isUICurrentlyMoved)
        {
            // Move the UI out of the way if it's not already moved
            isUICurrentlyMoved = true;
            if (client != null)
            {
                client.MoveUIOutOfWay();
            }
        }
        else if (!mainColliderHit && !sideColliderHit && isUICurrentlyMoved)
        {
            // Return the UI to its original position if no colliders are hit
            isUICurrentlyMoved = false;
            if (client != null)
            {
                client.ReturnUIToOriginalPosition();
            }
        }
        // If the UI is already moved and any collider is hit, do nothing (keep it moved)
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

        // Ensure the UI is reset when this anchor is destroyed

        client.DeleteAnchor(id);

        if (lineInstance != null)
        {
            Destroy(lineInstance);
        }
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
        if (isUICurrentlyMoved)
        {
            HandleUIHit(false, false);
        }

    }

    private void OnDestroy()
    {
        if (isUICurrentlyMoved)
        {
            if (client != null)
            {
                client.ReturnUIToOriginalPosition();
            }
        }
    }
}
