# image_processing.py

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

def process_image(current_frame, detection, rotation, position, fx, fy, cx, cy, depth_image):
    """
    Processes the detected object to calculate its world position using camera intrinsics and depth data.

    Parameters:
    - current_frame: The current color frame as a NumPy array.
    - detection: A single detection dictionary from YOLO.
    - rotation: Dictionary containing rotation data (x, y, z, w).
    - position: Dictionary containing position data (x, y, z).
    - fx, fy: Focal lengths.
    - cx, cy: Principal points.
    - depth_image: The corresponding depth frame as a NumPy array (can be None).

    Returns:
    - object_position: Dictionary with 'x', 'y', 'z' in world coordinates.
    """
    try:
        # Extract bounding box
        box = detection.get('box')
        if not box:
            print("No bounding box found in detection.")
            return None

        box_values = [int(v) for v in box.values()]
        cv2.rectangle(
            current_frame,
            (box_values[0], box_values[1]),
            (box_values[2], box_values[3]),
            (0, 255, 0),
            2
        )

        # Calculate center of the bounding box
        x = (box_values[0] + box_values[2]) / 2
        y = (box_values[1] + box_values[3]) / 2
        cv2.circle(current_frame, (int(x), int(y)), 5, (0, 0, 255), -1)

        # Extract depth from the depth_image at (x, y) if available
        if depth_image is not None:
            x_int = int(round(x))
            y_int = int(round(y))

            # Validate coordinates
            if (0 <= x_int < depth_image.shape[1]) and (0 <= y_int < depth_image.shape[0]):
                depth = depth_image[y_int, x_int]
                if depth <= 0:
                    print(f"Invalid depth value at ({x_int}, {y_int}): {depth}. Using default depth.")
                    depth = 1.5  # Default depth
            else:
                print(f"Coordinates ({x_int}, {y_int}) out of bounds for depth image. Using default depth.")
                depth = 1.5  # Default depth
        else:
            # Use default depth if no depth image is provided
            depth = 2.0

        # Normalize image coordinates
        x_normalized = (x - cx) / fx
        y_normalized = (y - cy) / fy

        # Convert to camera coordinates
        camera_coords = np.array([
            x_normalized * depth,
            y_normalized * depth,
            depth
        ])

        if rotation and position:
            # Extract rotation and position
            qx = rotation.get('x', 0)
            qy = rotation.get('y', 0)
            qz = rotation.get('z', 0)
            qw = rotation.get('w', 1)
            px = position.get('x', 0)
            py = position.get('y', 0)
            pz = position.get('z', 0)

            # Convert quaternion to rotation matrix
            rotation_matrix = quaternion_to_rotation_matrix(qx, qy, qz, qw)

            # Transform camera coordinates to world coordinates
            world_coords = rotation_matrix @ camera_coords + np.array([px, py, pz])

            object_position = {
                'x': float(world_coords[0]),
                'y': float(world_coords[1]),
                'z': float(world_coords[2])
            }
            print(f"Object position: {object_position}")
            return object_position

    except Exception as e:
        print(f"Error in process_image: {e}")
        return None

    return None
######################## CASE 2 #############################

import cv2
import numpy as np

def calculate_background_colors(image):
    # Calculate the mean color of the image in BGR
    mean_color_bgr = cv2.mean(image)[:3]
    mean_color_bgr_np = np.uint8([[mean_color_bgr]])
    
    # Convert mean color to LAB color space
    mean_color_lab = cv2.cvtColor(mean_color_bgr_np, cv2.COLOR_BGR2LAB)
    L_mean, a_mean, b_mean = mean_color_lab[0, 0]
    
    # Define the desired difference in L component
    delta_L = 63  # Adjust this value as needed for contrast
    
    if L_mean >= delta_L:
        # For images where we can subtract delta_L without going below 0
        L_back = L_mean - delta_L
    else:
        # If L_mean is less than delta_L, set background to 0 (black)
        L_back = 0

    if L_mean + delta_L <= 255:
        # For images where we can add delta_L without exceeding 255
        L_text = L_mean + delta_L
    else:
        # If L_mean + delta_L exceeds 255, set text to 255 (white)
        L_text = 255

    # Create background color (darker than mean color)
    back_color_lab = np.uint8([[[L_back, a_mean, b_mean]]])
    back_color_bgr = cv2.cvtColor(back_color_lab, cv2.COLOR_LAB2BGR)
    gui_back_color = (
        int(back_color_bgr[0, 0, 2]),  # R
        int(back_color_bgr[0, 0, 1]),  # G
        int(back_color_bgr[0, 0, 0])   # B
    )
    
    # Create text color (lighter than mean color)
    text_color_lab = np.uint8([[[L_text, a_mean, b_mean]]])
    text_color_bgr = cv2.cvtColor(text_color_lab, cv2.COLOR_LAB2BGR)
    gui_text_color = (
        int(text_color_bgr[0, 0, 2]),  # R
        int(text_color_bgr[0, 0, 1]),  # G
        int(text_color_bgr[0, 0, 0])   # B
    )
    
    return gui_back_color, gui_text_color

#################################################################
