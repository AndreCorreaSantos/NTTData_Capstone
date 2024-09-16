                
import cv2
import numpy as np
import json






##### CASE 1 #############################
def quaternion_to_rotation_matrix(qx, qy, qz, qw):
    return np.array([
        [1 - 2*(qy**2 + qz**2),     2*(qx*qy - qz*qw),     2*(qx*qz + qy*qw)],
        [2*(qx*qy + qz*qw),     1 - 2*(qx**2 + qz**2),     2*(qy*qz - qx*qw)],
        [2*(qx*qz - qy*qw),         2*(qy*qz + qx*qw),     1 - 2*(qx**2 + qy**2)]
    ])

def process_image(current_frame,model,rotation,position):
    # Process the current frame
    results = model(current_frame, verbose=False)
    for detection in results:
        if detection is not None:
            detection_json = detection.tojson()
            r = json.loads(detection_json)
            if r:
                box = r[0]['box']
                box_values = [int(v) for v in box.values()]
                cv2.rectangle(
                    current_frame,
                    (box_values[0], box_values[1]),
                    (box_values[2], box_values[3]),
                    (0, 255, 0),
                    2
                )

                x = (box_values[0] + box_values[2]) / 2
                y = (box_values[1] + box_values[3]) / 2
                cv2.circle(current_frame, (int(x), int(y)), 5, (0, 0, 255), -1)

                # Assuming depth is available or use default
                depth = 0.5  # Default depth; adjust as necessary

                fx = fy = 600.0  # Focal length in pixels (example value)
                cx = current_frame.shape[1] / 2
                cy = current_frame.shape[0] / 2

                x_normalized = (x - cx) / fx
                y_normalized = (y - cy) / fy

                camera_coords = np.array([
                    x_normalized * depth,
                    y_normalized * depth,
                    depth
                ])

                if rotation and position:
                    # Extract position and rotation
                    qx = rotation['x']
                    qy = rotation['y']
                    qz = rotation['z']
                    qw = rotation['w']
                    px = position['x']
                    py = position['y']
                    pz = position['z']

                    rotation_matrix = quaternion_to_rotation_matrix(qx, qy, qz, qw)

                    world_coords = rotation_matrix @ camera_coords + np.array([px, py, pz])

                    object_position = {
                        'x': world_coords[0],
                        'y': world_coords[1],
                        'z': world_coords[2]
                    }
                    return object_position



######################## CASE 2 #############################



def calculate_background_colors(image):
    mean_color_bgr = cv2.mean(image)[:3]
    mean_color_bgr_np = np.uint8([[mean_color_bgr]])
    mean_color_lab = cv2.cvtColor(mean_color_bgr_np, cv2.COLOR_BGR2LAB)
    L, a, b = mean_color_lab[0, 0]
    L_scaled = L * (100 / 255)

    delta_L = 27
    if L_scaled > 50:
        L_new = max(0, L_scaled - delta_L)
    else:
        L_new = min(100, L_scaled + delta_L)

    L_new_scaled = L_new * (255 / 100)
    new_color_lab = np.uint8([[[L_new_scaled, a, b]]])
    new_color_bgr = cv2.cvtColor(new_color_lab, cv2.COLOR_LAB2BGR)
    new_color_rgb = (
        int(new_color_bgr[0, 0, 2]),
        int(new_color_bgr[0, 0, 1]),
        int(new_color_bgr[0, 0, 0])
    )

    L_white_new = min(100, L_scaled + delta_L)
    L_white_scaled = L_white_new * (255 / 100)
    white_color_lab = np.uint8([[[L_white_scaled, 0, 0]]])
    white_color_bgr = cv2.cvtColor(white_color_lab, cv2.COLOR_LAB2BGR)
    white_color_rgb = (
        int(white_color_bgr[0, 0, 2]),
        int(white_color_bgr[0, 0, 1]),
        int(white_color_bgr[0, 0, 0])
    )

    return new_color_rgb, white_color_rgb




#################################################################


