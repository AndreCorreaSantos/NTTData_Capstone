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
    public float raycastInterval = 0.1f; // Raycast every 0.1 seconds for smoother detection

    public List<Transform> raycastOrigins;

    public GameObject linePrefab;
    public GameObject lineInstance;

    public string id;

    private bool DebugMode = false;


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
                sideColliderHit = false;
            }
            else if (hitType == RaycastHitType.SideCollider)
            {
                sideColliderHit = true;
                mainColliderHit = false;
            }
            else
            {
                mainColliderHit = false;
                sideColliderHit = false;
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
                client.mainHitPositions.Add(hitInfo.point);
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
        if (mainColliderHit)
        {
            client.sideObstructingAnchors.Remove(id);
            client.mainObstructingAnchors.Add(id);
        }
        else if(sideColliderHit)
        {
            client.mainObstructingAnchors.Remove(id);
            client.sideObstructingAnchors.Add(id);
        }
        else
        {
            client.mainObstructingAnchors.Remove(id);
            client.sideObstructingAnchors.Remove(id);
        }
    }

    IEnumerator SelfDestroy()
    {
        yield return new WaitForSeconds(3);

        // Ensure the UI is reset when this anchor is destroyed
        client.DeleteAnchor(id);
        client.mainObstructingAnchors.Remove(id);
        client.sideObstructingAnchors.Remove(id);

        if (lineInstance != null)
        {
            Destroy(lineInstance);
        }
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (client != null)
        {
            client.mainObstructingAnchors.Remove(id);
            client.sideObstructingAnchors.Remove(id);
        }
    }
}
