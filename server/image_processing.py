import cv2
import numpy as np
import json
import traceback

def quaternion_to_rotation_matrix(qx, qy, qz, qw):
    return np.array([
        [1 - 2*(qy**2 + qz**2),     2*(qx*qy - qz*qw),     2*(qx*qz + qy*qw)],
        [2*(qx*qy + qz*qw),     1 - 2*(qx**2 + qz**2),     2*(qy*qz - qx*qw)],
        [2*(qx*qz - qy*qw),         2*(qy*qz + qx*qw),     1 - 2*(qx**2 + qy**2)]
    ])

# def project_to_world(x, y, depth, fx, fy, cx, cy, camera_position, camera_rotation):


#     # Convert pixel coordinates to normalized image coordinates
#     Zc = depth
#     Xc = (x - cx) * Zc / fx
#     Yc = (y - cy) * Zc / fy  # Invert y coordinate
#     P_camera = np.array([Xc, Yc, Zc])

#     # Get rotation matrix from quaternion
#     qx = camera_rotation['x']
#     qy = camera_rotation['y']
#     qz = camera_rotation['z']
#     qw = camera_rotation['w']
#     R = quaternion_to_rotation_matrix(qx, qy, qz, qw)

#     T = np.array([camera_position['x'], camera_position['y'], camera_position['z']])

#     P_world = R @ P_camera + T  

#     return P_world


def get_world_position_from_screen_space(x,y,depth, inv_mat, camera_pos,width,height):
    
    # Convert screen position to Normalized Device Coordinates (NDC)
    ndc_x = (x - (width * 0.5)) / (width * 0.5)  # Range [-1, 1]
    ndc_y = (y - (height * 0.5)) / (height * 0.5)  # Range [-1, 1]
    
    # Create a point in NDC space (using the near clip plane for z = -1)
    near_clip_point = np.array([ndc_x, -ndc_y, -1.0, 1.0])  # 4D vector for NDC
    
    # Transform the NDC point to world space using the inverse matrix
    world_near = np.dot(inv_mat, near_clip_point)
    
    # Perform perspective divide to convert from homogeneous coordinates to 3D coordinates
    world_near_pos = world_near[:3] / world_near[3]
    
    # Calculate the ray direction by subtracting the camera's position from the world position
    ray_direction = world_near_pos - camera_pos
    
    # Normalize the ray direction
    ray_direction_normalized = ray_direction / np.linalg.norm(ray_direction)
    
    # Scale the ray direction by the provided depth value (world space depth)
    world_position = camera_pos + ray_direction_normalized * depth

    return world_position

def process_image(current_frame, depth_image, detection, inv_mat, camera_pos):
    try:
        box = detection.get('box')
        if not box:
            print("No bounding box found in detection.")
            return None

        box_values = [int(v) for v in box.values()]
        cv2.rectangle(current_frame, (box_values[0], box_values[1]), (box_values[2], box_values[3]), (0, 255, 0), 2)

        # Compute the center of the bounding box
        image_height, image_width, channels = current_frame.shape
        x_center = (box_values[0] + box_values[2]) / 2.0
        y_center = (box_values[1] + box_values[3]) / 2.0

        x_center = image_width - x_center  # Invert x coordinate

        # x_center = image_width - x_center  # Invert x coordinate
        # y_center = image_height - y_center  # Invert y coordinate
        cv2.circle(current_frame, (int(x_center), int(y_center)), 5, (0, 0, 255), -1)

        

        # Calculate mean depth over the bounding box
        depth = np.mean(depth_image[box_values[1]:box_values[3], box_values[0]:box_values[2]])

        # Project the center point to world coordinates
        center_wp = get_world_position_from_screen_space(x_center, y_center, depth, inv_mat, camera_pos, image_width, image_height)

        # Define the corner points
        box_p1 = (box_values[0], box_values[1])  # Top-left corner
        box_p2 = (box_values[2], box_values[3])  # Bottom-right corner

        # Project the corner points to world coordinates using the same depth
        w_box_p1 = get_world_position_from_screen_space(box_p1[0], box_p1[1], depth, inv_mat, camera_pos, image_width, image_height)
        w_box_p2 = get_world_position_from_screen_space(box_p2[0], box_p2[1], depth, inv_mat, camera_pos, image_width, image_height)

        # Calculate object dimensions
        object_width = np.linalg.norm(w_box_p1[[0, 2]] - w_box_p2[[0, 2]])  # Width in X-Z plane
        object_height = abs(w_box_p1[1] - w_box_p2[1])  # Height in Y-axis

        obj_data = {
            'x': float(center_wp[0]),
            'y': float(center_wp[1]),
            'z': float(center_wp[2]),
            'width': object_width,
            'height': object_height,
        }

        print(f"Object position: {obj_data}")
        return obj_data

    except Exception as e:
        print(traceback.format_exc())
        return None

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

    def f(t):
        return t ** 3 if t > 6/29 else (t - 16 / 116) / 7.787

    x_ = f(x) * 0.95047
    y_ = f(y) * 1.00000
    z_ = f(z) * 1.08883

    r = 3.2406 * x_ - 1.5372 * y_ - 0.4986 * z_
    g = -0.9689 * x_ + 1.8758 * y_ + 0.0415 * z_
    b = 0.0557 * x_ - 0.2040 * y_ + 1.0570 * z_

    def gamma_correction(c):
        return 12.92 * c if c <= 0.0031308 else 1.055 * (c ** (1/2.4)) - 0.055

    r = gamma_correction(r)
    g = gamma_correction(g)
    b = gamma_correction(b)

    r = np.clip(r * 255, 0, 255)
    g = np.clip(g * 255, 0, 255)
    b = np.clip(b * 255, 0, 255)

    return int(r), int(g), int(b)

def adjust_colors(a, b, flip_colors=False):
    """
    Adjust the colors by scaling and flipping if specified.

    Parameters:
    - a (numpy.ndarray): The first color channel.
    - b (numpy.ndarray): The second color channel.
    - flip_colors (bool): Whether to flip the colors.

    Returns:
    - Tuple[numpy.ndarray, numpy.ndarray]: Adjusted color channels a and b.
    """
    # Scale the input color channels by subtracting 128
    a_scaled = a - 128
    b_scaled = b - 128

    if flip_colors:
        # Flip the colors by negating the scaled values
        a_scaled = -a_scaled
        b_scaled = -b_scaled

    # Return adjusted values by adding 128 back to the scaled channels
    return a_scaled + 128, b_scaled + 128

# Example usage
# a, b = adjust_colors(a, b, flip_colors=True)


def calculate_background_colors(image, UIScreenCorners,flip_colors):
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

    image = cv2.flip(image, -1)
    # Extract the coordinates of the screen corners
    UIScreenCorners = [
        [UIScreenCorners[0]['x'], UIScreenCorners[0]['y']],
        [UIScreenCorners[1]['x'], UIScreenCorners[1]['y']],
        [UIScreenCorners[2]['x'], UIScreenCorners[2]['y']],
        [UIScreenCorners[3]['x'], UIScreenCorners[3]['y']]
    ]

    print(f"UIScreenCorners: {UIScreenCorners}")

    height, width = image.shape[:2]

    # Convert normalized coordinates to pixel coordinates
    pixel_corners = [[int(c[0] * width), int(c[1] * height)] for c in UIScreenCorners]

    xs = [p[0] for p in pixel_corners]
    ys = [p[1] for p in pixel_corners]

    min_x = max(0, min(xs))
    max_x = min(width, max(xs))
    min_y = max(0, min(ys))
    max_y = min(height, max(ys))

    print(f"min_x: {min_x}, max_x: {max_x}, min_y: {min_y}, max_y: {max_y}")
    print(f"width: {width}, height: {height}")

    # Define the interior region of interest (ROI) based on the calculated corners
    interior_roi = image[min_y:max_y, min_x:max_x]

    # Define an offset for the exterior ROI to capture surrounding areas
    interior_roi_offset = 0.05

    # Calculate the exterior ROI, clamping values to the image dimensions
    offset_x = int(interior_roi_offset * width)
    offset_y = int(interior_roi_offset * height)

    exterior_min_x = max(0, min_x - offset_x)
    exterior_max_x = min(width, max_x + offset_x)
    exterior_min_y = max(0, min_y - offset_y)
    exterior_max_y = min(height, max_y + offset_y)

    # Extract the exterior ROI
    exterior_roi = image[exterior_min_y:exterior_max_y, exterior_min_x:exterior_max_x]

    # Calculate the mean colors for the exterior ROI
    try:
        exterior_roi_lab = cv2.cvtColor(exterior_roi[:3], cv2.COLOR_BGR2LAB)
        mean_color_lab = np.array(cv2.mean(exterior_roi_lab)[:3])
    except:
        mean_color_lab = (0, 0, 0)

    # Calculate the mean color in LAB color space

    #print(f"Mean color LAB: {mean_color_lab}")

    L, a, b = mean_color_lab

    # Scale L for further processing
    L_scaled = L * (100 / 255)

    delta_L = 27
    # Adjust the lightness based on its current value
    if L_scaled > 50:
        L_new = max(0, L_scaled - delta_L)
    else:
        # If L_mean is less than delta_L, set background to 0 (black)
        L_new = 0

    # Scale the adjusted L back to the range suitable for LAB
    L_new_scaled = L_new * (255 / 100)

    # Flip the colors if specified
    a, b = adjust_colors(a, b, flip_colors=flip_colors)

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

    """ if L_new > 50:
        # Use a dark text color for light backgrounds
        gui_text_color = (0, 0, 0)
    else:
        # Use a light text color for dark backgrounds
        gui_text_color = (255, 255, 255) """ # Ficou estranho
    
    # Use a light text color for all backgrounds
    gui_text_color = (255, 255, 255)

    # Return the calculated GUI background and text colors, along with the ROI
    return gui_back_color, gui_text_color, interior_roi

