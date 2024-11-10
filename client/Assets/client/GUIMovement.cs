using UnityEngine;

using System.Collections.Generic;


public class movementLogic
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


public class GUIMovementStateMachine
{
    private State currentState;
    public float desiredMotionVal;
    private int remendo = 0;

    public GUIMovementStateMachine()
    {
        currentState = State.FollowerCentralized; // Initial state
        desiredMotionVal = 0;
    }

    public async void TransitionState(float angleFromCameraFustrum, int collisionCountMain, int collisionCountSide, float maxSwayAngleFromCameraFustrum = 45f, float maxAngleFromCameraFustrum = 60f, float minAngleFromCameraFustrumAfterAdjust = 5f)
    {
        switch (currentState)
        {
            case State.FollowerCentralized:
                Debug.Log("q1");
                Debug.Log("Collision count main:" + collisionCountMain);
                if (collisionCountMain >= 1) currentState = State.HorizontalSway; break;
            case State.HorizontalSway:
                Debug.Log("q2");
                if (collisionCountMain == 0) currentState = State.Stable;
                else if (angleFromCameraFustrum > maxSwayAngleFromCameraFustrum) currentState = State.VerticalSway;
                break;
            case State.VerticalSway:
                Debug.Log("q3");
                if (collisionCountMain == 0) currentState = State.Stable;
                if (angleFromCameraFustrum > maxAngleFromCameraFustrum) currentState = State.AdjustToPlayerView;
                break;
            case State.Stable:
                Debug.Log("q4");
                Debug.Log("Remendo:" + remendo);
                if (collisionCountMain > 0) { remendo = 0 ; currentState = State.HorizontalSway;}
                else if (collisionCountSide <= 0 && remendo < 50) { remendo ++;}
                else if (collisionCountSide <= 0 && remendo >= 50) { remendo = 0; currentState = State.FollowerCentralized; Debug.Log("q4.1"); }
                else if (angleFromCameraFustrum > maxAngleFromCameraFustrum) {remendo = 0; currentState = State.AdjustToPlayerView; } 
                break;
            case State.AdjustToPlayerView:
                Debug.Log("q5");
                if (angleFromCameraFustrum < minAngleFromCameraFustrumAfterAdjust) currentState = State.FollowerCentralized;
                break;
        }
    }

    public State GetCurrentState()
    {
        return currentState;
    }

    public void AnalyzeAndMoveGUI(GameObject gui, Transform CameraTransform, HashSet<string> uiObstructedObjectsMain, HashSet<string> uiObstructedObjectsSide, Dictionary<string, GameObject> anchors, float uiFollowSpeed = 2f)
    {
        float distanceFromPlayer = 2.0f; // Adjust this value as needed
        Vector3 forwardDirection = CameraTransform.forward;
        Vector3 baseDesiredPosition = CameraTransform.position + forwardDirection * distanceFromPlayer;

        Vector3 desiredPosition = baseDesiredPosition;

        // Direction from player to current UI position
        Vector3 dirToCanvas = gui.transform.position - CameraTransform.position;
        dirToCanvas.y = 0;
        dirToCanvas.Normalize();

        // Direction from player to average anchor position
        Vector3 avgAnchorPos = GetAvgAnchorPos(anchors);
        Vector3 dirToAvgAnchor = avgAnchorPos - CameraTransform.position;
        dirToAvgAnchor.y = 0;
        dirToAvgAnchor.Normalize();

        // Cross product to determine rotation direction
        Vector3 cross = Vector3.Cross(dirToCanvas, dirToAvgAnchor);

        // Determine rotation direction based on cross product
        float rotationDirection = cross.y > 0 ? -1f : 1f;

        // Adjust desired position by rotating it around the player
        float rotationSpeed = 5.0f; // Degrees per second, adjust as needed

        //calculate angle in 2D XZ plane between camera and object
        Vector3 CameraPosition = new Vector3(CameraTransform.position.x, CameraTransform.position.y, CameraTransform.position.z);
        Vector3 CameraDirection = new Vector3(CameraTransform.forward.x, 0, CameraTransform.forward.z);
        Vector3 dirToCanvasNoY = new Vector3(dirToCanvas.x, 0, dirToCanvas.z);

        float angleInDegrees = Vector3.Angle(CameraDirection, dirToCanvas);
        TransitionState(angleInDegrees, uiObstructedObjectsMain.Count, uiObstructedObjectsSide.Count);
        switch (currentState)
        {
            case State.FollowerCentralized:
                desiredPosition = CameraPosition + forwardDirection * distanceFromPlayer;
                break;
            case State.HorizontalSway:
                float angularSpeed = 10f * rotationDirection;
                desiredPosition = RotateAroundPoint(
                    gui.transform.position,
                    CameraPosition,
                    Quaternion.Euler(0, angularSpeed, 0)
                );

                break;
            case State.VerticalSway:
                desiredPosition = gui.transform.position;
                desiredPosition.y = CameraPosition.y + 1;
                break;
            case State.Stable:
                desiredPosition = gui.transform.position;
                break;
            case State.AdjustToPlayerView:
                desiredPosition = CameraPosition + forwardDirection * distanceFromPlayer;
                break;
        }
        float positionLerpSpeed = uiFollowSpeed * Time.deltaTime;
        gui.transform.position = Vector3.Lerp(
            gui.transform.position,
            desiredPosition,
            positionLerpSpeed
        );

    }
    Vector3 GetAvgAnchorPos(Dictionary<string, GameObject> anchors)
    {
        Vector3 avgAnchorPos = Vector3.zero;
        foreach (var anchor in anchors.Values)
        {
            avgAnchorPos += anchor.transform.position;
        }
        avgAnchorPos /= anchors.Count;
        return avgAnchorPos;
    }
    private Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }

}
