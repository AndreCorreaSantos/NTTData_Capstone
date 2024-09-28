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
    - rotation: Dictionary containing camera rotation data (x, y, z, w).
    - position: Dictionary containing camera position data (x, y, z).
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
        print(f"Detected object at ({x}, {y})")
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
            depth = 1.5

        depth = 5.0

        print(f"Camera intrinsics: fx={fx}, fy={fy}, cx={cx}, cy={cy}")
        # Normalize image coordinates
        x_normalized = (x - cx) / fx
        y_normalized = (y - cy) / fy

        obj_camspace = np.array([
            x_normalized * depth,
            y_normalized * depth,
            depth
        ])

        # Extract rotation and position
        qx = rotation.get('x', 0)
        qy = rotation.get('y', 0)
        qz = rotation.get('z', 0)
        qw = rotation.get('w', 1)
        px = position.get('x', 0)
        py = position.get('y', 0)
        pz = position.get('z', 0)

        # Convert quaternion to rotation matrix and transpose it
        rotation_matrix = quaternion_to_rotation_matrix(qx, qy, qz, qw).T

        # Transform camera coordinates to world coordinates
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
######################## CASE 2 #############################

def LAB_to_RGB(L, a, b):
    """
    Converts LAB color space to RGB color space.

    Parameters:
    L (float): L component of LAB color space.
    a (float): a component of LAB color space.
    b (float): b component of LAB color space.

    Returns:
    tuple: RGB values as integers in the range [0, 255].

    """

    y = (L + 16) / 116
    x = a / 500 + y
    z = y - b / 200
    f = lambda t: t ** 3 if t > 6/29 else (t - 16 / 116) / 7.787
    x_ = f(x) * 0.95047
    y_ = f(y) * 1.00000
    z_ = f(z) * 1.08883
    r = 3.2406 * x_ - 1.5372 * y_ - 0.4986 * z_
    g = -0.9689 * x_ + 1.8758 * y_ + 0.0415 * z_
    b = 0.0557 * x_ - 0.2040 * y_ + 1.0570 * z_
    r = 12.92 * r if r <= 0.0031308 else 1.055 * r ** (1/2.4) - 0.055
    g = 12.92 * g if g <= 0.0031308 else 1.055 * g ** (1/2.4) - 0.055
    b = 12.92 * b if b <= 0.0031308 else 1.055 * b ** (1/2.4) - 0.055

    return int(r*255), int(g*255), int(b*255)


def calculate_background_colors(image, UIScreenCorners):
    """
    Calculate the background and text colors for a graphical user interface (GUI) based on the specified corners of the screen.
    
    Parameters:
    - image: A NumPy array representing the input image from which colors will be calculated.
    - UIScreenCorners: A list of dictionaries containing the screen corner coordinates with 'x' and 'y' keys.

    Returns:
    - gui_back_color: A tuple representing the calculated background color in BGR format.
    - gui_text_color: A tuple representing the calculated text color in BGR format.
    - exterior_roi: A NumPy array representing the external region of interest around the screen corners.
    """

    # Round and extract the coordinates of the screen corners
    UIScreenCorners = [
        (np.round(UIScreenCorners[0]['x']), np.round(UIScreenCorners[0]['y'])),
        (np.round(UIScreenCorners[1]['x']), np.round(UIScreenCorners[1]['y'])),
        (np.round(UIScreenCorners[2]['x']), np.round(UIScreenCorners[2]['y'])),
        (np.round(UIScreenCorners[3]['x']), np.round(UIScreenCorners[3]['y']))
    ]

    print(f"UIScreenCorners: {UIScreenCorners}")

    height, width = image.shape[:2]
    
    # Calculate the minimum and maximum x and y coordinates to define the region of interest (ROI)
    min_x = int(min(UIScreenCorners[0][0], UIScreenCorners[1][0], UIScreenCorners[2][0], UIScreenCorners[3][0]))
    max_x = int(max(UIScreenCorners[0][0], UIScreenCorners[1][0], UIScreenCorners[2][0], UIScreenCorners[3][0]))
    min_y = int(min(UIScreenCorners[0][1], UIScreenCorners[1][1], UIScreenCorners[2][1], UIScreenCorners[3][1]))
    max_y = int(max(UIScreenCorners[0][1], UIScreenCorners[1][1], UIScreenCorners[2][1], UIScreenCorners[3][1]))
    
    # Define the interior region of interest (ROI) based on the calculated corners
    interior_min_x = max(0, min_x)
    interior_max_x = min(width, max_x)
    interior_min_y = max(0, min_y)
    interior_max_y = min(height, max_y)
    interior_roi = image[interior_min_y:interior_max_y, interior_min_x:interior_max_x]
    
    # Define an offset for the exterior ROI to capture surrounding areas
    interior_roi_offset = 0.05

    # Calculate the exterior ROI, clamping values to the image dimensions
    exterior_min_y = max(0, int(min_y - interior_roi_offset * height))
    exterior_max_y = min(height, int(max_y + interior_roi_offset * height))
    exterior_min_x = max(0, int(min_x - interior_roi_offset * width))
    exterior_max_x = min(width, int(max_x + interior_roi_offset * width))

    # Extract the exterior ROI
    exterior_roi = image[exterior_min_y:exterior_max_y, exterior_min_x:exterior_max_x]
    
    # Calculate the mean colors for the interior and exterior ROIs
    #interior_mean_color_bgr = np.array(cv2.mean(interior_roi[:3])[:3])
    exterior_mean_color_bgr = np.array(cv2.mean(exterior_roi[:3])[:3])

    # Reshape the mean color to create a valid image for cv2.cvtColor
    exterior_mean_color_bgr = np.reshape(exterior_mean_color_bgr, (1, 1, 3)).astype(np.uint8)

    # Convert the exterior mean color from BGR to LAB color space
    mean_color_lab = cv2.cvtColor(exterior_mean_color_bgr, cv2.COLOR_BGR2LAB)
    L, a, b = mean_color_lab[0, 0]

    # Scale L for further processing
    L_scaled = L * (100 / 255)

    delta_L = 27
    # Adjust the lightness based on its current value
    if L_scaled > 50:
        L_new = max(0, L_scaled - delta_L)
    else:
        # If L_mean is less than delta_L, set background to 0 (black)
        L_back = 0

    # Scale the adjusted L back to the range suitable for LAB
    L_new_scaled = L_new * (255 / 100)
    
    # Create a new LAB color using the adjusted lightness
    new_color_lab = np.uint8([[[L_new_scaled, a, b]]])
    
    # Convert the new LAB color back to BGR format
    new_color_bgr = cv2.cvtColor(new_color_lab, cv2.COLOR_LAB2BGR)

    # Define the GUI background color in BGR format
    gui_back_color = (
        int(new_color_bgr[0, 0, 2]),  # R
        int(new_color_bgr[0, 0, 1]),  # G
        int(new_color_bgr[0, 0, 0])   # B
    )

    # Calculate a lighter color for text, based on white in LAB color space
    white_color_bgr = LAB_to_RGB(100, 0, 0)  # Function to convert LAB white to RGB (White for now)
    gui_text_color = (
        int(white_color_bgr[0]),
        int(white_color_bgr[1]),
        int(white_color_bgr[2])
    )

    # Return the calculated GUI background and text colors, along with the ROI
    return gui_back_color, gui_text_color, interior_roi


#################################################################


if __name__ == "__main__":
    #print(f"calculate_background_colors: {calculate_background_colors(cv2.imread('test1.png'))}")
    print(f"{LAB_to_RGB(50, 0, 0)}")

