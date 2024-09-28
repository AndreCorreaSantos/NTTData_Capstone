import cv2
import numpy as np
import json

def quaternion_to_rotation_matrix(qx, qy, qz, qw):
    return np.array([
        [1 - 2*(qy**2 + qz**2),     2*(qx*qy - qz*qw),     2*(qx*qz + qy*qw)],
        [2*(qx*qy + qz*qw),     1 - 2*(qx**2 + qz**2),     2*(qy*qz - qx*qw)],
        [2*(qx*qz - qy*qw),         2*(qy*qz + qx*qw),     1 - 2*(qx**2 + qy**2)]
    ])

def process_image(current_frame,depth_image, detection, rotation, position, fx, fy, cx, cy, ):
    try:
        print()
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

        if depth_image is not None:
            x_int = int(round(x))
            y_int = int(round(y))

            if (0 <= x_int < depth_image.shape[1]) and (0 <= y_int < depth_image.shape[0]):
                depth = depth_image[y_int, x_int]
                if depth <= 0:
                    print(f"Invalid depth value at ({x_int}, {y_int}): {depth}. Using default depth.")
                    depth = 1.5
            else:
                print(f"Coordinates ({x_int}, {y_int}) out of bounds for depth image. Using default depth.")
                depth = 1.5
        else:
            depth = 1.5

        depth = 5.0

        print(f"Camera intrinsics: fx={fx}, fy={fy}, cx={cx}, cy={cy}")
        x_normalized = (x - cx) / fx
        y_normalized = (y - cy) / fy

        obj_camspace = np.array([x_normalized * depth, y_normalized * depth, depth])

        print(f"Camera rotation: {rotation}")
        qx, qy, qz, qw = rotation.get('x', 0), rotation.get('y', 0), rotation.get('z', 0), rotation.get('w', 1)
        norm = np.sqrt(qx*qx + qy*qy + qz*qz + qw*qw)
        qx /= norm
        qy /= norm
        qz /= norm
        qw /= norm
        px = position.get('x', 0)
        py = position.get('y', 0)
        pz = position.get('z', 0)

        rotation_matrix = quaternion_to_rotation_matrix(qx, qy, qz, qw).T
        world_coords = rotation_matrix @ obj_camspace + np.array([px, py, pz])

        object_position = {
            'x': float(world_coords[0]),
            'y': float(world_coords[1]),
            'z': float(world_coords[2])
        }
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
