import cv2
import numpy as np
import json

def quaternion_to_rotation_matrix(qx, qy, qz, qw):
    return np.array([
        [1 - 2*(qy**2 + qz**2),     2*(qx*qy - qz*qw),     2*(qx*qz + qy*qw)],
        [2*(qx*qy + qz*qw),     1 - 2*(qx**2 + qz**2),     2*(qy*qz - qx*qw)],
        [2*(qx*qz - qy*qw),         2*(qy*qz + qx*qw),     1 - 2*(qx**2 + qy**2)]
    ])

def process_image(current_frame, depth_image, detection, rotation, position, fx, fy, cx, cy):
    try:
        box = detection.get('box')
        if not box:
            print("No bounding box found in detection.")
            return None

        box_values = [int(v) for v in box.values()]
        cv2.rectangle(current_frame, (box_values[0], box_values[1]), (box_values[2], box_values[3]), (0, 255, 0), 2)

        x = (box_values[0] + box_values[2]) / 2
        y = (box_values[1] + box_values[3]) / 2
        cv2.circle(current_frame, (int(x), int(y)), 5, (0, 0, 255), -1)
        print(f"Detected object at ({x}, {y})")

        # Use a fixed depth value for testing
        depth = 5.0

        # Invert the y-coordinate to align coordinate systems
        y_normalized = -(y - cy) / fy
        x_normalized = (x - cx) / fx
        obj_camspace = np.array([x_normalized * depth, y_normalized * depth, depth])

        # Extract and normalize quaternion components
        qw = rotation.get('w', 1)
        qx = rotation.get('x', 0)
        qy = rotation.get('y', 0)
        qz = rotation.get('z', 0)

        norm = np.sqrt(qw**2 + qx**2 + qy**2 + qz**2)
        qw /= norm
        qx /= norm
        qy /= norm
        qz /= norm

        # Compute and transpose the rotation matrix
        rotation_matrix = quaternion_to_rotation_matrix(qw, qx, qy, qz).T

        # Camera position
        px = position.get('x', 0)
        py = position.get('y', 0)
        pz = position.get('z', 0)

        # Transform to world coordinates
        world_coords = rotation_matrix @ obj_camspace + np.array([px, py, pz])

        object_position = {
            'x': float(world_coords[0]),
            'y': float(world_coords[1]),
            'z': float(world_coords[2])
        }
        print("Object Position: ", object_position)
        return object_position

    except Exception as e:
        print(f"Error in process_image: {e}")
        return None

import cv2
import numpy as np

def calculate_background_colors(image):
    mean_color_bgr = cv2.mean(image)[:3]
    mean_color_bgr_np = np.uint8([[mean_color_bgr]])
    mean_color_lab = cv2.cvtColor(mean_color_bgr_np, cv2.COLOR_BGR2LAB)
    L_mean, a_mean, b_mean = mean_color_lab[0, 0]
    delta_L = 63

    if L_mean >= delta_L:
        L_back = L_mean - delta_L
    else:
        L_back = 0

    if L_mean + delta_L <= 255:
        L_text = L_mean + delta_L
    else:
        L_text = 255

    back_color_lab = np.uint8([[[L_back, a_mean, b_mean]]])
    back_color_bgr = cv2.cvtColor(back_color_lab, cv2.COLOR_LAB2BGR)
    gui_back_color = (int(back_color_bgr[0, 0, 2]), int(back_color_bgr[0, 0, 1]), int(back_color_bgr[0, 0, 0]))
    
    text_color_lab = np.uint8([[[L_text, a_mean, b_mean]]])
    text_color_bgr = cv2.cvtColor(text_color_lab, cv2.COLOR_LAB2BGR)
    gui_text_color = (int(text_color_bgr[0, 0, 2]), int(text_color_bgr[0, 0, 1]), int(text_color_bgr[0, 0, 0]))
    
    return gui_back_color, gui_text_color
